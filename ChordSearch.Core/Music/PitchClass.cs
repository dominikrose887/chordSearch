namespace ChordSearch.Core.Music;

/// <summary>Pitch class 0=C … 11=B.</summary>
public enum PitchClass
{
    C = 0,
    Cs = 1,
    D = 2,
    Ds = 3,
    E = 4,
    F = 5,
    Fs = 6,
    G = 7,
    Gs = 8,
    A = 9,
    As = 10,
    B = 11
}

public enum AccidentalPreference
{
    Sharp,
    Flat
}

public static class PitchClassNames
{
    private static readonly string[] Sharps = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static readonly string[] Flats = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];

    public static string ToDisplayName(PitchClass pc, AccidentalPreference preference = AccidentalPreference.Sharp)
        => preference == AccidentalPreference.Flat ? Flats[(int)pc] : Sharps[(int)pc];

    public static PitchClass FromMidi(int midiNote) => (PitchClass)(((midiNote % 12) + 12) % 12);

    public static PitchClass Transpose(PitchClass pc, int semitones)
        => (PitchClass)((((int)pc + semitones) % 12 + 12) % 12);
}
