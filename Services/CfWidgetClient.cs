using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Tempyr.Services;

/// <summary>
/// Keyless CurseForge data via CFWidget (https://cfwidget.com).
/// Uses a shared HttpClient — no disposal needed.
/// </summary>
public static class CfWidgetClient
{
    private const string BaseUrl = "https://api.cfwidget.com";

    private static readonly HttpClient Http = new();

    public static async Task<ModLatestFile?> GetLatestFileAsync(
        string cfSlug, CancellationToken ct = default)
    {
        var url      = $"{BaseUrl}/hytale/mods/{Uri.EscapeDataString(cfSlug)}";
        var response = await Http.GetFromJsonAsync<CfWidgetResponse>(url, ct);
        var file     = response?.Download;
        if (file is null) return null;

        var downloadUrl = file.Id > 0
            ? BuildCdnUrl(file.Id, file.Name)
            : file.Url;

        return new ModLatestFile(file.Name, file.Display, downloadUrl);
    }

    /// <summary>
    /// CurseForge CDN URL: split the file ID into first-4-digits / remainder.
    /// e.g. file 7649813 → https://mediafilez.forgecdn.net/files/7649/813/Mod.jar
    /// </summary>
    private static string BuildCdnUrl(long fileId, string fileName)
    {
        var id = fileId.ToString();
        var part1 = id[..4];
        var part2 = id[4..];
        return $"https://mediafilez.forgecdn.net/files/{part1}/{part2}/{fileName}";
    }

    // ── Private JSON models ───────────────────────────────────────────────────

    private class CfWidgetResponse
    {
        [JsonPropertyName("download")] public CfWidgetFile? Download { get; init; }
    }

    private class CfWidgetFile
    {
        [JsonPropertyName("id")]      public long    Id      { get; init; }
        [JsonPropertyName("name")]    public string  Name    { get; init; } = string.Empty;
        [JsonPropertyName("display")] public string  Display { get; init; } = string.Empty;
        [JsonPropertyName("url")]     public string? Url     { get; init; }
    }
}

public record ModLatestFile(string FileName, string DisplayName, string? DownloadUrl);
