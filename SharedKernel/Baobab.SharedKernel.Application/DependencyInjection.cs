using FluentValidation;
using Baobab.SharedKernel.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MassTransit;

namespace Baobab.SharedKernel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMediatorConfig<T>(this IServiceCollection services)
        where T : class
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<T>();

            config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        return services;
    }

    public static IServiceCollection AddAssemblyValidator<T>(this IServiceCollection services)
        where T : class
    {
        services.AddValidatorsFromAssembly(typeof(T).Assembly, includeInternalTypes: true);
        return services;
    }

    public static IServiceCollection AddMassTransitRabbitMQConfig<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class
    {
        services.AddMassTransit(config =>
        {
            config.AddConsumersFromNamespaceContaining<T>();

            config.UsingRabbitMq((ctx, cfg) =>
            {
                var uri = new Uri(Environment.GetEnvironmentVariable("SERVICE_BUS_URI")
                    ?? configuration["SERVICE_BUS_URI"]
                    ?? throw new ArgumentNullException("SERVICE_BUS_URI"));

                var userName = Environment.GetEnvironmentVariable("SERVICE_BUS_USER_NAME")
                    ?? configuration["SERVICE_BUS_USER_NAME"]
                    ?? throw new ArgumentNullException("SERVICE_BUS_USER_NAME");

                var password = Environment.GetEnvironmentVariable("SERVICE_BUS_PASSWORD")
                    ?? configuration["SERVICE_BUS_PASSWORD"]
                    ?? throw new ArgumentNullException("SERVICE_BUS_PASSWORD");

                cfg.Host(uri, host =>
                {
                    host.Username(userName);
                    host.Password(password);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
