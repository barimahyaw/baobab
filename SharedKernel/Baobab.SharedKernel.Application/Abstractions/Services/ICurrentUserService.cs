namespace Baobab.SharedKernel.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Ulid UserId { get; }
    List<KeyValuePair<string, string>> Claims { get; }
    bool IsInRole(string role);
    bool IsInAnyRole(List<string> role);
    string? UserName { get; }
    string? UserRegionId { get; }
    bool IsInZone(string zone);
    List<string> UserZones();
    string Role();
    /// <summary>
    /// Get the IP address of the current user.
    /// </summary>
    /// <returns> The IP address as a string, or null if not available.</returns>
    string? IpAddress();
    /// <summary>
    /// Get the user agent of the current user.
    /// </summary>
    /// <returns> The user agent as a string, or null if not available.</returns>
    string? UserAgent();
}
