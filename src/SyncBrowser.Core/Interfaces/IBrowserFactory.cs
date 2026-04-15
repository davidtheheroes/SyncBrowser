using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Interfaces;

/// <summary>
/// Factory pattern: creates isolated WebView2 environments per profile.
/// Returns object to keep Core layer framework-agnostic.
/// </summary>
public interface IBrowserFactory
{
    /// <summary>
    /// Creates an isolated browser environment for the given profile.
    /// Returns a CoreWebView2Environment instance (typed as object).
    /// </summary>
    Task<object> CreateEnvironmentAsync(BrowserProfile profile);

    /// <summary>
    /// Cleans up data folder for a profile.
    /// </summary>
    Task CleanupAsync(BrowserProfile profile);
}
