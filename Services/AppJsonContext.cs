using System.Text.Json.Serialization;
using Tempyr.Models;

namespace Tempyr.Services;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(CfWidgetResponse))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext;
