using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

namespace SheetMusic.Api.Configuration;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSheetMusicSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowMember", builder =>
                builder.WithOrigins("https://sheetmusic-member.azurewebsites.net",
                                    "https://medlem.stavanger-brassband.no",
                                    "http://localhost:5000",
                                    "http://localhost:5100",
                                    "https://localhost:5001")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        var secretKey = Encoding.ASCII.GetBytes(configuration[ConfigKeys.Secret] ?? throw new MissingConfigurationException(ConfigKeys.Secret));

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (context is null)
                        throw new Exception("Unable to process token with no context");

                    if (!context.Principal?.Identity?.IsAuthenticated ?? false || context.Principal?.Identity?.Name == null)
                    {
                        context.Fail("Unauthorized");
                        return;
                    }

                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    if (Guid.TryParse(context.Principal?.Identity?.Name, out var userId))
                    {
                        // Try as ApplicationUser ID (v2 tokens)
                        var user = await userManager.FindByIdAsync(userId.ToString());

                        // Fall back to Musician ID lookup (v1 tokens)
                        if (user == null)
                        {
                            var dbContext = context.HttpContext.RequestServices.GetRequiredService<SheetMusicContext>();
                            var musician = await dbContext.Musicians.FirstOrDefaultAsync(m => m.Id == userId);
                            if (musician?.ApplicationUserId != null)
                            {
                                user = await userManager.FindByIdAsync(musician.ApplicationUserId.Value.ToString());
                            }
                        }

                        if (user == null || user.Inactive)
                        {
                            context.Fail("Unauthorized");
                        }
                    }
                    else
                    {
                        context.Fail("Unauthorized");
                    }
                }
            };
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.Requirements.Add(new AdministratorRequirement("Admin")));
        });
        services.AddScoped<IAuthorizationHandler, AdministratorRequirementHandler>();

        return services;
    }

    public static IServiceCollection AddSheetMusicSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(setup =>
        {
            setup.OperationFilter<SwaggerDefaultValues>();
            setup.OperationFilter<SwaggerHideVersionHeader>();

            setup.AddSecurityDefinition("oauth2",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows()
                    {
                        Password = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("/token", UriKind.Relative),
                            TokenUrl = new Uri("/token", UriKind.Relative)
                        }
                    }
                });

            setup.AddSecurityRequirement(doc =>
            {
                var requirement = new OpenApiSecurityRequirement();
                requirement[new OpenApiSecuritySchemeReference("oauth2", doc)] = new List<string>();
                return requirement;
            });

            //Locate the XML file being generated by ASP.NET...
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.XML";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            //... and tell Swagger to use those XML comments.
            if (File.Exists(xmlPath))
                setup.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IServiceCollection AddSheetMusicVersioning(this IServiceCollection services)
    {
        var builder = services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
            config.ApiVersionReader = ApiVersionReader.Combine(new HeaderApiVersionReader("x-api-version"), new QueryStringApiVersionReader("api-version"));
        }).AddApiExplorer();

        return services;
    }

    public static IServiceCollection AddSheetMusicRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Azure App Service terminates client connections at its own front-end proxy, so the real client
        // IP is only available via the X-Forwarded-For header rather than Connection.RemoteIpAddress.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var forgotPasswordPermitLimit = configuration.GetValue<int?>(ConfigKeys.RateLimitingForgotPasswordPermitLimit) ?? 10;
        var forgotPasswordWindowSeconds = configuration.GetValue<int?>(ConfigKeys.RateLimitingForgotPasswordWindowSeconds) ?? 60;
        var tokenPermitLimit = configuration.GetValue<int?>(ConfigKeys.RateLimitingTokenPermitLimit) ?? 20;
        var tokenWindowSeconds = configuration.GetValue<int?>(ConfigKeys.RateLimitingTokenWindowSeconds) ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.ForgotPassword, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = forgotPasswordPermitLimit,
                        Window = TimeSpan.FromSeconds(forgotPasswordWindowSeconds),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.Token, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = tokenPermitLimit,
                        Window = TimeSpan.FromSeconds(tokenWindowSeconds),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetClientPartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
