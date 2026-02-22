using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tempyr.Models;

namespace Tempyr.Services;

public static partial class ModLoader
{
    private static readonly string[] SupportedExtensions = [".jar", ".zip"];

    // Fallback filename parser — used only when no manifest.json is present.
    [GeneratedRegex(@"^(?<name>.+?)[\-_ ]v?(?<version>\d+[\d.\-]*)(\s.*)?$", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();

    // Extracts the mod slug from a CurseForge URL.
    [GeneratedRegex(@"curseforge\.com/hytale/mods/(?<slug>[^/?#]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CurseForgeSlugRegex();

    public static IReadOnlyList<Mod> LoadFromDirectory(string installPath)
    {
        var modsPath = Path.Combine(installPath, "UserData", "Mods");
        if (!Directory.Exists(modsPath))
            return [];

        return Directory
            .EnumerateFiles(modsPath)
            .Where(f => SupportedExtensions.Contains(
                Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .Select(ParseModFile)
            .ToList();
    }

    public static Mod LoadFromFile(string filePath) => ParseModFile(filePath);

    private static Mod ParseModFile(string filePath)
    {
        var manifest = TryReadManifest(filePath);

        // Prefer filename-parsed version (manifest versions are often placeholders);
        // fall back to manifest for other fields.
        var (fallbackName, fallbackVersion) = ParseStem(Path.GetFileNameWithoutExtension(filePath));

        var authors = manifest?.Authors
            .Select(a => a.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .ToList() ?? [];

        var website  = manifest?.Website ?? string.Empty;
        var cfSlug   = ExtractCurseForgeSlug(website);
        var modName  = manifest?.Name ?? fallbackName;
        var slugGuessed = false;
        if (cfSlug is null)
        {
            cfSlug      = SlugFromName(modName);
            slugGuessed = cfSlug is not null;
        }

        return new Mod
        {
            Id                      = Path.GetFileName(filePath),
            Name                    = modName,
            Version                 = fallbackVersion.Length > 0
                                        ? fallbackVersion
                                        : manifest?.Version ?? string.Empty,
            Description             = manifest?.Description ?? string.Empty,
            Website                 = website,
            CurseForgeSlug          = cfSlug,
            CurseForgeSlugIsGuessed = slugGuessed,
            Authors        = authors,
            FilePath       = filePath,
            InstalledAt    = File.GetLastWriteTime(filePath),
        };
    }

    private static ModManifest? TryReadManifest(string filePath)
    {
        try
        {
            using var zip   = ZipFile.OpenRead(filePath);
            var       entry = zip.Entries.FirstOrDefault(e =>
                e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
            if (entry is null) return null;

            using var stream = entry.Open();
            return JsonSerializer.Deserialize(stream, AppJsonContext.Default.ModManifest);
        }
        catch { return null; }
    }

    private static (string Name, string Version) ParseStem(string stem)
    {
        var match = VersionRegex().Match(stem);
        if (match.Success)
            return (Humanize(match.Groups["name"].Value), match.Groups["version"].Value);

        return (Humanize(stem), string.Empty);
    }

    private static string Humanize(string s) =>
        s.Replace('_', ' ').Replace('-', ' ').Trim();

    internal static string? ExtractCurseForgeSlug(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = CurseForgeSlugRegex().Match(url);
        return m.Success ? m.Groups["slug"].Value : null;
    }

    // Converts a mod name to a kebab-case slug as a last resort
    // e.g. "Violet's Music Players" → "violets-music-players"
    private static string? SlugFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var sb          = new System.Text.StringBuilder();
        bool prevHyphen = true; // suppress leading hyphens

        foreach (char c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                prevHyphen = false;
            }
            else if (!prevHyphen && (c is ' ' or '-' or '_'))
            {
                sb.Append('-');
                prevHyphen = true;
            }
        }

        // Trim trailing hyphen
        if (sb.Length > 0 && sb[^1] == '-')
            sb.Length--;

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
