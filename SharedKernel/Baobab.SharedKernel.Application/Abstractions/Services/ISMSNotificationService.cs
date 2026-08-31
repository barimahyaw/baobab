namespace Baobab.SharedKernel.Application.Abstractions.Services;

public interface ISMSNotificationService
{
    Task SendSMSAsync(string[] to, string message);
    Task SendSMSInBackgroundAsync(string[] to, string message);
}