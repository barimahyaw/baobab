namespace Baobab.SharedKernel.Infrastructure.Services.NotificationService;

public class EmailSettings
{
    public string SmtpHostName { get; set; } = default!;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = default!;
    public string SmtpPassword { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;
    public string SenderName { get; set; } = default!;
    public bool UseSsl { get; set; }
    public DeliveryDecision DeliveryDecision { get; set; } = default!;
}