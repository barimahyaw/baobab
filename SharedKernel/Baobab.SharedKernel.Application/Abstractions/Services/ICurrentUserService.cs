namespace Baobab.SharedKernel.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    List<KeyValuePair<string, string>> Claims { get; }
    bool IsInRole(string role);
    bool IsInAnyRole(List<string> role);
    string? UserName { get; }
    string? UserRegionId { get; }
    bool IsInZone(string zone);
    List<string> UserZones();
    string Role();
    /// <summary>
    /// The IP address of the current user's client.
    /// </summary>
    string? IpAddress { get; }
    /// <summary>
    /// The User-Agent header of the current request.
    /// </summary>
    string? UserAgent { get; }
    /// <summary>
    /// The distributed trace identifier for the current request.
    /// </summary>
    string? TraceIdentifier { get; }
    /// <summary>
    /// The value of the "Channel" request header.
    /// </summary>
    string? Channel { get; }
    /// <summary>
    /// The value of the "Device-Id" request header.
    /// </summary>
    string? DeviceId { get; }
    /// <summary>
    /// The value of the "App-Version" request header.
    /// </summary>
    string? AppVersion { get; }
    /// <summary>
    /// The value of the "Device-Version" request header.
    /// </summary>
    string? DeviceVersion { get; }
    /// <summary>
    /// The bearer token extracted from the Authorization header, without the "Bearer " prefix.
    /// </summary>
    string? BearerToken { get; }
}
