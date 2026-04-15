using SyncBrowser.Core.Interfaces;

namespace SyncBrowser.Core.Interfaces;

/// <summary>
/// Manages browser extension paths that are loaded into all profiles.
/// </summary>
public interface IExtensionManager
{
    /// <summary>
    /// Gets all registered extension folder paths.
    /// </summary>
    Task<IReadOnlyList<string>> GetExtensionPathsAsync();

    /// <summary>
    /// Adds an extension folder path.
    /// </summary>
    Task AddExtensionAsync(string folderPath);

    /// <summary>
    /// Removes an extension folder path.
    /// </summary>
    Task RemoveExtensionAsync(string folderPath);
}
