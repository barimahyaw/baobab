namespace Baobab.SharedKernel.Domain.Notifications.Events;

public sealed record EmailNotificationIntegratedEvent
    (string Email,
    string Subject,
    string Message,
    string Product,
    byte[]? Attachment = default,
    string? AttachmentName = default);
