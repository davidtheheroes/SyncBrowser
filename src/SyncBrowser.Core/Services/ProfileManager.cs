using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Services;

/// <summary>
/// Manages browser profile lifecycle: create, update, delete.
/// Auto-generates isolated user data folder paths.
/// </summary>
public class ProfileManager
{
    private readonly IProfileRepository _repository;
    private readonly string _profilesBaseDir;

    /// <summary>
    /// Predefined color palette for new profiles.
    /// </summary>
    private static readonly string[] ColorPalette =
    [
        "#6C63FF", "#FF6584", "#43E97B", "#F5A623",
        "#00D2FF", "#FF4757", "#7BED9F", "#E056D0",
        "#1DD1A1", "#FF9FF3", "#48DBFB", "#FD7272"
    ];

    public ProfileManager(IProfileRepository repository)
    {
        _repository = repository;
        _profilesBaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SyncBrowser", "Profiles");
    }

    public async Task<IReadOnlyList<BrowserProfile>> GetAllProfilesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<BrowserProfile> CreateProfileAsync(string name, string? proxy = null, string? userAgent = null)
    {
        var profiles = await _repository.GetAllAsync();

        // Validate unique name
        if (profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Profile with name '{name}' already exists.");
        }

        var profile = new BrowserProfile
        {
            Name = name,
            ProxyServer = proxy,
            UserAgent = userAgent,
            ColorTag = ColorPalette[profiles.Count % ColorPalette.Length]
        };

        // Auto-generate isolated user data folder
        profile.UserDataFolder = Path.Combine(_profilesBaseDir, profile.Id);
        Directory.CreateDirectory(profile.UserDataFolder);

        await _repository.SaveAsync(profile);
        return profile;
    }

    public async Task UpdateProfileAsync(BrowserProfile profile)
    {
        await _repository.SaveAsync(profile);
    }

    public async Task DeleteProfileAsync(string id)
    {
        var profile = await _repository.GetByIdAsync(id);
        if (profile != null && Directory.Exists(profile.UserDataFolder))
        {
            try
            {
                Directory.Delete(profile.UserDataFolder, recursive: true);
            }
            catch (IOException)
            {
                // Profile folder may be locked by WebView2; will be cleaned up later
            }
        }

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// Creates a set of default profiles if none exist.
    /// </summary>
    public async Task EnsureDefaultProfilesAsync(int count = 2)
    {
        var profiles = await _repository.GetAllAsync();
        if (profiles.Count > 0) return;

        for (int i = 1; i <= count; i++)
        {
            await CreateProfileAsync($"Profile {i}");
        }
    }
}
