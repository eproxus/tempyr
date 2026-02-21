using CommunityToolkit.Mvvm.ComponentModel;
using Tempyr.Services;

namespace Tempyr.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallValid))]
    private string _installPath = string.Empty;

    public bool IsInstallValid => InstallationDetector.IsValidInstall(InstallPath);

    public SettingsViewModel(AppSettings settings)
    {
        _settings    = settings;
        _installPath = settings.HytaleInstallPath ?? string.Empty;
    }

    /// <summary>Called from the view's code-behind after a folder picker resolves.</summary>
    public void SetInstallPath(string path)
    {
        InstallPath = path;
        _settings.HytaleInstallPath = path;
        _settings.Save();
    }
}
