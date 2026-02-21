using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tempyr.Models;
using Tempyr.Services;

namespace Tempyr.ViewModels;

public partial class InstalledModsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private readonly string?     _installPath;

    public ObservableCollection<ModItemViewModel> Mods { get; } = [];

    [ObservableProperty] private ModItemViewModel? _selectedMod;
    [ObservableProperty] private bool              _isCheckingUpdates;
    [ObservableProperty] private string            _toastMessage  = string.Empty;
    [ObservableProperty] private bool              _isToastVisible;

    // Install-from-URL overlay state
    [ObservableProperty] private bool   _isInstallOverlayOpen;
    [ObservableProperty] private string _installUrl           = string.Empty;
    [ObservableProperty] private string _installStatusMessage = string.Empty;
    [ObservableProperty] private bool   _isInstalling;
    [ObservableProperty] private double _installProgress;

    public InstalledModsViewModel(string? installPath, AppSettings settings)
    {
        _settings    = settings;
        _installPath = installPath;

        RefreshModList();
    }

    private void RefreshModList()
    {
        Mods.Clear();
        SelectedMod = null;

        if (string.IsNullOrWhiteSpace(_installPath)) return;

        foreach (var mod in ModLoader.LoadFromDirectory(_installPath))
            Mods.Add(new ModItemViewModel(mod));
    }

    [RelayCommand]
    private async Task CheckForUpdates(CancellationToken ct)
    {
        IsCheckingUpdates = true;
        ShowToast("Checking for updates…");

        RefreshModList();

        var targets = Mods.Where(m => m.Mod.CurseForgeSlug is not null).ToList();
        await Task.WhenAll(targets.Select(m => CheckModAsync(m, ct)));

        var updates = targets.Count(m => m.UpdateStatus == UpdateStatus.UpdateAvailable);
        var errors  = targets.Count(m => m.UpdateStatus == UpdateStatus.Error);

        ShowToast(updates > 0
            ? $"{updates} update(s) available."
            : errors > 0
                ? $"Check complete — {errors} mod(s) could not be reached."
                : "All mods are up to date.");

        IsCheckingUpdates = false;
    }

    [RelayCommand]
    private async Task UpdateAll(CancellationToken ct)
    {
        var targets = Mods.Where(m => m.HasUpdate).ToList();
        if (targets.Count == 0) return;

        ShowToast($"Updating {targets.Count} mod(s)…");
        await Task.WhenAll(targets.Select(m => m.UpdateModCommand.ExecuteAsync(null)));

        var succeeded = targets.Count(m => m.UpdateStatus == UpdateStatus.UpToDate);
        var failed    = targets.Count(m => m.UpdateStatus == UpdateStatus.Error);

        ShowToast(failed > 0
            ? $"Updated {succeeded}/{targets.Count} mod(s). {failed} failed."
            : $"Successfully updated {succeeded} mod(s).");
    }

    [RelayCommand]
    private void DismissToast() => IsToastVisible = false;

    private void ShowToast(string message)
    {
        ToastMessage   = message;
        IsToastVisible = true;
    }

    private static async Task CheckModAsync(ModItemViewModel vm, CancellationToken ct)
    {
        vm.UpdateStatus = UpdateStatus.Checking;
        try
        {
            var latest = await CfWidgetClient.GetLatestFileAsync(vm.Mod.CurseForgeSlug!, ct);
            if (latest is null)
            {
                vm.UpdateStatus = vm.Mod.CurseForgeSlugIsGuessed ? UpdateStatus.NoSource : UpdateStatus.Error;
                return;
            }

            // Parse the version from the archive filename (e.g. "my-mod-1.0.0.jar" → "1.0.0")
            // so the UI shows a clean version string without the mod name.
            var latestVersion = ParseVersionFromFilename(latest.FileName) ?? latest.DisplayName;
            vm.LatestVersion  = latestVersion;

            var filenameMatch = string.Equals(latest.FileName, vm.Mod.Id, StringComparison.OrdinalIgnoreCase);

            // Compare versions parsed from both filenames — more reliable than comparing against
            // the manifest Version field, which may be empty or formatted differently.
            var localVersion = ParseVersionFromFilename(vm.Mod.Id);
            var versionMatch = localVersion is not null &&
                               string.Equals(
                                   latestVersion.TrimStart('v', 'V'),
                                   localVersion.TrimStart('v', 'V'),
                                   StringComparison.OrdinalIgnoreCase);

            vm.UpdateStatus = filenameMatch || versionMatch
                ? UpdateStatus.UpToDate
                : UpdateStatus.UpdateAvailable;

            // Keep the latest-file info on the VM so UpdateModCommand has the
            // download URL and target filename ready without a second API call.
            if (vm.UpdateStatus == UpdateStatus.UpdateAvailable)
                vm.LatestFile = latest;
        }
        catch (OperationCanceledException) { vm.UpdateStatus = UpdateStatus.Unknown; }
        catch (Exception ex)
        {
            Log.Error($"Update check failed for mod '{vm.Name}' (slug: {vm.Mod.CurseForgeSlug})", ex);
            vm.UpdateStatus = vm.Mod.CurseForgeSlugIsGuessed ? UpdateStatus.NoSource : UpdateStatus.Error;
        }
    }

    [GeneratedRegex(@"[\-_ ]v?(?<ver>\d+[\d.\-]*)(?:[\-_ ].*)?$")]
    private static partial Regex FileVersionRegex();

    private static string? ParseVersionFromFilename(string filename)
    {
        var stem  = Path.GetFileNameWithoutExtension(filename);
        var match = FileVersionRegex().Match(stem);
        return match.Success ? match.Groups["ver"].Value : null;
    }

    // ── Install-from-URL overlay ────────────────────────────────────────────

    [RelayCommand]
    private void OpenInstallOverlay()
    {
        InstallUrl           = string.Empty;
        InstallStatusMessage = string.Empty;
        InstallProgress      = 0;
        IsInstalling         = false;
        IsInstallOverlayOpen = true;
    }

    [RelayCommand]
    private void CloseInstallOverlay() => IsInstallOverlayOpen = false;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task InstallMod(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_installPath))
        {
            InstallStatusMessage = "Hytale install path is not configured.";
            return;
        }

        var slug = ModLoader.ExtractCurseForgeSlug(InstallUrl);
        if (slug is null)
        {
            InstallStatusMessage = "Not a valid CurseForge mod URL.";
            return;
        }

        IsInstalling         = true;
        InstallStatusMessage = "Fetching mod info…";
        InstallProgress      = 0;

        try
        {
            var latest = await CfWidgetClient.GetLatestFileAsync(slug, ct);
            if (latest is null || string.IsNullOrEmpty(latest.DownloadUrl))
            {
                InstallStatusMessage = "Could not find a downloadable file for this mod.";
                IsInstalling = false;
                return;
            }

            var modsDir = Path.Combine(_installPath, "UserData", "Mods");
            var destPath = Path.Combine(modsDir, latest.FileName);
            if (File.Exists(destPath))
            {
                InstallStatusMessage = $"\"{latest.FileName}\" is already installed.";
                IsInstalling = false;
                return;
            }

            InstallStatusMessage = "Downloading…";
            var progress = new Progress<double>(p => InstallProgress = p);
            await ModUpdateService.DownloadNewModAsync(modsDir, latest.DownloadUrl, latest.FileName, progress, ct);

            RefreshModList();
            IsInstallOverlayOpen = false;
            ShowToast($"Installed \"{latest.FileName}\" successfully.");
        }
        catch (OperationCanceledException)
        {
            InstallStatusMessage = "Cancelled.";
        }
        catch (Exception ex)
        {
            Log.Error("Mod install failed", ex);
            InstallStatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

}
