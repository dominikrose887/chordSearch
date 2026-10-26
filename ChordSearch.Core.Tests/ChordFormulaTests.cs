using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;

namespace ChordSearch.Core.Tests;

public class ChordFormulaTests
{
    [Fact]
    public void MinorSeventh_HasExpectedIntervals()
    {
        var spec = new ChordSpec
        {
            Root = PitchClass.C,
            Quality = ChordQuality.Minor
        }.ToggleExtension(ChordExtension.Seventh);

        var formula = ChordFormula.FromSpec(spec);
        Assert.Equal("Cm7", spec.DisplayName);
        Assert.Contains(0, formula.Intervals);
        Assert.Contains(3, formula.Intervals);
        Assert.Contains(7, formula.Intervals);
        Assert.Contains(10, formula.Intervals);
    }

    [Fact]
    public void Sus4_Name()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Sus4 };
        Assert.Equal("Csus4", spec.DisplayName);
        var formula = ChordFormula.FromSpec(spec);
        Assert.Contains(5, formula.Intervals);
        Assert.DoesNotContain(4, formula.Intervals);
    }

    [Fact]
    public void SlashChord_Name()
    {
        var spec = new ChordSpec
        {
            Root = PitchClass.D,
            Quality = ChordQuality.Major,
            Bass = PitchClass.Fs
        };
        Assert.Equal("D/F#", spec.DisplayName);
    }
}
