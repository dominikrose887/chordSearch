using System.Diagnostics;
using System.Text;

namespace ChordSearch.App.Diagnostics;

/// <summary>Simple append-only file + Debug logger for crash diagnosis.</summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static string? _path;
    private static bool _initialized;

    public static string LogDirectory { get; private set; } = "";
    public static string LogFilePath => _path ?? "";

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var localLogs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChordSearch",
            "logs");
        Directory.CreateDirectory(localLogs);

        LogDirectory = localLogs;
#if DEBUG
        // Dev convenience: also write under the repo when present.
        try
        {
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            if (repoRoot is not null)
            {
                var repoLogs = Path.Combine(repoRoot, "logs");
                Directory.CreateDirectory(repoLogs);
                if (IsWritable(repoLogs))
                    LogDirectory = repoLogs;
            }
        }
        catch
        {
            // keep LocalAppData
        }
#endif

        PruneOldLogs(LogDirectory, keep: 20);
        if (!string.Equals(LogDirectory, localLogs, StringComparison.OrdinalIgnoreCase))
            PruneOldLogs(localLogs, keep: 20);

        _path = Path.Combine(LogDirectory, $"chordsearch-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        File.WriteAllText(Path.Combine(LogDirectory, "latest.txt"), _path);
        try { File.WriteAllText(Path.Combine(localLogs, "latest.txt"), _path); }
        catch { /* ignore */ }

        Info("=== ChordSearch session start ===");
        Info($"PID={Environment.ProcessId} OS={Environment.OSVersion} 64bit={Environment.Is64BitProcess}");
        Info($"Log file: {_path}");
    }

    private static void PruneOldLogs(string directory, int keep)
    {
        try
        {
            if (!Directory.Exists(directory)) return;
            var old = Directory.EnumerateFiles(directory, "chordsearch-*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(keep);
            foreach (var file in old)
            {
                try { file.Delete(); } catch { /* ignore locked/in-use */ }
            }
        }
        catch
        {
            // never fail startup over log cleanup
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null)
    {
        var sb = new StringBuilder();
        sb.Append(message);
        if (ex is not null)
        {
            sb.AppendLine();
            sb.Append(ex.GetType().FullName).Append(": ").Append(ex.Message);
            sb.AppendLine();
            sb.Append(ex.StackTrace);
            if (ex.InnerException is not null)
            {
                sb.AppendLine();
                sb.Append("Inner: ").Append(ex.InnerException.GetType().FullName)
                    .Append(": ").Append(ex.InnerException.Message);
                sb.AppendLine();
                sb.Append(ex.InnerException.StackTrace);
            }
        }
        Write("ERROR", sb.ToString());
    }

    public static void Fatal(string message, Exception? ex = null)
    {
        Error("FATAL: " + message, ex);
        try { Flush(); } catch { /* ignore */ }
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] [T{Environment.CurrentManagedThreadId}] {message}";
        lock (Gate)
        {
            try
            {
                if (_path is not null)
                    File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                // never throw from logger
            }
        }

        Debug.WriteLine(line);
        try { Console.WriteLine(line); } catch { /* no console */ }
    }

    public static void Flush()
    {
        // AppendAllText already flushes; marker for readability
        Write("INFO", "--- flush ---");
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, ".write-probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ChordSearch.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "ChordSearch.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
