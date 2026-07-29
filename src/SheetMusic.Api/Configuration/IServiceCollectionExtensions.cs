using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Resend;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Email;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

    /// <summary>
    /// Registers Data Protection and, where possible, persists its key ring to blob storage (container
    /// "data-protection-keys", blob "keys.xml") instead of the local filesystem. App Service
    /// auto-persists keys to %HOME%, but Azure Container Apps' filesystem is ephemeral: every
    /// scale-to-zero cold start would otherwise regenerate the ring, invalidating outstanding
    /// password-reset tokens and breaking token validation across replicas. See
    /// BlobStorage/AzureBlobXmlRepository.cs for why a small custom <see cref="IXmlRepository"/> is used
    /// instead of Microsoft's official (legacy-SDK-only) DataProtection.AzureStorage package.
    ///
    /// The <see cref="BlobServiceClient"/> is resolved lazily from the app's own service provider, via
    /// DI-injected options configuration, rather than by building a second, throwaway
    /// <see cref="IServiceProvider"/> here. The Azure Client Factory registration added by
    /// <c>AddAzureBlobServiceClient</c> caches client state in a singleton that is shared across every
    /// provider built from this same <see cref="IServiceCollection"/> - building and disposing an extra
    /// provider to eagerly grab a <see cref="BlobServiceClient"/> disposed that shared state out from
    /// under the app's real provider, causing an intermittent <see cref="ObjectDisposedException"/>
    /// ("ClientRegistration") the first time any later request resolved a <see cref="BlobServiceClient"/>
    /// (issue: 500 on GetPartsForSet and similar). Guarded: hosts with no blob storage configured at all
    /// (e.g. the WebApplicationFactory-based test host, which doesn't run through the AppHost and where
    /// <see cref="BlobServiceClient"/> is registered but throws <see cref="InvalidOperationException"/>
    /// on resolution) fall back to Data Protection's default local key storage instead of failing
    /// application startup - matching the historical, storage-optional behaviour.
    /// </summary>
    public static IServiceCollection AddSheetMusicDataProtection(this IServiceCollection services, string applicationName)
    {
        services.AddDataProtection().SetApplicationName(applicationName);

        services.AddOptions<KeyManagementOptions>().Configure<IServiceProvider>((options, serviceProvider) =>
        {
            try
            {
                var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();
                var keyRingContainer = blobServiceClient.GetBlobContainerClient("data-protection-keys");
                options.XmlRepository = new AzureBlobXmlRepository(keyRingContainer, "keys.xml");
            }
            catch (InvalidOperationException)
            {
                // No blob storage configured for this host; Data Protection uses its default key storage.
            }
        });

        return services;
    }

    public static IServiceCollection AddSheetMusicSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // Same binary serves both test and prod (see AppHost.cs), so this list is the union of both
            // environments' real frontend origins.
            options.AddPolicy("AllowMember", builder =>
                builder.WithOrigins("https://medlem.stavanger-brassband.no",
                                    "https://orange-mud-00eed1803.1.azurestaticapps.net",
                                    "http://localhost:5000",
                                    "http://localhost:5100",
                                    "https://localhost:5001")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        var secretKey = Encoding.ASCII.GetBytes(configuration[ConfigKeys.JwtSigningKey] ?? throw new MissingConfigurationException(ConfigKeys.JwtSigningKey));

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
                            return;
                        }

                        // Roles are resolved per request rather than minted into the token: tokens live for
                        // days with no revocation, so a role change has to take effect immediately. Any role
                        // claim carried by the token itself is dropped first, so the database is the only
                        // source of truth even if a token was issued with roles baked in.
                        if (context.Principal!.Identity is ClaimsIdentity identity)
                        {
                            foreach (var staleRoleClaim in identity.FindAll(identity.RoleClaimType).ToList())
                                identity.RemoveClaim(staleRoleClaim);

                            identity.AddClaims(resolved.Roles.Select(role => new Claim(identity.RoleClaimType, role)));
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
            options.AddPolicy(AuthPolicy.Admin, policy => policy.RequireRole(Roles.Admin));
            options.AddPolicy(AuthPolicy.ManageMusic, policy => policy.RequireRole(Roles.Admin, Roles.Noteansvarlig));
        });
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
