namespace Baobab.SharedKernel.Domain.Notifications.Events;

public sealed record SmsNotificationIntegratedEvent
    (string[] Recipients,
    string Message,
    string SenderId,
    string ProductId);
