using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;
using System.Threading.RateLimiting;

namespace Baobab.SharedKernel.Presentation;

public static class DependencyInjection
{
    internal static IApiVersioningBuilder UseVersioning(this IServiceCollection services,
        int version,
        int versionPrec)
        => services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(version, versionPrec);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    public static IServiceCollection AddVersioning(
        this IServiceCollection services,
        int version,
        int versionPrec)
    {
        services.UseVersioning(version, versionPrec);
        return services;
    }

    public static WebApplicationBuilder AddSerilogConfiguration(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var seqEndpoint = configuration["SEQ_URL"] ?? Environment.GetEnvironmentVariable("SEQ_URL");
        builder.Host.UseSerilog((context, cfg) =>
        {
            cfg.ReadFrom.Configuration(context.Configuration);
            if (!string.IsNullOrWhiteSpace(seqEndpoint)) cfg.WriteTo.Seq(seqEndpoint);
        });
        return builder;
    }

    public static IServiceCollection AddSwaggerGenConfig(this IServiceCollection services, string projectName, string serviceName)
     => services.AddSwaggerGen(setupAction =>
     {
         var xmlCommentFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
         var xmlCommentPath = Path.Combine(AppContext.BaseDirectory, xmlCommentFile);
         var xmlCommentPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xmlCommentFile);
         //setupAction.IncludeXmlComments(xmlCommentPath);
         setupAction.SwaggerDoc("v1", new OpenApiInfo
         {
             Version = "v1",
             Title = $"{projectName} {serviceName} Service",
             License = new OpenApiLicense
             {
                 Name = "MIT License",
                 //Url = new Uri("") // to be provided later
             }
         });
         setupAction.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
         {
             Name = "Authorization",
             In = ParameterLocation.Header,
             Type = SecuritySchemeType.ApiKey,
             Scheme = "Bearer",
             BearerFormat = "JWT",
             Description = "Input your Bearer token in this format - Bearer {your token here} to access this API"
         });
         setupAction.AddSecurityRequirement(document => new OpenApiSecurityRequirement
         {
            {
                new OpenApiSecuritySchemeReference("Bearer", document), new List<string>()
            }
         });
     });

    public static IApplicationBuilder UseSwaggerUIConfig(this IApplicationBuilder app, string projectName, string serviceName)
    {
        app.UseSwagger();
        app.UseSwaggerUI(setupAction =>
        {
            setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", $"{projectName} - {serviceName} Service");
            setupAction.DefaultModelExpandDepth(2);
            setupAction.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            setupAction.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            setupAction.EnableDeepLinking();
            setupAction.DisplayOperationId();
        });

        return app;
    }

    public static IServiceCollection AddGlobalExceptionHandlerConfig(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddRateLimitConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Get values from environment variables with fallback defaults
            var permitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_PERMIT_LIMIT") ?? "100");
            var windowInSeconds = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_WINDOW_SECONDS") ?? "60");
            var queueLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_QUEUE_LIMIT") ?? "0");

            // Configure global rate limiting based on client IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowInSeconds),
                        QueueLimit = queueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // OnRejected handler
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var timeSpan)
                    ? timeSpan.TotalSeconds.ToString()
                    : "n/a";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter
                }, token);
            };
        });
        return services;
    }
}