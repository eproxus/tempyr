using System.Text.Json.Serialization;

namespace Tempyr.Models;

public class ModManifest
{
    [JsonPropertyName("Name")]        public string?              Name        { get; init; }
    [JsonPropertyName("Version")]     public string?              Version     { get; init; }
    [JsonPropertyName("Description")] public string?              Description { get; init; }
    [JsonPropertyName("Authors")]     public List<ModAuthorEntry> Authors     { get; init; } = [];
    [JsonPropertyName("Website")]     public string?              Website     { get; init; }
    [JsonPropertyName("Group")]       public string?              Group       { get; init; }
}

public class ModAuthorEntry
{
    [JsonPropertyName("Name")] public string? Name { get; init; }
    [JsonPropertyName("Url")]  public string? Url  { get; init; }
}
