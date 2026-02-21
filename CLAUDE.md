# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Tempyr** is a Hytale Mod Manager desktop application built with Avalonia 11 MVVM on .NET 10 (Windows-only).

## Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Publish (single-file, trimmed, self-contained)
dotnet publish -c Release
```

Publish defaults are set in the csproj (`PublishSingleFile`, `SelfContained`, `PublishTrimmed`, etc.) — no extra flags needed.

There are no tests at this time.

## VCS

This project uses **Jujutsu (jj)** colocated with git. Never use raw git commands — always use `jj`.

```bash
# Check status
jj status

# Commit (snapshot the working copy)
jj commit -m "type(scope): description"
```

## Commits

This project uses [Conventional Commits](https://www.conventionalcommits.org/). Format: `type(scope): description`

Common types: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`, `build`

## Architecture

The app follows strict MVVM. Navigation is ViewModel-first: `MainWindowViewModel` owns page VMs (`InstalledModsPage`, `SettingsPage`) and exposes `CurrentPage`. The `ViewLocator` maps ViewModel types → View instances via an explicit dictionary (not reflection). When adding a new page, register it in the `Map` dictionary in `ViewLocator.cs`.

**Data flow for the installed mods list:**

1. `MainWindowViewModel` loads `AppSettings` (JSON at `%AppData%\Tempyr\settings.json`), auto-detects the Hytale install path via `InstallationDetector` if not already saved.
2. `InstalledModsViewModel` calls `ModLoader.LoadFromDirectory(installPath)` which scans `{installPath}\UserData\Mods` for `.jar`/`.zip` files, reads `manifest.json` from inside each archive, and returns `Mod` records.
3. Each `Mod` becomes a `ModItemViewModel`. The remove action is injected as `Action<ModItemViewModel>` (avoids DataContext traversal in compiled bindings).
4. `CheckForUpdatesCommand` fans out to `CfWidgetClient.GetLatestFileAsync` for each mod that has a `CurseForgeSlug`. Update detection compares `latest.FileName` (the archive filename) against `Mod.Id` (the local filename).

**Key constraints:**

- `AvaloniaUseCompiledBindingsByDefault=true` is set in the csproj. This means `x:DataType` is required on every DataTemplate, and `ElementStyle` on `DataGridTextColumn` does **not** work — use `DataGridTemplateColumn` with an inline `TextBlock` instead.
- `DataGrid` requires a separate NuGet package (`Avalonia.Controls.DataGrid`) **and** its theme must be explicitly included in `App.axaml`:
  ```xml
  <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
  ```
- Target is `net10.0-windows` (required for `Microsoft.Win32.Registry`).

**Trimming & single-file publish:**

The csproj sets `PublishTrimmed=true` and `PublishSingleFile=true` by default. This requires trim-safe code:
- **JSON serialization** must use the source-generated `AppJsonContext` (`Services/AppJsonContext.cs`), not `JsonSerializer.Serialize<T>`/`Deserialize<T>`. Add new serialized types as `[JsonSerializable(typeof(...))]` attributes on the context.
- **ViewLocator** uses an explicit type → factory dictionary instead of reflection. New pages must be added to the `Map` in `ViewLocator.cs`.
- Avoid `Type.GetType()`, `Activator.CreateInstance()`, and other reflection patterns that the trimmer cannot analyze.

**Hytale install detection** (`InstallationDetector`):
- Primary: `HKCU\SOFTWARE\Hypixel Studios\Hytale` → `GameInstallPath`
- Fallback: common paths (Program Files, D:\Games\Hytale, etc.)
- Validity: `Directory.Exists(path)` only — no specific executable check
- Launcher location (separate): `HKLM\SOFTWARE\Hypixel Studios\Hytale Launcher` → `InstallDir`

**Update checking** (`CfWidgetClient`):
- Uses CFWidget (`https://api.cfwidget.com/hytale/mods/{slug}`) — no API key required
- Slug is extracted from the mod's `Website` field in `manifest.json` (must be a `curseforge.com/hytale/mods/` URL)
- `UpdateStatus` enum drives per-row display in the DataGrid: `Unknown`, `Checking`, `UpToDate`, `UpdateAvailable`, `NoSource`, `Error`

**Mod manifest schema** (`manifest.json` inside each `.jar`/`.zip`):
```json
{ "Name": "", "Version": "", "Description": "", "Authors": [{"Name": "", "Url": ""}], "Website": "" }
```
Fields are PascalCase. Mods without a `curseforge.com/hytale/mods/` website URL get `UpdateStatus.NoSource`.
