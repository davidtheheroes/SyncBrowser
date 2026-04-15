using System.Text.Json;
using SyncBrowser.Core.Interfaces;

namespace SyncBrowser.Infrastructure.Repositories;

/// <summary>
/// Persists extension folder paths to a JSON file.
/// Extensions are shared across all profiles.
/// </summary>
public class JsonExtensionManager : IExtensionManager
{
    private readonly string _filePath;
    private List<string> _extensions = new();
    private bool _loaded;

    public JsonExtensionManager()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SyncBrowser");
        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "extensions.json");
    }

    public async Task<IReadOnlyList<string>> GetExtensionPathsAsync()
    {
        await EnsureLoadedAsync();
        return _extensions.AsReadOnly();
    }

    public async Task AddExtensionAsync(string folderPath)
    {
        await EnsureLoadedAsync();

        if (!_extensions.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
        {
            _extensions.Add(folderPath);
            await SaveAsync();
        }
    }

    public async Task RemoveExtensionAsync(string folderPath)
    {
        await EnsureLoadedAsync();

        _extensions.RemoveAll(p => p.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        await SaveAsync();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;

        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _extensions = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_extensions, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_filePath, json);
    }
}
