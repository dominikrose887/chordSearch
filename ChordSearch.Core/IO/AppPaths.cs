namespace ChordSearch.Core.IO;

/// <summary>
/// Resolves writable data directories. If <c>portable.txt</c> sits next to the exe,
/// settings/tunings/logs live under <c>{app}\Data</c> (true portable). Otherwise LocalAppData.
/// </summary>
public static class AppPaths
{
    private static string? _root;

    public static bool IsPortable { get; private set; }

    public static string Root
    {
        get
        {
            if (_root is not null) return _root;
            var baseDir = AppContext.BaseDirectory;
            var marker = Path.Combine(baseDir, "portable.txt");
            if (File.Exists(marker))
            {
                IsPortable = true;
                _root = Path.Combine(baseDir, "Data");
            }
            else
            {
                IsPortable = false;
                _root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ChordSearch");
            }

            Directory.CreateDirectory(_root);
            return _root;
        }
    }

    public static string TuningsDirectory => Path.Combine(Root, "tunings");
    public static string LogsDirectory => Path.Combine(Root, "logs");
    public static string SettingsPath => Path.Combine(Root, "settings.json");

    /// <summary>Optional override for tests.</summary>
    public static void ResetForTests(string? root = null)
    {
        _root = root;
        IsPortable = root is not null;
    }
}
