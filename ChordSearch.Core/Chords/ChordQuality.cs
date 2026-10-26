namespace ChordSearch.Core.Chords;

public enum ChordQuality
{
    Major,
    Minor,
    Diminished,
    Augmented,
    Sus2,
    Sus4,
    Power
}

public enum ChordExtension
{
    Sixth,
    Seventh,
    MajorSeventh,
    Ninth,
    Eleventh,
    Thirteenth,
    Add9,
    Flat5,
    Sharp5,
    Flat9,
    Sharp9,
    Sharp11,
    Flat13
}

public static class ChordQualityInfo
{
    public static string Symbol(ChordQuality q) => q switch
    {
        ChordQuality.Major => "",
        ChordQuality.Minor => "m",
        ChordQuality.Diminished => "dim",
        ChordQuality.Augmented => "aug",
        ChordQuality.Sus2 => "sus2",
        ChordQuality.Sus4 => "sus4",
        ChordQuality.Power => "5",
        _ => ""
    };

    public static string DisplayName(ChordQuality q) => q switch
    {
        ChordQuality.Major => "Major",
        ChordQuality.Minor => "Minor",
        ChordQuality.Diminished => "Diminished",
        ChordQuality.Augmented => "Augmented",
        ChordQuality.Sus2 => "Sus2",
        ChordQuality.Sus4 => "Sus4",
        ChordQuality.Power => "Power (5)",
        _ => q.ToString()
    };

    public static string Symbol(ChordExtension e) => e switch
    {
        ChordExtension.Sixth => "6",
        ChordExtension.Seventh => "7",
        ChordExtension.MajorSeventh => "maj7",
        ChordExtension.Ninth => "9",
        ChordExtension.Eleventh => "11",
        ChordExtension.Thirteenth => "13",
        ChordExtension.Add9 => "add9",
        ChordExtension.Flat5 => "b5",
        ChordExtension.Sharp5 => "#5",
        ChordExtension.Flat9 => "b9",
        ChordExtension.Sharp9 => "#9",
        ChordExtension.Sharp11 => "#11",
        ChordExtension.Flat13 => "b13",
        _ => e.ToString()
    };
}
