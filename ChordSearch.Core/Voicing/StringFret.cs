namespace ChordSearch.Core.Voicing;

/// <summary>
/// Per-string state: muted (-1), open (0), or fretted (1+).
/// </summary>
public readonly record struct StringFret(int Fret)
{
    public static StringFret Muted { get; } = new(-1);
    public static StringFret Open { get; } = new(0);

    public bool IsMuted => Fret < 0;
    public bool IsOpen => Fret == 0;
    public bool IsFretted => Fret > 0;

    public override string ToString() => IsMuted ? "x" : Fret.ToString();
}

public sealed class ChordVoicing
{
    public IReadOnlyList<StringFret> Strings { get; init; } = [];

    public int StringCount => Strings.Count;

    public int? MinFret
    {
        get
        {
            var fretted = Strings.Where(s => s.IsFretted).Select(s => s.Fret).ToList();
            return fretted.Count == 0 ? null : fretted.Min();
        }
    }

    public int? MaxFret
    {
        get
        {
            var fretted = Strings.Where(s => s.IsFretted).Select(s => s.Fret).ToList();
            return fretted.Count == 0 ? null : fretted.Max();
        }
    }

    public int Span
    {
        get
        {
            if (MinFret is null || MaxFret is null) return 0;
            return MaxFret.Value - MinFret.Value;
        }
    }

    public int SoundingStringCount => Strings.Count(s => !s.IsMuted);

    public string ShapeKey => string.Join("-", Strings.Select(s => s.Fret));

    public override string ToString() => ShapeKey;
}
