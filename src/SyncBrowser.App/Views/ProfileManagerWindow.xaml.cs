using System.Windows;
using SyncBrowser.App.ViewModels;

namespace SyncBrowser.App.Views;

public partial class ProfileManagerWindow : Window
{
    public ProfileManagerWindow(ProfileManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            await viewModel.RefreshAsync();
        };
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
