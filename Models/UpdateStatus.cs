namespace Tempyr.Models;

public enum UpdateStatus
{
    Unknown,         // Not yet checked
    Checking,        // Request in flight
    UpToDate,        // Installed file matches latest on CurseForge
    UpdateAvailable, // Newer file exists on CurseForge
    Downloading,     // Update download in progress
    NoSource,        // Mod has no CurseForge URL in manifest
    Error,           // API call failed
}
