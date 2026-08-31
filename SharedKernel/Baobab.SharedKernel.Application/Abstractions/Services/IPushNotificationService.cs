namespace Baobab.SharedKernel.Application.Abstractions.Services;

public interface IPushNotificationService
{
    Task PublishNotificationAsync(string[] to, string message, string productId);
}
