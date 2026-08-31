using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Baobab.SharedKernel.Presentation.MinimalApi;

public static class ApiVersioningExtension
{
    public static WebApplication AddMinimalApiVersioningSet(
        this WebApplication app,
        (int version, int versionPrec)[] versions)
    {
        var versionSetBuilder = app.NewApiVersionSet()
            .ReportApiVersions();

        foreach (var (version, versionPrec) in versions)
            versionSetBuilder.HasApiVersion(new ApiVersion(version, versionPrec));

        ApiVersionSet versionSet = versionSetBuilder.Build();

        RouteGroupBuilder? routeGroupBuilder = app
            .MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet);

        app.MapEndpoints(routeGroupBuilder);

        return app;
    }

    public static IServiceCollection AddMinimalApiVersioning(this IServiceCollection services, int version, int versionPrec)
    {
        services.UseVersioning(version, versionPrec)
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
