using ChordSearch.Core.Settings;

namespace ChordSearch.Core.Tests;

public class AppSettingsTests : IDisposable
{
    private readonly string _dir;

    public AppSettingsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ChordSearchSettingsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Defaults_AreElectric24_And648()
    {
        var s = new AppSettings();
        Assert.Equal(24, s.MaxFrets);
        Assert.Equal(648, s.ScaleLengthMm);
    }

    [Theory]
    [InlineData(5, 12)]
    [InlineData(100, 24)]
    [InlineData(18, 18)]
    public void Clamp_MaxFrets(int input, int expected)
    {
        var s = new AppSettings { MaxFrets = input };
        s.Clamp();
        Assert.Equal(expected, s.MaxFrets);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(800)]
    public void Clamp_BadScale_ResetsTo648(double scale)
    {
        var s = new AppSettings { ScaleLengthMm = scale };
        s.Clamp();
        Assert.Equal(648, s.ScaleLengthMm);
    }

    [Fact]
    public void Store_RoundTrip()
    {
        var store = new AppSettingsStore(_dir);
        store.Load();
        store.Current.MaxFrets = 18;
        store.Current.ScaleLengthMm = 650;
        store.Current.MidiDeviceIndex = 2;
        store.Current.MidiVelocity = 100;
        store.Save();

        var reloaded = new AppSettingsStore(_dir);
        reloaded.Load();
        Assert.Equal(18, reloaded.Current.MaxFrets);
        Assert.Equal(650, reloaded.Current.ScaleLengthMm);
        Assert.Equal(2, reloaded.Current.MidiDeviceIndex);
        Assert.Equal(100, reloaded.Current.MidiVelocity);
        Assert.Null(reloaded.LastLoadWarning);
    }

    [Fact]
    public void Store_CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "NOT JSON");
        var store = new AppSettingsStore(_dir);
        store.Load();
        Assert.Equal(24, store.Current.MaxFrets);
        Assert.NotNull(store.LastLoadWarning);
        Assert.Contains("Corrupt", store.LastLoadWarning);

        // Corrupt file rewritten with valid defaults
        var json = File.ReadAllText(Path.Combine(_dir, "settings.json"));
        Assert.DoesNotContain("NOT JSON", json);
        Assert.Empty(Directory.EnumerateFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void FretCountPresets_Include18And24()
    {
        Assert.Contains(FretCountPresets.All, p => p.Frets == 18);
        Assert.Contains(FretCountPresets.All, p => p.Frets == 24);
    }
}
