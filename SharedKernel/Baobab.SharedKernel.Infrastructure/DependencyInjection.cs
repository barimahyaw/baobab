using Hangfire;
using Hangfire.Redis.StackExchange;
using Baobab.SharedKernel.Application.Abstractions.Services;
using Baobab.SharedKernel.Domain.Results;
using Baobab.SharedKernel.Infrastructure.Cache;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;
using System.Text;
using Baobab.SharedKernel.Application.Abstractions.Data;
using Baobab.SharedKernel.Infrastructure.Services.NotificationService;
using Baobab.SharedKernel.Infrastructure.Services.AmazonStorageService;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Baobab.SharedKernel.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Baobab.SharedKernel.Infrastructure.Services;


namespace Baobab.SharedKernel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedKernelExternalServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>()
            .TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<ISMSNotificationService, SMSNotificationService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        return services;
    }

    public static IServiceCollection AddAmazonStorageService(this IServiceCollection services)
    {
        services.AddScoped<IAmazonSimpleStorageService, AmazonSimpleStorageService>();
        return services;
    }

    public static IServiceCollection AddMemoryCacheService(this IServiceCollection services)
        => services.AddSingleton<ICacheManager, MemoryCacheManager>();

    public static IServiceCollection AddDistributedCacheService(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheConnection = Environment.GetEnvironmentVariable("CACHE_CONNECTION_STRING")
            ?? configuration["CACHE_CONNECTION_STRING"]
            ?? throw new ArgumentNullException("CACHE_CONNECTION_STRING");

        services.AddScoped(cfg =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(cacheConnection);
            return multiplexer.GetDatabase();
        });

        services.AddScoped<ICacheManager, DistributedCacheManager>();

        return services;
    }

    public static IServiceCollection AddJwtAuthenticationConfiguration(this IServiceCollection services)
    {
        var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_ISSUER_SIGNING_KEY")
            ?? throw new ArgumentNullException("JWT_ISSUER_SIGNING_KEY"));

        services.AddAuthentication(auth =>
        {
            auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(bearer =>
        {
            bearer.RequireHttpsMetadata = false;
            bearer.SaveToken = true;
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero
            };
            bearer.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = c =>
                {
                    if (c.Exception is SecurityTokenExpiredException)
                    {
                        c.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        c.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(Result.Fail(new Error("expired_token", "The Token is expired.")));
                        return c.Response.WriteAsync(result);
                    }
                    else
                    {
#if DEBUG
                        c.NoResult();
                        c.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        c.Response.ContentType = "text/plain";
                        return c.Response.WriteAsync(c.Exception.ToString());
#else
                            c.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            c.Response.ContentType = "application/json";
                            var result = JsonConvert.SerializeObject(Result.Fail("An unhandled error has occurred."));
                            return c.Response.WriteAsync(result);
#endif
                    }
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(Result.Fail(new Error("unauthorized", "You are not Authorized.")));
                        return context.Response.WriteAsync(result);
                    }

                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Response.ContentType = "application/json";
                    var result = JsonConvert.SerializeObject(Result.Fail(new Error("unauthorized_access", "You are not authorized to access this resource.")));
                    return context.Response.WriteAsync(result);
                },
            };
        });
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        return services;
    }

    public static IServiceCollection AddHangfireConfiguration<TDbContext>(this IServiceCollection services, IConfiguration configuration, string prefix = default!)
        where TDbContext : DbContext
    {
        var options = new RedisStorageOptions
        {
            SucceededListSize = 10000,
            DeletedListSize = 1000,
            InvisibilityTimeout = TimeSpan.FromSeconds(30),
            ExpiryCheckInterval = TimeSpan.FromSeconds(30),
        };

        if (!string.IsNullOrWhiteSpace(prefix)) options.Prefix = prefix;

        var connectionString = Environment.GetEnvironmentVariable("CACHE_CONNECTION_STRING")
            ?? configuration["CACHE_CONNECTION_STRING"]
            ?? throw new ArgumentNullException("CACHE_CONNECTION_STRING");

        services.AddHangfire(config =>
        {
            config.UseRedisStorage(connectionString, options);
            config.UseSerilogLogProvider();
        });

        services.AddHangfireServer();

        services.AddScoped<IOutBoxMessagesProcessingJob, OutBoxMessagesProcessingJob<TDbContext>>();

        return services;
    }

    public static WebApplication UseHangfireDashboard(this WebApplication app, string dashboardTitle, string serviceName)
    {
        app.UseHangfireDashboard($"/{serviceName}/workers", new DashboardOptions
        {
            DashboardTitle = dashboardTitle,
            Authorization = new[] { new HanfireAuthorizationFilter() },
            AppPath = "/",
            DisplayStorageConnectionString = false,
            IsReadOnlyFunc = context => false,
            IgnoreAntiforgeryToken = true,
        });
        return app;
    }

    public static IServiceCollection AddSMTPServerConfiguration
      (this IServiceCollection services,
      IConfiguration configuration,
      string configurationName)
    {
        // var emailConfig = configuration.GetSection("EmailConfiguration");
        var emailConfig = configuration.GetSection(configurationName);
        services.Configure<EmailSettings>(emailConfig);
        return services;
    }

    public static IServiceCollection AddOpenTelemetryConfiguration
        (this IServiceCollection services, WebApplicationBuilder builder, string serviceName)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddOtlpExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHangfireInstrumentation()
                    .AddSqlClientInstrumentation(o => o.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName);

                tracing.AddOtlpExporter();
            });

        builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());

        return services;
    }

    public static IServiceCollection AddGRPCServerConfiguration(this IServiceCollection services)
    {
        services.AddGrpc();
        services.AddGrpcReflection();
        services.AddGrpcHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

        return services;
    }

    public static WebApplication AddGRPCClientServiceConfiguration<T>(this WebApplication app)
        where T : class
    {
        app.MapGrpcService<T>();
        app.MapGrpcHealthChecksService();
        app.MapGet("/", () => "pong");

        return app;
    }

    public static IServiceCollection AddGrpcClientConfiguration<T>(this IServiceCollection services, string baseUrl)
        where T : class
    {
        var notificationEndpoint = Environment.GetEnvironmentVariable(baseUrl)
                    ?? throw new ArgumentNullException(baseUrl);

        services.AddGrpcClient<T>(opt =>
        {
            opt.Address = new Uri(notificationEndpoint);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });

        return services;
    }

    public static IServiceCollection AddSentryConfiguration(this IServiceCollection services, IConfiguration config)
    {
        // Wire-compatible with self-hosted GlitchTip as well as hosted Sentry -
        // point SENTRY_DSN at whichever instance you run.
        var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
            ?? config["SENTRY_DSN"]
            ?? throw new ArgumentNullException("SENTRY_DSN");

        SentrySdk.Init(opt =>
        {
            opt.Dsn = dsn;
            opt.UseOpenTelemetry();
        });

        return services;
    }
}