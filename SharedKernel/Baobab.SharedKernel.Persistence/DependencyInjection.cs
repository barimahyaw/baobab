using Baobab.SharedKernel.Application.Abstractions.Data;
using Baobab.SharedKernel.Domain.Notifications.Repositories;
using Baobab.SharedKernel.Persistence.OutBox.Interceptors;
using Baobab.SharedKernel.Persistence.Repositories;
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

    public static IServiceCollection AddOutboxIdempotentConfig(this IServiceCollection services)
    {
        // the decorator pattern is used to make sure that the domain event handlers are idempotent
        // come from the Scrutor library
        //services.Decorate(typeof(INotificationHandler<>), typeof(IdempotentDomainEventHandler<,>));

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