using SyncBrowser.Core.Enums;

namespace SyncBrowser.Core.Models;

/// <summary>
/// Represents an isolated browser profile with its own user data,
/// proxy settings, and user agent configuration.
/// </summary>
public class BrowserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string UserDataFolder { get; set; } = string.Empty;
    public string? ProxyServer { get; set; }
    public string? UserAgent { get; set; }
    public string StartUrl { get; set; } = "https://www.google.com";
    public bool IsMaster { get; set; }
    public ProfileStatus Status { get; set; } = ProfileStatus.Inactive;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Color tag for visual identification in the UI grid.
    /// </summary>
    public string ColorTag { get; set; } = "#6C63FF";
}
