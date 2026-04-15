using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using SyncBrowser.App.ViewModels;
using SyncBrowser.Core.Enums;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.App.Views;

/// <summary>
/// Code-behind for BrowserTabView. Bridges the WebView2 control
/// with the ViewModel and handles input capture for master browsers.
/// 
/// Resolves dependencies from the DI container directly since this view
/// is created by a DataTemplate (cannot receive constructor injection).
/// </summary>
public partial class BrowserTabView : UserControl
{
    private BrowserTabViewModel? _viewModel;
    private MainViewModel? _mainViewModel;
    private IBrowserFactory? _browserFactory;
    private IExtensionManager? _extensionManager;
    private bool _isInitialized;

    public BrowserTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = DataContext as BrowserTabViewModel;
        if (_viewModel != null)
        {
            // Bridge ViewModel actions to WebView2 control
            _viewModel.NavigateAction = url =>
            {
                if (WebView.CoreWebView2 != null)
                    WebView.CoreWebView2.Navigate(url);
            };
            _viewModel.ReloadAction = () => WebView.CoreWebView2?.Reload();
            _viewModel.GoBackAction = () => WebView.CoreWebView2?.GoBack();
            _viewModel.GoForwardAction = () => WebView.CoreWebView2?.GoForward();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized || _viewModel == null) return;

        // Resolve dependencies from DI container (since DataTemplate creates us)
        var serviceProvider = ((App)Application.Current).Services;
        _mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
        _browserFactory = serviceProvider.GetRequiredService<IBrowserFactory>();
        _extensionManager = serviceProvider.GetRequiredService<IExtensionManager>();

        _isInitialized = true;

        try
        {
            _viewModel.IsLoading = true;
            _viewModel.Status = ProfileStatus.Loading;

            // Create isolated environment for this profile
            var envObj = await _browserFactory.CreateEnvironmentAsync(_viewModel.Profile);
            var environment = (CoreWebView2Environment)envObj;

            await WebView.EnsureCoreWebView2Async(environment);

            // Load extensions into this profile
            await LoadExtensionsAsync();

            // Configure user agent if specified
            if (!string.IsNullOrEmpty(_viewModel.Profile.UserAgent))
            {
                WebView.CoreWebView2.Settings.UserAgent = _viewModel.Profile.UserAgent;
            }

            // Listen for navigation events
            WebView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _viewModel.CurrentUrl = WebView.CoreWebView2.Source;
                    _viewModel.Title = WebView.CoreWebView2.DocumentTitle;
                    _viewModel.IsLoading = false;
                });
            };

            WebView.CoreWebView2.NavigationStarting += (_, _) =>
            {
                Dispatcher.Invoke(() => _viewModel.IsLoading = true);
            };

            // Register with MainViewModel for sync
            _mainViewModel.RegisterWebView(_viewModel.ProfileId, WebView.CoreWebView2);

            // Set up input capture for master browser
            SetupInputCapture();

            // Navigate to start URL
            WebView.CoreWebView2.Navigate(_viewModel.Profile.StartUrl);
            _viewModel.Status = ProfileStatus.Active;
        }
        catch (Exception ex)
        {
            _viewModel.Status = ProfileStatus.Error;
            _viewModel.IsLoading = false;
            MessageBox.Show($"Failed to initialize browser: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Loads registered extensions into the current WebView2 profile.
    /// Uses the WebView2 Profile API so extensions work without restart.
    /// </summary>
    private async Task LoadExtensionsAsync()
    {
        if (WebView.CoreWebView2 == null || _extensionManager == null) return;

        var extensionPaths = await _extensionManager.GetExtensionPathsAsync();
        var profile = WebView.CoreWebView2.Profile;

        foreach (var path in extensionPaths)
        {
            try
            {
                await profile.AddBrowserExtensionAsync(path);
            }
            catch (Exception)
            {
                // Extension may already be installed or path invalid — skip
            }
        }
    }

    /// <summary>
    /// Injects JavaScript to capture mouse and keyboard events on the master browser
    /// and forwards them to the SyncMediator via the ViewModel.
    /// </summary>
    private void SetupInputCapture()
    {
        if (WebView.CoreWebView2 == null || _viewModel == null) return;

        // Inject input capture script after each navigation
        WebView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            if (!_viewModel.IsMaster) return;
            await InjectCaptureScriptAsync();
        };

        // Handle messages from injected script
        WebView.CoreWebView2.WebMessageReceived += async (_, args) =>
        {
            if (!_viewModel.IsMaster || _mainViewModel == null) return;

            try
            {
                var json = args.TryGetWebMessageAsString();
                if (json == null) return;

                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                InputAction? action = type switch
                {
                    "mouseMove" => new MouseAction
                    {
                        ActionType = InputActionType.MouseMove,
                        X = root.GetProperty("x").GetDouble(),
                        Y = root.GetProperty("y").GetDouble(),
                        // If buttons > 0, we're dragging — send left button for selection
                        Button = root.TryGetProperty("buttons", out var btns) && btns.GetInt32() > 0
                            ? MouseButton.Left : MouseButton.None,
                        ClickCount = 0
                    },

                    "mouseDown" => MouseAction.Press(
                        root.GetProperty("x").GetDouble(),
                        root.GetProperty("y").GetDouble(),
                        MapButton(root.GetProperty("button").GetInt32())),

                    "mouseUp" => MouseAction.Release(
                        root.GetProperty("x").GetDouble(),
                        root.GetProperty("y").GetDouble(),
                        MapButton(root.GetProperty("button").GetInt32())),

                    "keyDown" => KeyboardAction.KeyDown(
                        root.GetProperty("key").GetString() ?? "",
                        root.GetProperty("code").GetString() ?? "",
                        root.GetProperty("keyCode").GetInt32(),
                        root.GetProperty("modifiers").GetInt32()),

                    "keyUp" => KeyboardAction.KeyUp(
                        root.GetProperty("key").GetString() ?? "",
                        root.GetProperty("code").GetString() ?? "",
                        root.GetProperty("keyCode").GetInt32(),
                        root.GetProperty("modifiers").GetInt32()),

                    "scroll" => ScrollAction.Create(
                        root.GetProperty("x").GetDouble(),
                        root.GetProperty("y").GetDouble(),
                        root.GetProperty("deltaX").GetDouble(),
                        root.GetProperty("deltaY").GetDouble()),

                    _ => null
                };

                if (action != null)
                {
                    await _mainViewModel.ForwardInputAsync(action);
                }
            }
            catch
            {
                // Silently ignore malformed messages
            }
        };
    }

    /// <summary>
    /// Re-registers this WebView2 with the mediator (called after master change).
    /// If this tab is now the master, injects the JS capture script immediately.
    /// </summary>
    public async void ReRegister()
    {
        if (WebView.CoreWebView2 != null && _viewModel != null)
        {
            _mainViewModel?.RegisterWebView(_viewModel.ProfileId, WebView.CoreWebView2);

            // If this tab just became master, inject capture script now
            if (_viewModel.IsMaster)
            {
                // Reset the guard flag so script re-injects
                await WebView.CoreWebView2.ExecuteScriptAsync(
                    "window.__syncBrowserCapture = false;");
                await InjectCaptureScriptAsync();
            }
        }
    }

    /// <summary>
    /// Injects the JS input capture script into the current page.
    /// </summary>
    private async Task InjectCaptureScriptAsync()
    {
        if (WebView.CoreWebView2 == null) return;

        await WebView.CoreWebView2.ExecuteScriptAsync(CaptureScript);
    }

    /// <summary>
    /// The JS script that captures all input events and posts them to C#.
    /// Extracted as a constant so it can be reused for re-injection.
    /// </summary>
    private const string CaptureScript = @"
        (function() {
            if (window.__syncBrowserCapture) return;
            window.__syncBrowserCapture = true;

            let _buttons = 0;
            let _lastMoveTime = 0;

            function post(data) {
                window.chrome.webview.postMessage(JSON.stringify(data));
            }

            function getMods(e) {
                return (e.altKey?1:0)|(e.ctrlKey?2:0)|(e.metaKey?4:0)|(e.shiftKey?8:0);
            }

            document.addEventListener('mousemove', e => {
                const now = Date.now();
                if (now - _lastMoveTime < 16) return;
                _lastMoveTime = now;
                post({ type: 'mouseMove', x: e.clientX, y: e.clientY, button: _buttons > 0 ? 0 : -1, buttons: _buttons, modifiers: getMods(e) });
            }, true);

            document.addEventListener('mousedown', e => {
                _buttons |= (1 << e.button);
                post({ type: 'mouseDown', x: e.clientX, y: e.clientY, button: e.button, clickCount: e.detail || 1, modifiers: getMods(e) });
            }, true);

            document.addEventListener('mouseup', e => {
                _buttons &= ~(1 << e.button);
                post({ type: 'mouseUp', x: e.clientX, y: e.clientY, button: e.button, clickCount: 0, modifiers: getMods(e) });
            }, true);

            document.addEventListener('keydown', e => {
                const msg = { type: 'keyDown', key: e.key, code: e.code, keyCode: e.keyCode, modifiers: getMods(e) };
                if (e.key.length === 1 && !e.ctrlKey && !e.metaKey) {
                    msg.text = e.key;
                }
                post(msg);
            }, true);

            document.addEventListener('keyup', e => {
                post({ type: 'keyUp', key: e.key, code: e.code, keyCode: e.keyCode, modifiers: getMods(e) });
            }, true);

            document.addEventListener('wheel', e => {
                post({ type: 'scroll', x: e.clientX, y: e.clientY, deltaX: e.deltaX, deltaY: e.deltaY });
            }, true);
        })();
    ";

    private static MouseButton MapButton(int jsButton) => jsButton switch
    {
        0 => MouseButton.Left,
        1 => MouseButton.Middle,
        2 => MouseButton.Right,
        _ => MouseButton.None
    };

    private void OnCloseTabClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _mainViewModel != null)
        {
            _ = _mainViewModel.RemoveTabAsync(_viewModel);
        }
    }
}
