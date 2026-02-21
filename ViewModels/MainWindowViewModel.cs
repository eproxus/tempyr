using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tempyr.Services;

namespace Tempyr.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public InstalledModsViewModel InstalledModsPage { get; }
    public SettingsViewModel SettingsPage { get; }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public bool IsInstalledModsActive => ReferenceEquals(CurrentPage, InstalledModsPage);
    public bool IsSettingsActive => ReferenceEquals(CurrentPage, SettingsPage);

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsInstalledModsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    public MainWindowViewModel()
    {
        var settings = AppSettings.Load();

        // Auto-detect install path if not already saved
        if (!InstallationDetector.IsValidInstall(settings.HytaleInstallPath ?? string.Empty))
        {
            var detected = InstallationDetector.Detect();
            if (detected is not null)
            {
                settings.HytaleInstallPath = detected;
                settings.Save();
            }
        }

        SettingsPage = new SettingsViewModel(settings);
        InstalledModsPage = new InstalledModsViewModel(settings.HytaleInstallPath, settings);

        _currentPage = InstalledModsPage;

        // Run an update check on startup (fire-and-forget)
        InstalledModsPage.CheckForUpdatesCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void NavigateTo(ViewModelBase page) => CurrentPage = page;
}
