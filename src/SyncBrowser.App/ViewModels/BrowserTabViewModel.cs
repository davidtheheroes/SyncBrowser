using SyncBrowser.Core.Enums;
using SyncBrowser.Core.Models;

namespace SyncBrowser.App.ViewModels;

/// <summary>
/// ViewModel for a single browser tab. Manages the state of one
/// WebView2 instance and its associated profile.
/// </summary>
public class BrowserTabViewModel : ViewModelBase
{
    private string _currentUrl;
    private string _title = "New Tab";
    private bool _isMaster;
    private bool _isLoading;
    private ProfileStatus _status = ProfileStatus.Inactive;

    public BrowserProfile Profile { get; }

    public string ProfileId => Profile.Id;
    public string ProfileName => Profile.Name;
    public string ColorTag => Profile.ColorTag;

    public string CurrentUrl
    {
        get => _currentUrl;
        set => SetProperty(ref _currentUrl, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsMaster
    {
        get => _isMaster;
        set
        {
            if (SetProperty(ref _isMaster, value))
            {
                Profile.IsMaster = value;
                OnPropertyChanged(nameof(RoleBadge));
                OnPropertyChanged(nameof(BorderColor));
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ProfileStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                Profile.Status = value;
            }
        }
    }

    public string RoleBadge => IsMaster ? "👑 MASTER" : "📡 SLAVE";
    public string BorderColor => IsMaster ? "#FFD700" : ColorTag;

    // Commands
    public RelayCommand NavigateCommand { get; }
    public RelayCommand ReloadCommand { get; }
    public RelayCommand SetAsMasterCommand { get; }
    public RelayCommand GoBackCommand { get; }
    public RelayCommand GoForwardCommand { get; }

    // Actions set by the View (bridge between ViewModel and WebView2 control)
    public Action<string>? NavigateAction { get; set; }
    public Action? ReloadAction { get; set; }
    public Action? GoBackAction { get; set; }
    public Action? GoForwardAction { get; set; }

    /// <summary>
    /// Raised when user requests this tab to become master.
    /// MainViewModel listens to this to coordinate master assignment.
    /// </summary>
    public event Action<BrowserTabViewModel>? MasterRequested;

    public BrowserTabViewModel(BrowserProfile profile)
    {
        Profile = profile;
        _currentUrl = profile.StartUrl;

        NavigateCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrWhiteSpace(CurrentUrl))
            {
                var url = CurrentUrl;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    url = "https://" + url;
                NavigateAction?.Invoke(url);
            }
        });

        ReloadCommand = new RelayCommand(_ => ReloadAction?.Invoke());
        GoBackCommand = new RelayCommand(_ => GoBackAction?.Invoke());
        GoForwardCommand = new RelayCommand(_ => GoForwardAction?.Invoke());

        SetAsMasterCommand = new RelayCommand(_ =>
        {
            MasterRequested?.Invoke(this);
        });
    }
}
