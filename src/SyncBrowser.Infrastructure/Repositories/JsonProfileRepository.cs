using System.Text.Json;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Infrastructure.Repositories;

/// <summary>
/// Repository pattern implementation: persists browser profiles to a JSON file.
/// Thread-safe with SemaphoreSlim.
/// </summary>
public class JsonProfileRepository : IProfileRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonProfileRepository()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SyncBrowser");
        Directory.CreateDirectory(appDataDir);
        _filePath = Path.Combine(appDataDir, "profiles.json");
    }

    public async Task<IReadOnlyList<BrowserProfile>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<BrowserProfile>();

            var json = await File.ReadAllTextAsync(_filePath);
            var profiles = JsonSerializer.Deserialize<List<BrowserProfile>>(json, JsonOptions);
            return profiles?.AsReadOnly() ?? (IReadOnlyList<BrowserProfile>)Array.Empty<BrowserProfile>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<BrowserProfile?> GetByIdAsync(string id)
    {
        var profiles = await GetAllAsync();
        return profiles.FirstOrDefault(p => p.Id == id);
    }

    public async Task SaveAsync(BrowserProfile profile)
    {
        await _lock.WaitAsync();
        try
        {
            var profiles = await LoadUnsafeAsync();
            var existing = profiles.FindIndex(p => p.Id == profile.Id);

            if (existing >= 0)
                profiles[existing] = profile;
            else
                profiles.Add(profile);

            await PersistUnsafeAsync(profiles);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var profiles = await LoadUnsafeAsync();
            profiles.RemoveAll(p => p.Id == id);
            await PersistUnsafeAsync(profiles);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAllAsync(IEnumerable<BrowserProfile> profiles)
    {
        await _lock.WaitAsync();
        try
        {
            await PersistUnsafeAsync(profiles.ToList());
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads profiles without acquiring the lock (caller must hold lock).
    /// </summary>
    private async Task<List<BrowserProfile>> LoadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
            return new List<BrowserProfile>();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<BrowserProfile>>(json, JsonOptions)
               ?? new List<BrowserProfile>();
    }

    /// <summary>
    /// Persists profiles without acquiring the lock (caller must hold lock).
    /// </summary>
    private async Task PersistUnsafeAsync(List<BrowserProfile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
