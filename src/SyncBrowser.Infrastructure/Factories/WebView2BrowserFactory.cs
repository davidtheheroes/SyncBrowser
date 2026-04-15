using Microsoft.Web.WebView2.Core;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Infrastructure.Factories;

/// <summary>
/// Factory pattern implementation: creates isolated CoreWebView2Environment
/// instances per profile, each with its own user data folder.
/// </summary>
public class WebView2BrowserFactory : IBrowserFactory
{
    public async Task<object> CreateEnvironmentAsync(BrowserProfile profile)
    {
        Directory.CreateDirectory(profile.UserDataFolder);

        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = true
        };

        if (!string.IsNullOrEmpty(profile.ProxyServer))
        {
            options.AdditionalBrowserArguments = $"--proxy-server={profile.ProxyServer}";
        }

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: profile.UserDataFolder,
            options: options);

        return environment;
    }

    public Task CleanupAsync(BrowserProfile profile)
    {
        if (Directory.Exists(profile.UserDataFolder))
        {
            try
            {
                Directory.Delete(profile.UserDataFolder, recursive: true);
            }
            catch (IOException)
            {
                // Folder may be locked; schedule for cleanup later
            }
        }

        return Task.CompletedTask;
    }
}
