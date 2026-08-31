using Microsoft.AspNetCore.Http;
using System.Text;

namespace Baobab.SharedKernel.Application.Abstractions.Services;

public static class ApiSecretService
{
    private const string ApiSecretHeaderName = "X-Api-Secret";

    public static string GenerateApiSecret(string userName)
    {
        var apiSecretValue = Environment.GetEnvironmentVariable("API_SECRET");
        var secret = $"{apiSecretValue}_{userName}_{Guid.NewGuid()}";
        byte[] plainSecretBytes = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(plainSecretBytes);
    }

    public static bool IsApiSecretValid(HttpContext context)
    {
        string apiSecret = context.Request.Headers[ApiSecretHeaderName]!;
        if (string.IsNullOrWhiteSpace(apiSecret)) return false;

        var apiSecretValue = Environment.GetEnvironmentVariable("API_SECRET");

        byte[] base64EncodedBytes;
        try
        {
            base64EncodedBytes = Convert.FromBase64String(apiSecret);
        }
        catch (FormatException)
        {
            return false;
        }

        var decodedApiSecret = Encoding.UTF8.GetString(base64EncodedBytes);

        var parts = decodedApiSecret.Split('_');
        if (parts.Length != 3) return false;

        if (parts[0] != apiSecretValue) return false;

        return true;
    }
}
