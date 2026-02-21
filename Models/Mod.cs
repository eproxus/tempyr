namespace Tempyr.Models;

public class Mod
{
    public string                Id          { get; init; } = string.Empty;
    public string                Name        { get; init; } = string.Empty;
    public string                Version     { get; init; } = string.Empty;
    public string                Description { get; init; } = string.Empty;
    public string                Website          { get; init; } = string.Empty;
    public string?               CurseForgeSlug        { get; init; }
    public bool                  CurseForgeSlugIsGuessed { get; init; }
    public IReadOnlyList<string> Authors          { get; init; } = [];
    public DateTime              InstalledAt { get; init; }
    public string                FilePath    { get; init; } = string.Empty;
}
