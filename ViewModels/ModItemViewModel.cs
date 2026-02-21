using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tempyr.Models;
using Tempyr.Services;

namespace Tempyr.ViewModels;

public partial class ModItemViewModel : ViewModelBase
{
    public Mod Mod { get; private set; }

    public string Name    => Mod.Name;
    public string Version => Mod.Version;
    public string Authors => string.Join(", ", Mod.Authors);

    // Set by InstalledModsViewModel after a successful update check so this
    // command has the download URL and target filename ready to go.
    internal ModLatestFile? LatestFile { get; set; }

    // Mutable — updated to the new path after a successful install so a
    // subsequent update still points at the right file.
    private string _currentFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdate))]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    private UpdateStatus _updateStatus = UpdateStatus.Unknown;

    [ObservableProperty]
    private string _latestVersion = string.Empty;

    [ObservableProperty]
    private double _downloadProgress;

    public bool HasUpdate    => UpdateStatus == UpdateStatus.UpdateAvailable;
    public bool IsDownloading => UpdateStatus == UpdateStatus.Downloading;

    public ModItemViewModel(Mod mod)
    {
        Mod              = mod;
        _currentFilePath = mod.FilePath;
        _updateStatus    = mod.CurseForgeSlug is null
            ? UpdateStatus.NoSource
            : UpdateStatus.Unknown;
    }

    private void ReloadFromFile(string filePath)
    {
        Mod              = ModLoader.LoadFromFile(filePath);
        _currentFilePath = filePath;
        OnPropertyChanged(nameof(Mod));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(Authors));
    }

    [RelayCommand]
    private async Task UpdateMod(CancellationToken ct)
    {
        if (LatestFile?.DownloadUrl is null) return;

        UpdateStatus     = UpdateStatus.Downloading;
        DownloadProgress = 0;

        var progress = new Progress<double>(p => DownloadProgress = p);
        try
        {
            var newPath = await ModUpdateService.DownloadAndInstallAsync(
                _currentFilePath,
                LatestFile.DownloadUrl,
                LatestFile.FileName,
                progress,
                ct);

            ReloadFromFile(newPath);
            UpdateStatus = UpdateStatus.UpToDate;
        }
        catch (OperationCanceledException)
        {
            UpdateStatus = UpdateStatus.UpdateAvailable; // allow retry
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to update mod '{Name}' from {LatestFile.DownloadUrl}", ex);
            UpdateStatus = UpdateStatus.Error;
        }
        finally
        {
            DownloadProgress = 0;
        }
    }
}
