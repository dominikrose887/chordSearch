using ChordSearch.Core.Music;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Core.Tests;

public class TuningStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly TuningStore _store;

    public TuningStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ChordSearchTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _store = new TuningStore(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void Load_EmptyDir_KeepsStandardDefault()
    {
        _store.Load();
        Assert.Equal(6, _store.ActiveTuning.StringCount);
        Assert.Empty(_store.UserTunings);
        Assert.NotEmpty(_store.Presets);
    }

    [Fact]
    public void Presets_AreIndependentCopies()
    {
        _store.Load();
        var preset = _store.Presets.First(t => t.Id == TuningPresets.StandardE.Id);
        var before = TuningPresets.StandardE.OpenPitch(0).MidiNote;
        preset.TransposeAll(2);
        Assert.Equal(before, TuningPresets.StandardE.OpenPitch(0).MidiNote);
        Assert.Equal(before, _store.Presets.First(t => t.Id == TuningPresets.StandardE.Id).OpenPitch(0).MidiNote);
    }

    [Fact]
    public void SaveActive_UsesAtomicWrite_NoTmpLeftBehind()
    {
        _store.Load();
        _store.SetActive(TuningPresets.DropD.Copy());
        _store.SaveActive();
        Assert.True(File.Exists(Path.Combine(_dir, "_active.json")));
        Assert.Empty(Directory.EnumerateFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void SaveUserTuning_And_Reload()
    {
        _store.Load();
        var custom = new GuitarTuning("My Drop", [
            Pitch.FromNote(PitchClass.C, 2),
            Pitch.FromNote(PitchClass.G, 2),
            Pitch.FromNote(PitchClass.C, 3),
            Pitch.FromNote(PitchClass.F, 3),
            Pitch.FromNote(PitchClass.A, 3),
            Pitch.FromNote(PitchClass.D, 4)
        ]);

        _store.SaveUserTuning(custom);
        Assert.Contains(_store.UserTunings, t => t.Id == custom.Id);

        var reloaded = new TuningStore(_dir);
        reloaded.Load();
        Assert.Contains(reloaded.UserTunings, t => t.Id == custom.Id && t.Name == "My Drop");
    }

    [Fact]
    public void SaveActive_PersistsAcrossStores()
    {
        _store.Load();
        var drop = TuningPresets.DropD.Clone("Active Drop");
        _store.SetActive(drop);
        _store.SaveActive();

        var reloaded = new TuningStore(_dir);
        reloaded.Load();
        Assert.Equal(PitchClass.D, reloaded.ActiveTuning.OpenPitch(0).PitchClass);
    }

    [Fact]
    public void DeleteUserTuning_RemovesFile()
    {
        _store.Load();
        var custom = TuningPresets.OpenG.Clone("temp");
        _store.SaveUserTuning(custom);
        _store.DeleteUserTuning(custom.Id);

        Assert.DoesNotContain(_store.UserTunings, t => t.Id == custom.Id);
        Assert.False(File.Exists(Path.Combine(_dir, $"{custom.Id}.json")));
    }

    [Fact]
    public void Load_SkipsCorruptJson()
    {
        File.WriteAllText(Path.Combine(_dir, "junk.json"), "{ not valid");
        File.WriteAllText(Path.Combine(_dir, "empty.json"), "{}");
        _store.Load();
        // should not throw
        Assert.NotNull(_store.ActiveTuning);
    }

    [Fact]
    public void Load_SkipsInvalidStringCount()
    {
        File.WriteAllText(Path.Combine(_dir, "bad.json"),
            """{"id":"x","name":"bad","openMidiNotes":[40,45]}""");
        _store.Load();
        Assert.DoesNotContain(_store.UserTunings, t => t.Id == "x");
    }
}
