using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SyncBrowser.App.ViewModels;
using SyncBrowser.App.Views;
using SyncBrowser.Core.Interfaces;

namespace SyncBrowser.App;

/// <summary>
/// Main application window. Handles initialization of browser tabs
/// and coordinates between Views and ViewModels.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider, IBrowserFactory browserFactory)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _serviceProvider = serviceProvider;

        DataContext = _viewModel;

        // Handle profile manager requests
        _viewModel.ShowProfileManagerRequested += () =>
        {
            var pmVm = _serviceProvider.GetRequiredService<ProfileManagerViewModel>();
            pmVm.ProfilesChanged += async () =>
            {
                await _viewModel.LoadProfilesAsync();
            };

            var window = new ProfileManagerWindow(pmVm) { Owner = this };
            window.ShowDialog();
        };

        // Handle extension manager requests
        _viewModel.ShowExtensionManagerRequested += () =>
        {
            var emVm = _serviceProvider.GetRequiredService<ExtensionManagerViewModel>();
            emVm.ExtensionsChanged += async () =>
            {
                await _viewModel.LoadProfilesAsync();
            };

            var window = new ExtensionManagerWindow(emVm) { Owner = this };
            window.ShowDialog();
        };

        // Handle master re-registration
        _viewModel.ReRegisterAllWebViews += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                var tabViews = FindVisualChildren<BrowserTabView>(BrowserGrid);
                foreach (var tabView in tabViews)
                {
                    tabView.ReRegister();
                }
            });
        };

        Loaded += async (_, _) =>
        {
            await _viewModel.LoadProfilesAsync();
        };
    }

    /// <summary>
    /// Recursively finds all child elements of a given type in the visual tree.
    /// </summary>
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
            {
                yield return t;
            }

            foreach (var grandChild in FindVisualChildren<T>(child))
            {
                yield return grandChild;
            }
        }
    }
}