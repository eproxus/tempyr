using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tempyr.Services;

namespace Tempyr.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public InstalledModsViewModel InstalledModsPage { get; }
    public SettingsViewModel SettingsPage { get; }

    public string VersionText { get; } = GetVersionText();

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

    private static string GetVersionText()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (info is null)
            return "v0.1.0-alpha";

        // Strip the +<sha> that .NET appends after InformationalVersion
        // and rebuild as "v0.1.0-alpha (abc1234)"
        var parts = info.Split('+', 2);
        var version = $"v{parts[0]}";
        return parts.Length > 1 ? $"{version} ({parts[1]})" : version;
    }
}
