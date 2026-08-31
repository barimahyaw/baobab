using Baobab.SharedKernel.Application.Abstractions.Services;
using Hangfire;
using System.Net.Mail;
using System.Net;
using System.Security;
using Baobab.SharedKernel.Application.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Baobab.SharedKernel.Domain.Notifications.Repositories;
using Baobab.SharedKernel.Domain.Notifications;

namespace Baobab.SharedKernel.Infrastructure.Services.NotificationService;

internal class EmailNotificationService
    (ILogger<EmailNotificationService> logger,
    IOptions<EmailSettings> emailSettings,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger = logger;
    private readonly EmailSettings _emailSettings = emailSettings.Value;
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task SendEmailAsync(
       string email,
       string subject,
       string message,
       byte[] attachment = default!,
       string attachmentName = default!)
    {
        _logger.LogInformation("Email sending...");
        var errorMessage = Environment.GetEnvironmentVariable("NOT_DELIVER_EMAIL_COMMENT") ?? _emailSettings.DeliveryDecision.Reason;

        var notification = Notification.Create(email, message, subject);
        notification.SetNotificationType(Domain.Notifications.Enums.NotificationType.Email);

        try
        {
            var securePassword = new SecureString();
            var password = Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_PASSWORD") ?? _emailSettings.SmtpPassword;
            foreach (char c in password) securePassword.AppendChar(c);

            using var client = new SmtpClient
            {
                Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_USER_NAME") ?? _emailSettings.SmtpUsername,
                    securePassword),
                Host = Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_SMTP_HOST_NAME") ?? _emailSettings.SmtpHostName,
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_PORT") ?? _emailSettings.SmtpPort.ToString()),
                EnableSsl = true
            };
            var mailMessage = new MailMessage { From = new MailAddress(Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_SENDER_ADDRESS") ?? _emailSettings.SenderEmail) };
            mailMessage.To.Add(email);
            mailMessage.Subject = $"{Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_SENDER_NAME") ?? _emailSettings.SenderName} - {subject}";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = message;
            if (attachment != null)
            {
                var att = new Attachment(new MemoryStream(attachment), attachmentName, "PDF");
                mailMessage.Attachments.Add(att);
            }

            client.Send(mailMessage);

            _logger.LogInformation($"Email, {subject} Sent to {email}");

            notification.MarkAsDelivered();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Email Sender exception raised");
            errorMessage = ex.Message;
            _logger.LogError(ex, errorMessage);

            notification.MarkAsFailed(errorMessage);
        }

        await AddNotification(notification);
    }

    public Task SendEmailInBackgroundAsync(
        string email,
        string subject,
        string message,
        byte[] attachment = default!,
        string attachmentName = default!)
    {
        return Task.FromResult(
            BackgroundJob.Enqueue(() => SendEmailAsync(
            email,
            subject,
            message,
            attachment,
            attachmentName)));
    }

    private async Task AddNotification(Notification notification)
    {
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }
}
