using Baobab.SharedKernel.Application.Abstractions.Services;
using Baobab.SharedKernel.Domain.Notifications.Events;
using MassTransit;

namespace Baobab.SharedKernel.Infrastructure.Services.NotificationService;

public class PushNotificationService(IPublishEndpoint publishEndpoint) : IPushNotificationService
{
    public async Task PublishNotificationAsync(string[] to, string message, string productId)
        => await publishEndpoint.Publish(new PushNotificationIntegratedEvent
            (
                to,
                message,
                productId
            ));
}
