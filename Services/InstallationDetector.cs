using Microsoft.Win32;

namespace Tempyr.Services;

/// <summary>
/// Tries to locate the Hytale game installation directory using several strategies,
/// in priority order: registry (game key) → common paths.
/// </summary>
public static class InstallationDetector
{
    private static readonly string[] CommonPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),         "Hytale"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),      "Hytale"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Hytale"),
        Path.Combine("D:", "Games", "Hytale"),
        Path.Combine("C:", "Games", "Hytale"),
    ];

    /// <summary>
    /// Runs all detection strategies and returns the first valid game path found, or null.
    /// Does NOT check persisted settings — that's the caller's responsibility.
    /// </summary>
    public static string? Detect() =>
        TryGameRegistryKey() ?? TryCommonPaths();

    /// <summary>
    /// A path is valid if the directory exists. The registry value is authoritative;
    /// common-path fallback also just requires the directory to be present.
    /// </summary>
    public static bool IsValidInstall(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

    // ── Strategies ────────────────────────────────────────────────────────────

    /// <summary>
    /// Primary source: HKCU\SOFTWARE\Hypixel Studios\Hytale → GameInstallPath.
    /// Written by the Hytale launcher when the user installs the game. Trusted as-is.
    /// </summary>
    private static string? TryGameRegistryKey()
    {
        var path = TryReadRegistry(
            Registry.CurrentUser,
            @"SOFTWARE\Hypixel Studios\Hytale",
            "GameInstallPath");

        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static string? TryReadRegistry(RegistryKey hive, string subKey, string valueName)
    {
        try
        {
            using var key = hive.OpenSubKey(subKey);
            return key?.GetValue(valueName) as string;
        }
        catch { return null; }
    }

    private static string? TryCommonPaths() =>
        CommonPaths.FirstOrDefault(IsValidInstall);

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Hytale Launcher install directory separately from the game.
    /// Useful for launching the launcher process.
    /// </summary>
    public static string? GetLauncherDirectory() =>
        TryReadRegistry(
            Registry.LocalMachine,
            @"SOFTWARE\Hypixel Studios\Hytale Launcher",
            "InstallDir");
}
