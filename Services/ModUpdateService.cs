namespace Tempyr.Services;

/// <summary>
/// Downloads a mod update, backs up the existing file to an Archive sub-folder,
/// then installs the new file in its place.
/// </summary>
public static class ModUpdateService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Tempyr/1.0");
        return client;
    }

    /// <summary>
    /// Downloads the new version of a mod, archives the old file, and installs the
    /// new one. Returns the path to the newly installed file.
    /// </summary>
    /// <param name="existingFilePath">Full path to the currently installed .jar/.zip.</param>
    /// <param name="downloadUrl">Direct download URL for the new version.</param>
    /// <param name="newFileName">Filename to give the installed file (e.g. from CFWidget).</param>
    /// <param name="progress">Optional 0.0–1.0 download progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full path to the newly installed file.</returns>
    public static async Task<string> DownloadAndInstallAsync(
        string            existingFilePath,
        string            downloadUrl,
        string            newFileName,
        IProgress<double>? progress = null,
        CancellationToken ct        = default)
    {
        var modsDir    = Path.GetDirectoryName(existingFilePath)
                         ?? throw new ArgumentException("Cannot determine mods directory.", nameof(existingFilePath));
        var archiveDir = Path.Combine(modsDir, "Archive");
        Directory.CreateDirectory(archiveDir);

        // 1. Download to a temp file so the original is untouched on failure.
        var tempPath = Path.Combine(modsDir, newFileName + ".tmp");
        try
        {
            await DownloadFileAsync(downloadUrl, tempPath, progress, ct);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }

        // 2. Archive the old file (overwrite a previous backup of the same version).
        var archiveDest = Path.Combine(archiveDir, Path.GetFileName(existingFilePath));
        File.Copy(existingFilePath, archiveDest, overwrite: true);

        // 3. Install the new file first, then clean up the old one.
        //    This order means a failure in Move leaves the original untouched,
        //    while a failure in Delete just leaves an orphan (no data loss).
        var newFilePath = Path.Combine(modsDir, newFileName);
        File.Move(tempPath, newFilePath, overwrite: true);

        // Only delete the old file when it has a different name — if they
        // match, the Move above already overwrote it.
        if (!string.Equals(existingFilePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            File.Delete(existingFilePath);

        return newFilePath;
    }

    /// <summary>
    /// Downloads a new mod (fresh install, no backup/archive step).
    /// Returns the path to the installed file.
    /// </summary>
    public static async Task<string> DownloadNewModAsync(
        string             modsDirectory,
        string             downloadUrl,
        string             fileName,
        IProgress<double>? progress = null,
        CancellationToken  ct       = default)
    {
        Directory.CreateDirectory(modsDirectory);

        var destPath = Path.Combine(modsDirectory, fileName);
        var tempPath = destPath + ".tmp";
        try
        {
            await DownloadFileAsync(downloadUrl, tempPath, progress, ct);
            File.Move(tempPath, destPath, overwrite: false);
            return destPath;
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }

    private static async Task DownloadFileAsync(
        string             url,
        string             destPath,
        IProgress<double>? progress,
        CancellationToken  ct)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;

        await using var dest   = File.Create(destPath);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        var  buffer = new byte[81_920];
        long read   = 0;
        int  chunk;

        while ((chunk = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, chunk), ct);
            read += chunk;
            if (total > 0) progress?.Report((double)read / total);
        }

        // If Content-Length was absent we can only report completion at the end.
        if (total <= 0) progress?.Report(1.0);
    }
}
