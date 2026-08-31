using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;

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
                 Name = "Keed Digital license",
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
}