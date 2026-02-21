using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Tempyr.Services;
using Tempyr.ViewModels;

namespace Tempyr.Views;

public partial class InstalledModsView : UserControl
{
    public InstalledModsView()
    {
        InitializeComponent();
        InstallModButton.Click += OnInstallModClick;
    }

    private async void OnInstallModClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InstalledModsViewModel vm) return;

        // Open overlay first (resets URL to empty)
        vm.OpenInstallOverlayCommand.Execute(null);

        // Then prefill from clipboard if it contains a CurseForge URL
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var text = clipboard is not null
                ? await clipboard.TryGetTextAsync()
                : null;
            if (!string.IsNullOrWhiteSpace(text) && ModLoader.ExtractCurseForgeSlug(text) is not null)
                vm.InstallUrl = text.Trim();
        }
        catch
        {
            // Clipboard access can fail — just leave the field empty
        }
    }
}
