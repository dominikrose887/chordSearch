using System.Text.Json;
using ChordSearch.Core.IO;

namespace ChordSearch.Core.Tuning;

public sealed class TuningStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _directory;
    private readonly List<GuitarTuning> _userTunings = [];

    public TuningStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChordSearch",
            "tunings");
    }

    /// <summary>Fresh copies of built-in presets — never the shared static instances.</summary>
    public IReadOnlyList<GuitarTuning> Presets => TuningPresets.All.Select(p => p.Copy()).ToArray();

    public IReadOnlyList<GuitarTuning> UserTunings => _userTunings;

    public IEnumerable<GuitarTuning> AllTunings => Presets.Concat(_userTunings.Select(t => t.Copy()));

    public GuitarTuning ActiveTuning { get; private set; } = TuningPresets.StandardE.Copy();

    public void SetActive(GuitarTuning tuning) => ActiveTuning = tuning.Copy();

    public void Load()
    {
        Directory.CreateDirectory(_directory);
        _userTunings.Clear();

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            if (Path.GetFileName(file).Equals("_active.json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var dto = JsonSerializer.Deserialize<TuningDto>(json, JsonOptions);
                if (dto is null || dto.OpenMidiNotes.Length is < 4 or > 8)
                    continue;
                _userTunings.Add(GuitarTuning.FromDto(dto));
            }
            catch
            {
                // skip corrupt files
            }
        }

        var activePath = Path.Combine(_directory, "_active.json");
        if (File.Exists(activePath))
        {
            try
            {
                var dto = JsonSerializer.Deserialize<TuningDto>(File.ReadAllText(activePath), JsonOptions);
                if (dto is not null)
                    ActiveTuning = GuitarTuning.FromDto(dto);
            }
            catch
            {
                ActiveTuning = TuningPresets.StandardE.Copy();
            }
        }
    }

    public void SaveUserTuning(GuitarTuning tuning)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"{tuning.Id}.json");
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(tuning.ToDto(), JsonOptions));

        var idx = _userTunings.FindIndex(t => t.Id == tuning.Id);
        if (idx >= 0) _userTunings[idx] = tuning.Copy();
        else _userTunings.Add(tuning.Copy());
    }

    public void DeleteUserTuning(string id)
    {
        var path = Path.Combine(_directory, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);
        _userTunings.RemoveAll(t => t.Id == id);
    }

    public void SaveActive()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "_active.json");
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(ActiveTuning.ToDto(), JsonOptions));
    }
}
