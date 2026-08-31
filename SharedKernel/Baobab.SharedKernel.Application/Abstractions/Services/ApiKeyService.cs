using Microsoft.AspNetCore.Http;
using System.Text;

namespace Baobab.SharedKernel.Application.Abstractions.Services;

public static class ApiKeyService
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public static string GenerateApiKey(string userName, Guid accountId, string keyName)
    {
        var apiSecretValue = Environment.GetEnvironmentVariable("API_SECRET");
        var apiKeyValue = Environment.GetEnvironmentVariable("API_KEY");
        var secret = $"{apiKeyValue}_{userName}_{Guid.NewGuid()}_{apiSecretValue}_{accountId}_{keyName}";
        byte[] plainSecretBytes = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(plainSecretBytes);
    }

    public static bool IsApiKeyValid(HttpContext context)
    {
        string apiKey = context.Request.Headers[ApiKeyHeaderName]!;
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        var apiKeyValue = Environment.GetEnvironmentVariable("API_KEY");

        var parts = SplitApiKeyParts(apiKey);
        if (parts.Length != 6) return false;

        if (parts[0] != apiKeyValue) return false;

        return true;
    }

    private static string[] SplitApiKeyParts(string apiKey)
    {
        byte[] base64EncodedBytes;
        try
        {
            base64EncodedBytes = Convert.FromBase64String(apiKey);
        }
        catch (FormatException)
        {
            return [];
        }

        var decodedApiKey = Encoding.UTF8.GetString(base64EncodedBytes);

        return decodedApiKey.Split('_');
    }

    public static string GetAccountIdFromApiKey(HttpContext context)
    {
        string apiKey = context.Request.Headers[ApiKeyHeaderName]!;
        if (string.IsNullOrWhiteSpace(apiKey)) return string.Empty;

        var parts = SplitApiKeyParts(apiKey);
        if (parts.Length != 6) return string.Empty;

        return parts[4];
    }
}
