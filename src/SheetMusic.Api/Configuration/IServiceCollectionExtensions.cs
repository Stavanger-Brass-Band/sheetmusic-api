using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Repositories;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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

                    var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    if (Guid.TryParse(context.Principal?.Identity?.Name, out var userId))
                    {
                        var user = await userRepo.GetByIdAsync(userId);

                        if (user == null)
                        {
                            // return unauthorized if user no longer exists
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

            setup.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "oauth2",
                            Type = ReferenceType.SecurityScheme,
                        }
                    },
                    new List<string>()
                }
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
}
