using Baobab.SharedKernel.Application.Abstractions.Data;
using Baobab.SharedKernel.Domain.Notifications.Repositories;
using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Persistence.OutBox.Idempotence;
using Baobab.SharedKernel.Persistence.OutBox.Interceptors;
using Baobab.SharedKernel.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baobab.SharedKernel.Persistence;

public static class DependencyInjection
{
    private static DbContextOptionsBuilder ConfigureDbContextOptions(this DbContextOptionsBuilder options,
        string connectionString)
        => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

    public static IServiceCollection AddDatabaseConfiguration<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services.AddScoped<ConvertDomainEventsToOutboxMessagesInterceptor>();

        var cs = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? configuration.GetConnectionString("CONNECTION_STRING")
            ?? throw new ArgumentNullException("CONNECTION_STRING");

        services.AddDbContext<TDbContext>(
            (sp, optionBuilder) =>
            {                
                optionBuilder.ConfigureDbContextOptions(cs);

                var interceptor = sp.GetService<ConvertDomainEventsToOutboxMessagesInterceptor>();
                optionBuilder.AddInterceptors(interceptor!);
            });

        return services;
    }

    public static IServiceCollection AddOutboxIdempotentConfig<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Decorates every registered INotificationHandler<TDomainEvent> with IdempotentDomainEventHandler<,>
        // so domain event handlers are idempotent when the OutBox reprocesses a message after a failure.
        services.AddScoped<IOutboxMessageContext, OutboxMessageContext>();

        var handlerDescriptors = services
            .Where(s => s.ServiceType.IsGenericType
                && s.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                && typeof(IDomainEvent).IsAssignableFrom(s.ServiceType.GetGenericArguments()[0]))
            .ToList();

        foreach (var descriptor in handlerDescriptors)
        {
            var domainEventType = descriptor.ServiceType.GetGenericArguments()[0];
            var decoratorType = typeof(IdempotentDomainEventHandler<,>)
                .MakeGenericType(domainEventType, typeof(TDbContext));

            services.Remove(descriptor);

            services.Add(ServiceDescriptor.Describe(
                descriptor.ServiceType,
                sp =>
                {
                    object innerHandler = descriptor.ImplementationFactory != null
                        ? descriptor.ImplementationFactory(sp)
                        : ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);

                    return ActivatorUtilities.CreateInstance(sp, decoratorType, innerHandler);
                },
                descriptor.Lifetime));
        }

        return services;
    }

    public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();

        return services;
    }

    public static IServiceCollection AddNotificationRepository<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
        => services.AddScoped<INotificationRepository, NotificationRepository<TDbContext>>();
}