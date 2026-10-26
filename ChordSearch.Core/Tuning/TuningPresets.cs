using ChordSearch.Core.Music;

namespace ChordSearch.Core.Tuning;

public static class TuningPresets
{
    public static GuitarTuning StandardE { get; } = new(
        "Standard E",
        [
            Pitch.FromNote(PitchClass.E, 2),
            Pitch.FromNote(PitchClass.A, 2),
            Pitch.FromNote(PitchClass.D, 3),
            Pitch.FromNote(PitchClass.G, 3),
            Pitch.FromNote(PitchClass.B, 3),
            Pitch.FromNote(PitchClass.E, 4)
        ],
        "preset-standard-e");

    public static GuitarTuning DropD { get; } = new(
        "Drop D",
        [
            Pitch.FromNote(PitchClass.D, 2),
            Pitch.FromNote(PitchClass.A, 2),
            Pitch.FromNote(PitchClass.D, 3),
            Pitch.FromNote(PitchClass.G, 3),
            Pitch.FromNote(PitchClass.B, 3),
            Pitch.FromNote(PitchClass.E, 4)
        ],
        "preset-drop-d");

    public static GuitarTuning OpenG { get; } = new(
        "Open G",
        [
            Pitch.FromNote(PitchClass.D, 2),
            Pitch.FromNote(PitchClass.G, 2),
            Pitch.FromNote(PitchClass.D, 3),
            Pitch.FromNote(PitchClass.G, 3),
            Pitch.FromNote(PitchClass.B, 3),
            Pitch.FromNote(PitchClass.D, 4)
        ],
        "preset-open-g");

    public static GuitarTuning OpenD { get; } = new(
        "Open D",
        [
            Pitch.FromNote(PitchClass.D, 2),
            Pitch.FromNote(PitchClass.A, 2),
            Pitch.FromNote(PitchClass.D, 3),
            Pitch.FromNote(PitchClass.Fs, 3),
            Pitch.FromNote(PitchClass.A, 3),
            Pitch.FromNote(PitchClass.D, 4)
        ],
        "preset-open-d");

    public static GuitarTuning HalfStepDown { get; } = new(
        "Half Step Down",
        [
            Pitch.FromNote(PitchClass.Ds, 2),
            Pitch.FromNote(PitchClass.Gs, 2),
            Pitch.FromNote(PitchClass.Cs, 3),
            Pitch.FromNote(PitchClass.Fs, 3),
            Pitch.FromNote(PitchClass.As, 3),
            Pitch.FromNote(PitchClass.Ds, 4)
        ],
        "preset-half-down");

    public static GuitarTuning BassStandard { get; } = new(
        "Bass Standard",
        [
            Pitch.FromNote(PitchClass.E, 1),
            Pitch.FromNote(PitchClass.A, 1),
            Pitch.FromNote(PitchClass.D, 2),
            Pitch.FromNote(PitchClass.G, 2)
        ],
        "preset-bass-standard");

    public static GuitarTuning SevenStringStandard { get; } = new(
        "7-String Standard",
        [
            Pitch.FromNote(PitchClass.B, 1),
            Pitch.FromNote(PitchClass.E, 2),
            Pitch.FromNote(PitchClass.A, 2),
            Pitch.FromNote(PitchClass.D, 3),
            Pitch.FromNote(PitchClass.G, 3),
            Pitch.FromNote(PitchClass.B, 3),
            Pitch.FromNote(PitchClass.E, 4)
        ],
        "preset-7-standard");

    public static IReadOnlyList<GuitarTuning> All { get; } =
    [
        StandardE, DropD, OpenG, OpenD, HalfStepDown, BassStandard, SevenStringStandard
    ];
}
