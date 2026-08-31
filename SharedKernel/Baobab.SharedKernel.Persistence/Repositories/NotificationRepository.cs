using Baobab.SharedKernel.Domain.Notifications.Repositories;
using Baobab.SharedKernel.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.Repositories;

internal sealed class NotificationRepository<TDbContext>(TDbContext dbContext) 
        : INotificationRepository
        where TDbContext : DbContext
{
    public async Task AddAsync(Notification notification)
        => await dbContext.Set<Notification>().AddAsync(notification);
}