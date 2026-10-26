namespace ChordSearch.Core.Music;

/// <summary>Absolute pitch as MIDI note number (C4 = 60).</summary>
public readonly record struct Pitch(int MidiNote)
{
    public PitchClass PitchClass => PitchClassNames.FromMidi(MidiNote);

    public int Octave => MidiNote / 12 - 1;

    public Pitch Transpose(int semitones) => new(MidiNote + semitones);

    public static Pitch FromNote(PitchClass pc, int octave)
        => new((octave + 1) * 12 + (int)pc);

    public string ToDisplayName(AccidentalPreference preference = AccidentalPreference.Sharp)
        => $"{PitchClassNames.ToDisplayName(PitchClass, preference)}{Octave}";

    public override string ToString() => ToDisplayName();

    public static Pitch Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Empty pitch.", nameof(text));

        text = text.Trim();
        var i = 0;
        var letter = char.ToUpperInvariant(text[i++]);
        if (letter is < 'A' or > 'G')
            throw new FormatException($"Invalid pitch: {text}");

        var pc = letter switch
        {
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => throw new FormatException($"Invalid pitch: {text}")
        };

        while (i < text.Length && (text[i] is '#' or 'b' or 'B' or '♯' or '♭'))
        {
            if (text[i] is '#' or '♯') pc++;
            else pc--;
            i++;
        }

        var octaveSign = 1;
        if (i < text.Length && text[i] == '-')
        {
            octaveSign = -1;
            i++;
        }

        if (i >= text.Length || !char.IsDigit(text[i]))
            throw new FormatException($"Missing octave in pitch: {text}");

        var octave = 0;
        while (i < text.Length && char.IsDigit(text[i]))
        {
            octave = octave * 10 + (text[i] - '0');
            i++;
        }

        octave *= octaveSign;
        pc = ((pc % 12) + 12) % 12;
        return FromNote((PitchClass)pc, octave);
    }
}
