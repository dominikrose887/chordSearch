using ChordSearch.Core.Music;

namespace ChordSearch.Core.Chords;

public sealed class ChordFormula
{
    public required IReadOnlySet<int> Intervals { get; init; }
    public required IReadOnlySet<int> RequiredIntervals { get; init; }
    public PitchClass Root { get; init; }
    public PitchClass? Bass { get; init; }

    public IReadOnlySet<PitchClass> PitchClasses =>
        Intervals.Select(i => PitchClassNames.Transpose(Root, i)).ToHashSet();

    public IReadOnlySet<PitchClass> RequiredPitchClasses =>
        RequiredIntervals.Select(i => PitchClassNames.Transpose(Root, i)).ToHashSet();

    public static ChordFormula FromSpec(ChordSpec spec)
    {
        var intervals = new HashSet<int>();
        var required = new HashSet<int>();

        void Add(int interval, bool isRequired = true)
        {
            intervals.Add(interval % 12);
            if (isRequired) required.Add(interval % 12);
        }

        Add(0); // root always required

        switch (spec.Quality)
        {
            case ChordQuality.Major:
                Add(4); Add(7);
                break;
            case ChordQuality.Minor:
                Add(3); Add(7);
                break;
            case ChordQuality.Diminished:
                Add(3); Add(6);
                break;
            case ChordQuality.Augmented:
                Add(4); Add(8);
                break;
            case ChordQuality.Sus2:
                Add(2); Add(7);
                break;
            case ChordQuality.Sus4:
                Add(5); Add(7);
                break;
            case ChordQuality.Power:
                Add(7);
                break;
        }

        foreach (var ext in spec.Extensions)
        {
            switch (ext)
            {
                case ChordExtension.Sixth:
                    Add(9);
                    break;
                case ChordExtension.Seventh:
                    // dim typically gets bb7 (9), otherwise b7
                    if (spec.Quality == ChordQuality.Diminished)
                        Add(9);
                    else
                        Add(10);
                    break;
                case ChordExtension.MajorSeventh:
                    Add(11);
                    break;
                case ChordExtension.Ninth:
                    Add(2);
                    // Dominant 9 includes b7; maj9 gets maj7 from MajorSeventh (do not add b7)
                    if (!spec.Extensions.Contains(ChordExtension.MajorSeventh))
                        Add(10);
                    break;
                case ChordExtension.Eleventh:
                    Add(5, isRequired: false);
                    Add(2, isRequired: false);
                    if (!spec.Extensions.Contains(ChordExtension.MajorSeventh))
                        Add(10, isRequired: false);
                    break;
                case ChordExtension.Thirteenth:
                    Add(9, isRequired: false);
                    Add(2, isRequired: false);
                    if (!spec.Extensions.Contains(ChordExtension.MajorSeventh))
                        Add(10, isRequired: false);
                    break;
                case ChordExtension.Add9:
                    Add(2);
                    break;
                case ChordExtension.Flat5:
                    intervals.Remove(7);
                    required.Remove(7);
                    Add(6);
                    break;
                case ChordExtension.Sharp5:
                    intervals.Remove(7);
                    required.Remove(7);
                    Add(8);
                    break;
                case ChordExtension.Flat9:
                    Add(1);
                    break;
                case ChordExtension.Sharp9:
                    Add(3, isRequired: false);
                    break;
                case ChordExtension.Sharp11:
                    Add(6, isRequired: false);
                    break;
                case ChordExtension.Flat13:
                    Add(8, isRequired: false);
                    break;
            }
        }

        // Prefer omitting 5th as optional for dense extensions
        if (spec.Extensions.Any(e => e is ChordExtension.Ninth or ChordExtension.Eleventh or ChordExtension.Thirteenth))
            required.Remove(7);

        return new ChordFormula
        {
            Root = spec.Root,
            Bass = spec.Bass,
            Intervals = intervals,
            RequiredIntervals = required
        };
    }
}
