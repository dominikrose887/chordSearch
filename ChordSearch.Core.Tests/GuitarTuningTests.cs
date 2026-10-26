using ChordSearch.Core.Music;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Core.Tests;

public class GuitarTuningTests
{
    [Fact]
    public void StandardE_SixStrings()
    {
        var t = TuningPresets.StandardE;
        Assert.Equal(6, t.StringCount);
        Assert.Equal(PitchClass.E, t.OpenPitch(0).PitchClass);
        Assert.Equal(PitchClass.E, t.OpenPitch(5).PitchClass);
        Assert.Equal(2, t.OpenPitch(0).Octave);
        Assert.Equal(4, t.OpenPitch(5).Octave);
    }

    [Fact]
    public void PitchAt_Fret12_IsOctaveUp()
    {
        var t = TuningPresets.StandardE;
        var open = t.OpenPitch(0);
        var twelfth = t.PitchAt(0, 12);
        Assert.Equal(open.PitchClass, twelfth.PitchClass);
        Assert.Equal(open.MidiNote + 12, twelfth.MidiNote);
    }

    [Fact]
    public void PitchAt_NegativeFret_Throws()
    {
        var t = TuningPresets.StandardE;
        Assert.Throws<ArgumentOutOfRangeException>(() => t.PitchAt(0, -1));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(9)]
    public void Constructor_InvalidStringCount_Throws(int count)
    {
        var pitches = Enumerable.Range(0, count).Select(_ => Pitch.FromNote(PitchClass.E, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GuitarTuning("bad", pitches));
    }

    [Fact]
    public void SetStringCount_7_InsertsLowString()
    {
        var t = TuningPresets.StandardE.Clone();
        t.SetStringCount(7, Pitch.FromNote(PitchClass.B, 1));
        Assert.Equal(7, t.StringCount);
        Assert.Equal(PitchClass.B, t.OpenPitch(0).PitchClass);
    }

    [Fact]
    public void SetStringCount_4_RemovesLowest()
    {
        var t = TuningPresets.StandardE.Clone();
        t.SetStringCount(4);
        Assert.Equal(4, t.StringCount);
        // removes from index 0 twice → starts at D
        Assert.Equal(PitchClass.D, t.OpenPitch(0).PitchClass);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(9)]
    public void SetStringCount_OutOfRange_Throws(int count)
    {
        var t = TuningPresets.StandardE.Clone();
        Assert.Throws<ArgumentOutOfRangeException>(() => t.SetStringCount(count));
    }

    [Fact]
    public void Dto_RoundTrip()
    {
        var original = TuningPresets.DropD;
        var clone = GuitarTuning.FromDto(original.ToDto());
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.OpenStrings.Select(p => p.MidiNote), clone.OpenStrings.Select(p => p.MidiNote));
    }

    [Fact]
    public void Clone_GetsNewId()
    {
        var a = TuningPresets.StandardE;
        var b = a.Clone();
        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(a.OpenStrings.Count, b.OpenStrings.Count);
    }

    [Fact]
    public void Copy_KeepsSameId_AndIsolatesMutation()
    {
        var original = TuningPresets.StandardE;
        var e0 = original.OpenPitch(0).MidiNote;
        var copy = original.Copy();
        Assert.Equal(original.Id, copy.Id);

        copy.TransposeAll(1);
        Assert.Equal(e0, original.OpenPitch(0).MidiNote);
        Assert.Equal(e0 + 1, copy.OpenPitch(0).MidiNote);
    }

    [Fact]
    public void BassPreset_FourStrings()
        => Assert.Equal(4, TuningPresets.BassStandard.StringCount);

    [Fact]
    public void SevenString_LowB()
    {
        var t = TuningPresets.SevenStringStandard;
        Assert.Equal(7, t.StringCount);
        Assert.Equal(PitchClass.B, t.OpenPitch(0).PitchClass);
        Assert.Equal(1, t.OpenPitch(0).Octave);
    }
}
