using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Tempyr.ViewModels;

namespace Tempyr.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        BrowseButton.Click += OnBrowseClick;
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } sp) return;

        var results = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Hytale Installation Directory",
            AllowMultiple = false,
        });

        if (results.Count > 0 && DataContext is SettingsViewModel vm)
            vm.SetInstallPath(results[0].Path.LocalPath);
    }
}
