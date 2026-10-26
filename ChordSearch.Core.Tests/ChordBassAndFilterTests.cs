using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class ChordBassAndFilterTests
{
    [Fact]
    public void Cm_ValidBass_OnlyChordTones()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor };
        var bass = ChordBassOptions.ValidBassPitchClasses(spec);

        Assert.Contains(PitchClass.C, bass);
        Assert.Contains(PitchClass.Ds, bass); // Eb
        Assert.Contains(PitchClass.G, bass);
        Assert.DoesNotContain(PitchClass.F, bass);
        Assert.DoesNotContain(PitchClass.Gs, bass);
        Assert.DoesNotContain(PitchClass.A, bass);
        Assert.Equal(3, bass.Count);
    }

    [Fact]
    public void Cmaj7_ValidBass_IncludesB()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major }
            .ToggleExtension(ChordExtension.MajorSeventh);
        var bass = ChordBassOptions.ValidBassPitchClasses(spec);

        Assert.Contains(PitchClass.C, bass);
        Assert.Contains(PitchClass.E, bass);
        Assert.Contains(PitchClass.G, bass);
        Assert.Contains(PitchClass.B, bass);
        Assert.DoesNotContain(PitchClass.As, bass); // Bb / A# not in Cmaj7
    }

    [Fact]
    public void IsValidBass_RejectsNonChordTone()
    {
        var bad = new ChordSpec
        {
            Root = PitchClass.C,
            Quality = ChordQuality.Minor,
            Bass = PitchClass.F
        };
        Assert.False(ChordBassOptions.IsValidBass(bad));

        var good = bad.WithBass(PitchClass.G);
        Assert.True(ChordBassOptions.IsValidBass(good));
    }

    [Fact]
    public void Search_InvalidBass_ReturnsNoVoicings()
    {
        var response = new ChordSearchService().Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor, Bass = PitchClass.F },
            TuningPresets.StandardE);

        Assert.Empty(response.Results);
        Assert.Contains("Invalid bass", response.TheorySummary);
    }

    [Fact]
    public void Search_ValidSlash_CmG_FindsVoicings()
    {
        var response = new ChordSearchService().Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor, Bass = PitchClass.G },
            TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);
        foreach (var r in response.Results.Take(10))
        {
            var pitches = r.SoundingPitches(TuningPresets.StandardE)
                .Where(p => p is not null).Select(p => p!.Value).ToList();
            Assert.Equal(PitchClass.G, pitches.MinBy(p => p.MidiNote).PitchClass);
        }
    }

    [Fact]
    public void PracticalFilter_RejectsInteriorMutes()
    {
        var engine = new VoicingEngine(new VoicingOptions { AllowMutedInteriorStrings = false });
        // Use search — every returned voicing must be contiguous
        var response = new ChordSearchService(new VoicingOptions { AllowMutedInteriorStrings = false })
            .Search(new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major }, TuningPresets.StandardE);

        foreach (var r in response.Results)
        {
            var frets = r.Voicing.Strings.Select(s => s.Fret).ToArray();
            var first = -1;
            var last = -1;
            for (var i = 0; i < frets.Length; i++)
            {
                if (frets[i] < 0) continue;
                if (first < 0) first = i;
                last = i;
            }

            for (var i = first; i <= last; i++)
                Assert.True(frets[i] >= 0, $"Interior mute at string {i} in {r.Voicing.ShapeKey}");
        }
    }

    [Fact]
    public void PracticalFilter_NoExcessiveSpanWithoutBarre()
    {
        var response = new ChordSearchService(new VoicingOptions
        {
            ComfortableSpan = 3,
            AbsoluteMaxSpan = 5,
            RequireBarreForWideSpan = true
        }).Search(new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Minor }, TuningPresets.StandardE);

        foreach (var r in response.Results)
        {
            if (r.Voicing.Span <= 3) continue;
            // wide shapes need a repeated fret at the lowest fretted position
            var fretted = r.Voicing.Strings.Where(s => s.IsFretted).Select(s => s.Fret).ToList();
            if (fretted.Count < 2) continue;
            var min = fretted.Min();
            Assert.True(fretted.Count(f => f == min) >= 2,
                $"Wide span {r.Voicing.Span} without low-fret barre: {r.Voicing.ShapeKey}");
        }
    }

    [Fact]
    public void PracticalFilter_RejectsNotesBehindHigherBarre()
    {
        // x x 5 5 1 x — mini-barre at 5 with a note at fret 1 behind it
        var response = new ChordSearchService().Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Power },
            TuningPresets.StandardE);

        Assert.DoesNotContain(response.Results, r => r.Voicing.ShapeKey == "-1--1-5-5-1--1");
        Assert.DoesNotContain(response.Results, r => r.Voicing.ShapeKey == "-1--1-5-0-1--1");

        foreach (var r in response.Results.Where(r => r.Voicing.Span > 3))
        {
            var fretted = r.Voicing.Strings.Where(s => s.IsFretted).Select(s => s.Fret).ToList();
            var min = fretted.Min();
            Assert.True(fretted.Count(f => f == min) >= 2,
                $"Accepted wide unbarre'd shape: {r.Voicing.ShapeKey}");
        }
    }

    [Fact]
    public void TransposeAll_ShiftsEveryString()
    {
        var t = TuningPresets.StandardE.Clone();
        var before = t.OpenStrings.Select(p => p.MidiNote).ToArray();
        t.TransposeAll(-1);
        var after = t.OpenStrings.Select(p => p.MidiNote).ToArray();
        Assert.Equal(before.Select(n => n - 1), after);
        Assert.Equal(PitchClass.Ds, t.OpenPitch(0).PitchClass); // E -> D#
    }

    [Fact]
    public void Extension_AddsToneToValidBass()
    {
        var plain = ChordBassOptions.ValidBassPitchClasses(
            new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major });
        var with7 = ChordBassOptions.ValidBassPitchClasses(
            new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major }
                .ToggleExtension(ChordExtension.Seventh));

        Assert.DoesNotContain(PitchClass.F, plain);
        Assert.Contains(PitchClass.F, with7); // b7 of G7
    }

    [Fact]
    public void PowerChord_BassOnlyRootAndFifth()
    {
        var bass = ChordBassOptions.ValidBassPitchClasses(
            new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Power });
        Assert.Equal(2, bass.Count);
        Assert.Contains(PitchClass.A, bass);
        Assert.Contains(PitchClass.E, bass);
    }
}
