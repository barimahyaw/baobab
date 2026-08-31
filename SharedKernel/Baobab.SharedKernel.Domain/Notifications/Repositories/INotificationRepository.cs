namespace Baobab.SharedKernel.Domain.Notifications.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
}