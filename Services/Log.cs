namespace Tempyr.Services;

/// <summary>
/// Minimal file logger. Appends timestamped entries to %AppData%\Tempyr\tempyr.log.
/// </summary>
public static class Log
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Tempyr", "tempyr.log");

    private static readonly object Lock = new();

    public static void Error(string message, Exception? ex = null)
    {
        var line = ex is null
            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR  {message}"
            : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR  {message}\n  {ex}";
        Append(line);
    }

    public static void Info(string message)
    {
        Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   {message}");
    }

    private static void Append(string line)
    {
        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch { /* logging must never crash the app */ }
    }
}
