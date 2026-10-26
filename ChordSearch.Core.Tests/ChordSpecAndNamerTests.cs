using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;

namespace ChordSearch.Core.Tests;

public class ChordSpecAndNamerTests
{
    [Fact]
    public void ToggleExtension_RemovesOnSecondToggle()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major }
            .ToggleExtension(ChordExtension.Seventh)
            .ToggleExtension(ChordExtension.Seventh);

        Assert.Empty(spec.Extensions);
        Assert.Equal("C", spec.DisplayName);
    }

    [Fact]
    public void Seventh_And_Maj7_Conflict()
    {
        var with7 = new ChordSpec { Root = PitchClass.C }.ToggleExtension(ChordExtension.Seventh);
        var withMaj = with7.ToggleExtension(ChordExtension.MajorSeventh);

        Assert.Contains(ChordExtension.MajorSeventh, withMaj.Extensions);
        Assert.DoesNotContain(ChordExtension.Seventh, withMaj.Extensions);
        Assert.Equal("Cmaj7", withMaj.DisplayName);
    }

    [Fact]
    public void Flat5_And_Sharp5_Conflict()
    {
        var spec = new ChordSpec { Root = PitchClass.C }
            .ToggleExtension(ChordExtension.Flat5)
            .ToggleExtension(ChordExtension.Sharp5);

        Assert.Contains(ChordExtension.Sharp5, spec.Extensions);
        Assert.DoesNotContain(ChordExtension.Flat5, spec.Extensions);
    }

    [Fact]
    public void Ninth_Removes_Add9_And_Implies_Seventh()
    {
        var spec = new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major }
            .ToggleExtension(ChordExtension.Add9)
            .ToggleExtension(ChordExtension.Ninth);

        Assert.Contains(ChordExtension.Ninth, spec.Extensions);
        Assert.Contains(ChordExtension.Seventh, spec.Extensions);
        Assert.DoesNotContain(ChordExtension.Add9, spec.Extensions);
        Assert.Equal("G9", spec.DisplayName);
    }

    [Fact]
    public void Ninth_With_Maj7_DoesNotForce_Dominant7()
    {
        var spec = new ChordSpec { Root = PitchClass.C }
            .ToggleExtension(ChordExtension.MajorSeventh)
            .ToggleExtension(ChordExtension.Ninth);

        Assert.Contains(ChordExtension.MajorSeventh, spec.Extensions);
        Assert.DoesNotContain(ChordExtension.Seventh, spec.Extensions);
        Assert.Equal("Cmaj9", spec.DisplayName);

        var formula = ChordFormula.FromSpec(spec);
        Assert.Contains(11, formula.Intervals); // maj7
        Assert.Contains(2, formula.Intervals);  // 9
        Assert.DoesNotContain(10, formula.Intervals); // no b7
    }

    [Fact]
    public void Maj11_And_Maj13_NamesAndIntervals()
    {
        var maj11 = new ChordSpec { Root = PitchClass.C }
            .ToggleExtension(ChordExtension.MajorSeventh)
            .ToggleExtension(ChordExtension.Eleventh);
        Assert.Equal("Cmaj11", maj11.DisplayName);
        var f11 = ChordFormula.FromSpec(maj11);
        Assert.Contains(11, f11.Intervals);
        Assert.DoesNotContain(10, f11.Intervals);

        var maj13 = new ChordSpec { Root = PitchClass.F }
            .ToggleExtension(ChordExtension.MajorSeventh)
            .ToggleExtension(ChordExtension.Thirteenth);
        Assert.Equal("Fmaj13", maj13.DisplayName);
        var f13 = ChordFormula.FromSpec(maj13);
        Assert.Contains(11, f13.Intervals);
        Assert.DoesNotContain(10, f13.Intervals);
    }

    [Theory]
    [InlineData(ChordQuality.Major, "C")]
    [InlineData(ChordQuality.Minor, "Cm")]
    [InlineData(ChordQuality.Diminished, "Cdim")]
    [InlineData(ChordQuality.Augmented, "Caug")]
    [InlineData(ChordQuality.Sus2, "Csus2")]
    [InlineData(ChordQuality.Sus4, "Csus4")]
    [InlineData(ChordQuality.Power, "C5")]
    public void Quality_Symbols(ChordQuality q, string expected)
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = q };
        Assert.Equal(expected, spec.DisplayName);
    }

    [Fact]
    public void SlashChord_SameBassAsRoot_NoSlash()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Bass = PitchClass.C };
        Assert.Equal("C", spec.DisplayName);
    }

    [Fact]
    public void WithBass_Null_ClearsSlash()
    {
        var spec = new ChordSpec { Root = PitchClass.D, Bass = PitchClass.Fs }.WithBass(null);
        Assert.Null(spec.Bass);
        Assert.Equal("D", spec.DisplayName);
    }

    [Fact]
    public void Minor6_Name()
    {
        var spec = new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Minor }
            .ToggleExtension(ChordExtension.Sixth);
        Assert.Equal("Am6", spec.DisplayName);
    }

    [Fact]
    public void Formula_PowerChord_OnlyRootAndFifth()
    {
        var formula = ChordFormula.FromSpec(new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Power });
        Assert.Equal(2, formula.Intervals.Count);
        Assert.Contains(0, formula.Intervals);
        Assert.Contains(7, formula.Intervals);
    }

    [Fact]
    public void Formula_Flat5_Replaces_PerfectFifth()
    {
        var formula = ChordFormula.FromSpec(
            new ChordSpec { Root = PitchClass.C }.ToggleExtension(ChordExtension.Flat5));
        Assert.Contains(6, formula.Intervals);
        Assert.DoesNotContain(7, formula.Intervals);
    }

    [Fact]
    public void Formula_Dim7_Uses_Bb7_Interval()
    {
        // dim + 7 → interval 9 (bb7)
        var formula = ChordFormula.FromSpec(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Diminished }
                .ToggleExtension(ChordExtension.Seventh));
        Assert.Contains(9, formula.Intervals);
    }
}
