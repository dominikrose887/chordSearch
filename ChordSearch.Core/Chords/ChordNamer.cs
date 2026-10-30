using ChordSearch.Core.Music;

namespace ChordSearch.Core.Chords;

public static class ChordNamer
{
    public static string Name(ChordSpec spec)
    {
        var root = PitchClassNames.ToDisplayName(spec.Root, spec.AccidentalPreference);
        var quality = ChordQualityInfo.Symbol(spec.Quality);

        var extParts = new List<string>();
        var exts = spec.Extensions.ToHashSet();

        // Ordered symbol assembly — collapse maj7+9/11/13 → maj9/maj11/maj13
        var hasMaj7 = exts.Contains(ChordExtension.MajorSeventh);
        var has13 = exts.Contains(ChordExtension.Thirteenth);
        var has11 = exts.Contains(ChordExtension.Eleventh);
        var has9 = exts.Contains(ChordExtension.Ninth);

        if (hasMaj7 && has13)
            extParts.Add("maj13");
        else if (hasMaj7 && has11)
            extParts.Add("maj11");
        else if (hasMaj7 && has9)
            extParts.Add("maj9");
        else if (hasMaj7)
            extParts.Add("maj7");
        else if (exts.Contains(ChordExtension.Seventh) && !HasDominantExtension(exts))
            extParts.Add("7");

        if (exts.Contains(ChordExtension.Sixth) && !has13)
            extParts.Add("6");

        if (!hasMaj7 || (!has9 && !has11 && !has13))
        {
            if (has13)
                extParts.Add("13");
            else if (has11)
                extParts.Add("11");
            else if (has9)
                extParts.Add("9");
            else if (exts.Contains(ChordExtension.Add9))
                extParts.Add("add9");
        }
        else if (exts.Contains(ChordExtension.Add9) && !has9)
            extParts.Add("add9");

        AppendAlteration(extParts, exts, ChordExtension.Flat5);
        AppendAlteration(extParts, exts, ChordExtension.Sharp5);
        AppendAlteration(extParts, exts, ChordExtension.Flat9);
        AppendAlteration(extParts, exts, ChordExtension.Sharp9);
        AppendAlteration(extParts, exts, ChordExtension.Sharp11);
        AppendAlteration(extParts, exts, ChordExtension.Flat13);

        // Dominant 9/11/13 already imply 7 — strip standalone "7"
        if (extParts.Any(p => p is "9" or "11" or "13") && extParts.Contains("7"))
            extParts.Remove("7");

        var name = root + quality + string.Concat(extParts);

        if (spec.Bass is { } bass && bass != spec.Root)
            name += "/" + PitchClassNames.ToDisplayName(bass, spec.AccidentalPreference);

        return name;
    }

    private static bool HasDominantExtension(HashSet<ChordExtension> exts)
        => exts.Contains(ChordExtension.Ninth)
           || exts.Contains(ChordExtension.Eleventh)
           || exts.Contains(ChordExtension.Thirteenth);

    private static void AppendAlteration(List<string> parts, HashSet<ChordExtension> exts, ChordExtension e)
    {
        if (exts.Contains(e))
            parts.Add("(" + ChordQualityInfo.Symbol(e) + ")");
    }
}
