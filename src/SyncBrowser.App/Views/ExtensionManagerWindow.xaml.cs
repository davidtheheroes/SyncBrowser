using System.Windows;
using Microsoft.Win32;
using SyncBrowser.App.ViewModels;

namespace SyncBrowser.App.Views;

public partial class ExtensionManagerWindow : Window
{
    public ExtensionManagerWindow(ExtensionManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.BrowseFolderFunc = () =>
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Extension Folder"
            };

            return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
        };

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
