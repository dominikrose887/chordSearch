using ChordSearch.Core.Music;

namespace ChordSearch.Core.Chords;

/// <summary>
/// Valid slash-bass choices for a chord: root position + chord tones only.
/// Non-chord tones (e.g. F or G# under Cm) are excluded.
/// </summary>
public static class ChordBassOptions
{
    public static IReadOnlyList<PitchClass> ValidBassPitchClasses(ChordSpec specWithoutRelyingOnBass)
    {
        var formula = ChordFormula.FromSpec(specWithoutRelyingOnBass.WithBass(null));
        return formula.PitchClasses
            .OrderBy(pc => ((int)pc - (int)specWithoutRelyingOnBass.Root + 12) % 12)
            .ToArray();
    }

    public static bool IsValidBass(ChordSpec spec)
    {
        if (spec.Bass is null) return true;
        if (spec.Bass == spec.Root) return true;
        return ChordFormula.FromSpec(spec.WithBass(null)).PitchClasses.Contains(spec.Bass.Value);
    }
}
