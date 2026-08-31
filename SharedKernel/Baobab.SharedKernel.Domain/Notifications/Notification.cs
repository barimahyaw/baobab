using Baobab.SharedKernel.Domain.Notifications.Enums;

namespace Baobab.SharedKernel.Domain.Notifications;

public class Notification
{
    public long Id { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string Contact { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public bool IsDelivered { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? DeliveryError { get; private set; }

    private Notification(string contact, string message, string subject)
    {
        Contact = contact;
        Message = message;
        Subject = subject;
        IsDelivered = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Notification() { }

    public static Notification Create(string contact, string message, string subject)
        => new(contact, message, subject);

    public void MarkAsDelivered()
    {
        IsDelivered = true;
        DeliveredAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        IsDelivered = false;
        DeliveryError = error;
    }

    public void SetNotificationType(NotificationType type)
       => NotificationType = type;
}