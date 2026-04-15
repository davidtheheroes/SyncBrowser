using System.Collections.ObjectModel;
using SyncBrowser.Core.Models;
using SyncBrowser.Core.Services;

namespace SyncBrowser.App.ViewModels;

/// <summary>
/// ViewModel for the Profile Manager dialog.
/// Handles CRUD operations for browser profiles.
/// </summary>
public class ProfileManagerViewModel : ViewModelBase
{
    private readonly ProfileManager _profileManager;
    private string _newProfileName = string.Empty;
    private string _newProfileProxy = string.Empty;
    private string _newProfileUserAgent = string.Empty;
    private BrowserProfile? _selectedProfile;

    public ObservableCollection<BrowserProfile> Profiles { get; } = new();

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetProperty(ref _newProfileName, value);
    }

    public string NewProfileProxy
    {
        get => _newProfileProxy;
        set => SetProperty(ref _newProfileProxy, value);
    }

    public string NewProfileUserAgent
    {
        get => _newProfileUserAgent;
        set => SetProperty(ref _newProfileUserAgent, value);
    }

    public BrowserProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public AsyncRelayCommand CreateCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Raised when profiles change and the main window should refresh.
    /// </summary>
    public event Action? ProfilesChanged;

    public ProfileManagerViewModel(ProfileManager profileManager)
    {
        _profileManager = profileManager;

        CreateCommand = new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(NewProfileName)) return;

            await _profileManager.CreateProfileAsync(
                NewProfileName,
                string.IsNullOrWhiteSpace(NewProfileProxy) ? null : NewProfileProxy,
                string.IsNullOrWhiteSpace(NewProfileUserAgent) ? null : NewProfileUserAgent);

            NewProfileName = string.Empty;
            NewProfileProxy = string.Empty;
            NewProfileUserAgent = string.Empty;

            await RefreshAsync();
            ProfilesChanged?.Invoke();
        });

        DeleteCommand = new AsyncRelayCommand(async () =>
        {
            if (SelectedProfile == null) return;

            await _profileManager.DeleteProfileAsync(SelectedProfile.Id);
            await RefreshAsync();
            ProfilesChanged?.Invoke();
        });

        LoadCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public async Task RefreshAsync()
    {
        var profiles = await _profileManager.GetAllProfilesAsync();
        Profiles.Clear();
        foreach (var p in profiles)
        {
            Profiles.Add(p);
        }
    }
}
