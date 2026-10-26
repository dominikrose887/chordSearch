namespace ChordSearch.Core.Fingering;

/// <summary>Finger index 1–4; 0 = none (open/muted).</summary>
public readonly record struct FingerId(int Value)
{
    public static FingerId None { get; } = new(0);
    public static FingerId Index { get; } = new(1);
    public static FingerId Middle { get; } = new(2);
    public static FingerId Ring { get; } = new(3);
    public static FingerId Pinky { get; } = new(4);

    public bool IsAssigned => Value is >= 1 and <= 4;
    public override string ToString() => Value == 0 ? "-" : Value.ToString();
}

public sealed class BarreSegment
{
    public int Fret { get; init; }
    public int Finger { get; init; }
    public int FromString { get; init; }
    public int EndString { get; init; }
}

public sealed class ChordFingering
{
    /// <summary>Finger per string (same length as voicing). 0 = open/muted.</summary>
    public IReadOnlyList<FingerId> Fingers { get; init; } = [];

    public IReadOnlyList<BarreSegment> Barres { get; init; } = [];

    public double DifficultyScore { get; init; }

    public string? Notes { get; init; }
}
