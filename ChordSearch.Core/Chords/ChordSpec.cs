using ChordSearch.Core.Music;

namespace ChordSearch.Core.Chords;

public sealed class ChordSpec
{
    public PitchClass Root { get; init; } = PitchClass.C;
    public ChordQuality Quality { get; init; } = ChordQuality.Major;
    public IReadOnlyList<ChordExtension> Extensions { get; init; } = [];
    public PitchClass? Bass { get; init; }
    public AccidentalPreference AccidentalPreference { get; init; } = AccidentalPreference.Sharp;

    public ChordSpec WithRoot(PitchClass root) => Clone(root: root);
    public ChordSpec WithQuality(ChordQuality quality) => Clone(quality: quality);
    public ChordSpec WithBass(PitchClass? bass) => Clone(bass: bass, clearBass: bass is null);
    public ChordSpec WithPreference(AccidentalPreference preference) => Clone(preference: preference);

    public ChordSpec ToggleExtension(ChordExtension extension)
    {
        var set = Extensions.ToHashSet();
        if (!set.Add(extension))
            set.Remove(extension);

        // Resolve common conflicts
        ResolveConflicts(set, extension);
        return Clone(extensions: set.OrderBy(e => e).ToArray());
    }

    public ChordSpec WithExtensions(IEnumerable<ChordExtension> extensions)
    {
        var set = extensions.ToHashSet();
        foreach (var e in set.ToArray())
            ResolveConflicts(set, e);
        return Clone(extensions: set.OrderBy(e => e).ToArray());
    }

    private static void ResolveConflicts(HashSet<ChordExtension> set, ChordExtension justAdded)
    {
        if (justAdded is ChordExtension.Seventh && set.Contains(ChordExtension.MajorSeventh))
            set.Remove(ChordExtension.MajorSeventh);
        if (justAdded is ChordExtension.MajorSeventh && set.Contains(ChordExtension.Seventh))
            set.Remove(ChordExtension.Seventh);

        if (justAdded is ChordExtension.Flat5 && set.Contains(ChordExtension.Sharp5))
            set.Remove(ChordExtension.Sharp5);
        if (justAdded is ChordExtension.Sharp5 && set.Contains(ChordExtension.Flat5))
            set.Remove(ChordExtension.Flat5);

        if (justAdded is ChordExtension.Ninth)
            set.Remove(ChordExtension.Add9);
        if (justAdded is ChordExtension.Add9 && set.Contains(ChordExtension.Ninth))
            set.Remove(ChordExtension.Ninth);

        // 9/11/13 imply dominant 7 unless maj7 is present
        if (justAdded is ChordExtension.Ninth or ChordExtension.Eleventh or ChordExtension.Thirteenth)
        {
            if (!set.Contains(ChordExtension.MajorSeventh))
                set.Add(ChordExtension.Seventh);
        }
    }

    private ChordSpec Clone(
        PitchClass? root = null,
        ChordQuality? quality = null,
        IReadOnlyList<ChordExtension>? extensions = null,
        PitchClass? bass = null,
        bool clearBass = false,
        AccidentalPreference? preference = null)
        => new()
        {
            Root = root ?? Root,
            Quality = quality ?? Quality,
            Extensions = extensions ?? Extensions,
            Bass = clearBass ? null : bass ?? Bass,
            AccidentalPreference = preference ?? AccidentalPreference
        };

    public string DisplayName => ChordNamer.Name(this);

    public override string ToString() => DisplayName;
}
