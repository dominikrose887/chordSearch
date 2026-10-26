using ChordSearch.Core.Music;

namespace ChordSearch.Core.Tests;

public class PitchTests
{
    [Theory]
    [InlineData("E2", PitchClass.E, 2)]
    [InlineData("A2", PitchClass.A, 2)]
    [InlineData("D#4", PitchClass.Ds, 4)]
    [InlineData("Eb4", PitchClass.Ds, 4)]
    [InlineData("C4", PitchClass.C, 4)]
    [InlineData("Bb3", PitchClass.As, 3)]
    [InlineData("F#1", PitchClass.Fs, 1)]
    [InlineData("  g#5 ", PitchClass.Gs, 5)]
    public void Parse_ValidNotes(string text, PitchClass pc, int octave)
    {
        var p = Pitch.Parse(text);
        Assert.Equal(pc, p.PitchClass);
        Assert.Equal(octave, p.Octave);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Empty_Throws(string text)
        => Assert.Throws<ArgumentException>(() => Pitch.Parse(text));

    [Theory]
    [InlineData("H4")]
    [InlineData("X2")]
    [InlineData("C")]
    [InlineData("C#")]
    [InlineData("4C")]
    public void Parse_Invalid_Throws(string text)
        => Assert.ThrowsAny<Exception>(() => Pitch.Parse(text));

    [Fact]
    public void FromNote_C4_IsMidi60()
        => Assert.Equal(60, Pitch.FromNote(PitchClass.C, 4).MidiNote);

    [Fact]
    public void Transpose_WrapsPitchClass()
    {
        var e = Pitch.FromNote(PitchClass.E, 2);
        Assert.Equal(PitchClass.F, e.Transpose(1).PitchClass);
        Assert.Equal(PitchClass.Ds, e.Transpose(-1).PitchClass);
    }

    [Fact]
    public void FromMidi_NegativeModulo_Safe()
    {
        // MidiNote can be negative theoretically — PitchClass uses modulo
        var pc = PitchClassNames.FromMidi(-1);
        Assert.Equal(PitchClass.B, pc);
    }

    [Fact]
    public void DisplayName_FlatPreference()
    {
        var p = Pitch.FromNote(PitchClass.As, 3);
        Assert.Equal("A#3", p.ToDisplayName(AccidentalPreference.Sharp));
        Assert.Equal("Bb3", p.ToDisplayName(AccidentalPreference.Flat));
    }

    [Fact]
    public void PitchClass_Transpose_NegativeOctaveWrap()
    {
        var pc = PitchClassNames.Transpose(PitchClass.C, -1);
        Assert.Equal(PitchClass.B, pc);
    }

    [Fact]
    public void PitchClass_Transpose_LargePositive()
    {
        var pc = PitchClassNames.Transpose(PitchClass.A, 14);
        Assert.Equal(PitchClass.B, pc); // +14 = +2 mod 12
    }
}
