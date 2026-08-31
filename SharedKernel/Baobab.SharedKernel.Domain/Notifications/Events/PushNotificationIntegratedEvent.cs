namespace Baobab.SharedKernel.Domain.Notifications.Events;

public sealed record PushNotificationIntegratedEvent
    (string[] To,
    string Message,
    string ProductId);
