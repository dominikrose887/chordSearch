namespace ChordSearch.Core.Voicing;

public sealed class VoicingOptions
{
    /// <summary>Search up to this fret (inclusive). Matches neck length setting (default 24).</summary>
    public int MaxFret { get; set; } = 24;

    /// <summary>Hard reject above this fretted span (inclusive difference max-min).</summary>
    public int AbsoluteMaxSpan { get; set; } = 5;

    /// <summary>Spans above this get a steep score penalty.</summary>
    public int ComfortableSpan { get; set; } = 3;

    public int MaxResults { get; set; } = 40;
    public int MinSoundingStrings { get; set; } = 3;

    /// <summary>If false, muted strings between the first and last sounding string are rejected.</summary>
    public bool AllowMutedInteriorStrings { get; set; } = false;

    /// <summary>Reject fretted counts above 4 unless a barre (repeated fret) is present.</summary>
    public bool RequireBarreForMoreThanFourFrets { get; set; } = true;

    /// <summary>
    /// Reject span &gt; ComfortableSpan for crowded shapes (4+ fretted notes) when there is no
    /// repeated-fret barre at the lowest fretted fret. Sparse 2–3 note reaches defer to fingering.
    /// </summary>
    public bool RequireBarreForWideSpan { get; set; } = true;

    /// <summary>Maximum fingering difficulty accepted after assignment (higher = harder).</summary>
    public double MaxFingeringDifficulty { get; set; } = 10.0;

    public VoicingOptions Clone() => new()
    {
        MaxFret = MaxFret,
        AbsoluteMaxSpan = AbsoluteMaxSpan,
        ComfortableSpan = ComfortableSpan,
        MaxResults = MaxResults,
        MinSoundingStrings = MinSoundingStrings,
        AllowMutedInteriorStrings = AllowMutedInteriorStrings,
        RequireBarreForMoreThanFourFrets = RequireBarreForMoreThanFourFrets,
        RequireBarreForWideSpan = RequireBarreForWideSpan,
        MaxFingeringDifficulty = MaxFingeringDifficulty
    };
}
