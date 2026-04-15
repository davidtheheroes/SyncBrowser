using System.Collections.ObjectModel;
using System.Windows;
using SyncBrowser.Core.Enums;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;
using SyncBrowser.Core.Services;

namespace SyncBrowser.App.ViewModels;

/// <summary>
/// Main application ViewModel. Orchestrates browser tabs, sync state,
/// and profile management. Central coordinator for the MVVM pattern.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ProfileManager _profileManager;
    private readonly ISyncMediator _syncMediator;
    private readonly IBrowserFactory _browserFactory;
    private bool _isSyncing;
    private string _syncStatusText = "Sync: OFF";
    private string _navigateAllUrl = "https://www.google.com";
    private int _gridColumns = 2;

    public ObservableCollection<BrowserTabViewModel> BrowserTabs { get; } = new();

    public bool IsSyncing
    {
        get => _isSyncing;
        set
        {
            if (SetProperty(ref _isSyncing, value))
            {
                OnPropertyChanged(nameof(SyncButtonText));
                SyncStatusText = value ? $"Sync: ON ({BrowserTabs.Count(t => !t.IsMaster)} slaves)" : "Sync: OFF";
            }
        }
    }

    public string SyncStatusText
    {
        get => _syncStatusText;
        set => SetProperty(ref _syncStatusText, value);
    }

    public string SyncButtonText => IsSyncing ? "⏹ Stop Sync" : "▶ Start Sync";

    public string NavigateAllUrl
    {
        get => _navigateAllUrl;
        set => SetProperty(ref _navigateAllUrl, value);
    }

    public int GridColumns
    {
        get => _gridColumns;
        set => SetProperty(ref _gridColumns, value);
    }

    // Commands
    public AsyncRelayCommand LoadProfilesCommand { get; }
    public AsyncRelayCommand AddProfileCommand { get; }
    public RelayCommand ToggleSyncCommand { get; }
    public RelayCommand NavigateAllCommand { get; }
    public RelayCommand OpenProfileManagerCommand { get; }
    public RelayCommand OpenExtensionManagerCommand { get; }
    public RelayCommand SetGridColumnsCommand { get; }

    /// <summary>
    /// Raised when the Profile Manager window should be shown.
    /// The View handles the actual window creation.
    /// </summary>
    public event Action? ShowProfileManagerRequested;

    /// <summary>
    /// Raised when the Extension Manager window should be shown.
    /// </summary>
    public event Action? ShowExtensionManagerRequested;

    public MainViewModel(
        ProfileManager profileManager,
        ISyncMediator syncMediator,
        IBrowserFactory browserFactory)
    {
        _profileManager = profileManager;
        _syncMediator = syncMediator;
        _browserFactory = browserFactory;

        _syncMediator.SyncStatusChanged += (_, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSyncing = e.IsSyncing;
            });
        };

        LoadProfilesCommand = new AsyncRelayCommand(LoadProfilesAsync);

        AddProfileCommand = new AsyncRelayCommand(async () =>
        {
            var count = BrowserTabs.Count + 1;
            var profile = await _profileManager.CreateProfileAsync($"Profile {count}");
            var tabVm = CreateTab(profile);
            BrowserTabs.Add(tabVm);
            UpdateGridColumns();
        });

        ToggleSyncCommand = new RelayCommand(_ =>
        {
            if (IsSyncing)
            {
                _syncMediator.StopSync();
            }
            else
            {
                _syncMediator.StartSync();
            }
        });

        NavigateAllCommand = new RelayCommand(_ =>
        {
            if (string.IsNullOrWhiteSpace(NavigateAllUrl)) return;

            var url = NavigateAllUrl;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            foreach (var tab in BrowserTabs)
            {
                tab.CurrentUrl = url;
                tab.NavigateAction?.Invoke(url);
            }
        });

        OpenProfileManagerCommand = new RelayCommand(_ =>
        {
            ShowProfileManagerRequested?.Invoke();
        });

        OpenExtensionManagerCommand = new RelayCommand(_ =>
        {
            ShowExtensionManagerRequested?.Invoke();
        });

        SetGridColumnsCommand = new RelayCommand(param =>
        {
            if (param is string s && int.TryParse(s, out var cols))
            {
                GridColumns = cols;
            }
        });
    }

    /// <summary>
    /// Loads all profiles and creates browser tabs for each.
    /// </summary>
    public async Task LoadProfilesAsync()
    {
        await _profileManager.EnsureDefaultProfilesAsync();
        var profiles = await _profileManager.GetAllProfilesAsync();

        BrowserTabs.Clear();
        _syncMediator.ClearAll();

        bool masterSet = false;
        foreach (var profile in profiles)
        {
            var tabVm = CreateTab(profile);

            if (!masterSet)
            {
                tabVm.IsMaster = true;
                masterSet = true;
            }

            BrowserTabs.Add(tabVm);
        }

        UpdateGridColumns();
    }

    /// <summary>
    /// Registers a WebView2 CoreWebView2 instance for a tab.
    /// Called by the View after WebView2 initialization completes.
    /// </summary>
    public void RegisterWebView(string profileId, object coreWebView2)
    {
        var tab = BrowserTabs.FirstOrDefault(t => t.ProfileId == profileId);
        if (tab == null) return;

        if (tab.IsMaster)
        {
            _syncMediator.SetMaster(profileId, coreWebView2);
        }
        else
        {
            _syncMediator.AddSlave(profileId, coreWebView2);
        }

        tab.Status = ProfileStatus.Active;
    }

    /// <summary>
    /// Removes a browser tab and its associated profile.
    /// </summary>
    public async Task RemoveTabAsync(BrowserTabViewModel tab)
    {
        _syncMediator.RemoveSlave(tab.ProfileId);
        BrowserTabs.Remove(tab);
        await _profileManager.DeleteProfileAsync(tab.ProfileId);
        UpdateGridColumns();
    }

    /// <summary>
    /// Forwards a captured input action from the master to the mediator.
    /// </summary>
    public async Task ForwardInputAsync(InputAction action)
    {
        await _syncMediator.OnInputCapturedAsync(action);
    }

    private BrowserTabViewModel CreateTab(BrowserProfile profile)
    {
        var tabVm = new BrowserTabViewModel(profile);
        tabVm.MasterRequested += OnMasterRequested;
        return tabVm;
    }

    private void OnMasterRequested(BrowserTabViewModel requestedTab)
    {
        // Preserve sync state across master switch
        var wasSyncing = _syncMediator.IsSyncing;

        // Demote current master
        foreach (var tab in BrowserTabs)
        {
            if (tab.IsMaster)
            {
                tab.IsMaster = false;
            }
        }

        // Promote requested tab
        requestedTab.IsMaster = true;

        // Clear mediator so old master's profileId doesn't block AddSlave
        _syncMediator.ClearAll();

        // Re-register all WebViews with new master/slave roles
        ReRegisterAllWebViews?.Invoke();

        // Restore sync state
        if (wasSyncing)
        {
            _syncMediator.StartSync();
        }
    }

    /// <summary>
    /// Raised when all WebView2 instances need to be re-registered
    /// (e.g., after master change).
    /// </summary>
    public event Action? ReRegisterAllWebViews;

    private void UpdateGridColumns()
    {
        GridColumns = BrowserTabs.Count switch
        {
            <= 1 => 1,
            2 => 2,
            <= 4 => 2,
            <= 6 => 3,
            _ => 4
        };
    }
}
