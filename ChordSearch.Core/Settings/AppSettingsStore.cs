using System.Text.Json;
using ChordSearch.Core.IO;

namespace ChordSearch.Core.Settings;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public AppSettingsStore(string? directory = null)
    {
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "settings.json");
        }
        else
        {
            _path = AppPaths.SettingsPath;
            Directory.CreateDirectory(AppPaths.Root);
        }
    }

    public AppSettings Current { get; private set; } = new();

    /// <summary>Set when Load recovered from a corrupt or unreadable file.</summary>
    public string? LastLoadWarning { get; private set; }

    public void Load()
    {
        LastLoadWarning = null;
        if (!File.Exists(_path))
        {
            Current = new AppSettings();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            Current.Clamp();
        }
        catch (Exception ex)
        {
            LastLoadWarning = $"Corrupt settings.json — using defaults ({ex.GetType().Name}: {ex.Message})";
            Current = new AppSettings();
            try { Save(); }
            catch
            {
                // leave warning; next launch will try again
            }
        }
    }

    public void Save()
    {
        Current.Clamp();
        AtomicFile.WriteAllText(_path, JsonSerializer.Serialize(Current, JsonOptions));
    }
}
