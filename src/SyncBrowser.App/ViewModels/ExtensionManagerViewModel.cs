using System.Collections.ObjectModel;
using SyncBrowser.Core.Interfaces;

namespace SyncBrowser.App.ViewModels;

/// <summary>
/// ViewModel for the Extension Manager dialog.
/// Manages browser extension folder paths that are loaded into all profiles.
/// </summary>
public class ExtensionManagerViewModel : ViewModelBase
{
    private readonly IExtensionManager _extensionManager;
    private string? _selectedExtension;

    public ObservableCollection<string> Extensions { get; } = new();

    public string? SelectedExtension
    {
        get => _selectedExtension;
        set => SetProperty(ref _selectedExtension, value);
    }

    public AsyncRelayCommand AddCommand { get; }
    public AsyncRelayCommand RemoveCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Raised when the extension list changes so profiles can be reloaded.
    /// </summary>
    public event Action? ExtensionsChanged;

    /// <summary>
    /// The View sets this to a delegate that opens a folder browser dialog
    /// and returns the selected path (or null if cancelled).
    /// </summary>
    public Func<string?>? BrowseFolderFunc { get; set; }

    public ExtensionManagerViewModel(IExtensionManager extensionManager)
    {
        _extensionManager = extensionManager;

        AddCommand = new AsyncRelayCommand(async () =>
        {
            var folderPath = BrowseFolderFunc?.Invoke();
            if (string.IsNullOrWhiteSpace(folderPath)) return;

            await _extensionManager.AddExtensionAsync(folderPath);
            await RefreshAsync();
            ExtensionsChanged?.Invoke();
        });

        RemoveCommand = new AsyncRelayCommand(async () =>
        {
            if (SelectedExtension == null) return;

            await _extensionManager.RemoveExtensionAsync(SelectedExtension);
            await RefreshAsync();
            ExtensionsChanged?.Invoke();
        });

        LoadCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public async Task RefreshAsync()
    {
        var paths = await _extensionManager.GetExtensionPathsAsync();
        Extensions.Clear();
        foreach (var p in paths)
        {
            Extensions.Add(p);
        }
    }
}
