using ChordSearch.Core.Fingering;
using ChordSearch.Core.Music;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Results;

public sealed class VoicingResult
{
    public ChordVoicing Voicing { get; set; } = new() { Strings = [] };
    public ChordFingering Fingering { get; set; } = new() { Fingers = [] };
    public double Score { get; set; }
    public string Label { get; set; } = "";
    public string? Detail { get; set; }

    public int Position => Voicing.MinFret ?? 0;

    public bool HasBarre => Fingering.Barres.Count > 0;

    /// <summary>Distinct fretting fingers used (barre counts as one).</summary>
    public int FingerCount => Fingering.Fingers
        .Where(f => f.IsAssigned)
        .Select(f => f.Value)
        .Distinct()
        .Count();

    /// <summary>First-position / nut-area shape (highest fretted fret ≤ 3).</summary>
    public bool IsOpenPosition
    {
        get
        {
            var max = Voicing.MaxFret;
            return max is null or <= 3;
        }
    }

    /// <summary>Learner-facing difficulty from internal rank score.</summary>
    public string DifficultyLabel => Score switch
    {
        < 2.5 => "Easy",
        < 6.0 => "Medium",
        _ => "Harder"
    };

    public IReadOnlyList<Pitch?> SoundingPitches(GuitarTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (Voicing.StringCount != tuning.StringCount)
            throw new ArgumentException(
                $"Voicing has {Voicing.StringCount} strings but tuning has {tuning.StringCount}.",
                nameof(tuning));

        var result = new Pitch?[Voicing.StringCount];
        for (var i = 0; i < Voicing.StringCount; i++)
        {
            var sf = Voicing.Strings[i];
            if (sf.IsMuted) result[i] = null;
            else result[i] = tuning.PitchAt(i, sf.Fret);
        }
        return result;
    }
}
