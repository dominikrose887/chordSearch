namespace ChordSearch.Core.Settings;

public sealed class AppSettings
{
    /// <summary>Highest fretted note on the neck (nut is 0). Default 24 for electric.</summary>
    public int MaxFrets { get; set; } = 24;

    /// <summary>Scale length in millimetres (Fender-style default 648).</summary>
    public double ScaleLengthMm { get; set; } = 648.0;

    /// <summary>Last selected MIDI output device index (−1 = none).</summary>
    public int MidiDeviceIndex { get; set; } = -1;

    /// <summary>MIDI note velocity 1–127.</summary>
    public int MidiVelocity { get; set; } = 90;

    public void Clamp()
    {
        MaxFrets = Math.Clamp(MaxFrets, 12, 24);
        if (ScaleLengthMm is < 500 or > 700)
            ScaleLengthMm = 648.0;
        MidiVelocity = Math.Clamp(MidiVelocity, 1, 127);
        if (MidiDeviceIndex < -1)
            MidiDeviceIndex = -1;
    }
}

public static class FretCountPresets
{
    public static IReadOnlyList<(string Label, int Frets)> All { get; } =
    [
        ("18 — Classical", 18),
        ("19", 19),
        ("20", 20),
        ("21", 21),
        ("22 — Many electrics", 22),
        ("24 — Electric (default)", 24)
    ];
}
