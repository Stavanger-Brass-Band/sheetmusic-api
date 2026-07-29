using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Resend;
using SheetMusic.Api.Database;
using SheetMusic.Api.Email;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;

namespace SheetMusic.Api.Configuration;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ResendEmailSender"/> when <see cref="ConfigKeys.ResendApiKey"/> is
    /// configured, or a logging no-op sender otherwise. Guards against a test environment - holding an
    /// anonymised copy of production data - ever sending real password-reset email because a live
    /// Resend key was configured there by mistake: without a key, nothing can be sent regardless.
    /// </summary>
    public static IServiceCollection AddSheetMusicEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        var resendApiKey = configuration[ConfigKeys.ResendApiKey];
        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            services.AddResend(options => options.ApiToken = resendApiKey);
            services.AddScoped<IEmailSender, ResendEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, NoOpEmailSender>();
        }

        return services;
    }

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

                    if (Guid.TryParse(context.Principal?.Identity?.Name, out var userId))
                    {
                        var resolver = context.HttpContext.RequestServices.GetRequiredService<LegacyAuthResolver>();
                        var resolved = await resolver.ResolveAsync(userId);

                        if (resolved is null || resolved.IsInactive)
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
        services.AddScoped<LegacyAuthResolver>();

        return services;
    }

    /// <summary>
    /// Registers a native OpenAPI document per discovered API version, wired up with the same version metadata,
    /// OAuth2 security scheme and operation shaping (OData query params, hidden version header, locked-down
    /// api-version parameter) previously provided by Swashbuckle. Must be called after <see cref="AddSheetMusicVersioning"/>.
    /// </summary>
    public static IServiceCollection AddSheetMusicOpenApi(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var apiVersionDescriptionProvider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            services.AddOpenApi(description.GroupName, options =>
            {
                options.AddDocumentTransformer(new ApiVersionDocumentInfoTransformer(description));
                options.AddDocumentTransformer<OAuthSecuritySchemeTransformer>();
                options.AddOperationTransformer<OpenApiDefaultValuesTransformer>();
                options.AddOperationTransformer<HideApiVersionHeaderTransformer>();
                options.AddOperationTransformer<ODataQueryParamsTransformer>();
            });
        }

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
