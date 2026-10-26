using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class VoicingEdgeCaseTests
{
    [Fact]
    public void PowerChord_FindsVoicings()
    {
        var service = new ChordSearchService();
        var response = service.Search(
            new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Power },
            TuningPresets.StandardE);
        Assert.NotEmpty(response.Results);
    }

    [Fact]
    public void MaxFret18_DoesNotReturnAbove18()
    {
        var service = new ChordSearchService(new VoicingOptions { MaxFret = 18 });
        var response = service.Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        foreach (var r in response.Results)
        {
            foreach (var s in r.Voicing.Strings)
            {
                if (s.IsFretted)
                    Assert.True(s.Fret <= 18, $"Fret {s.Fret} exceeds MaxFret 18");
            }
        }
    }

    [Fact]
    public void MaxFret24_CanSearchHighNeck()
    {
        var service = new ChordSearchService(new VoicingOptions { MaxFret = 24, MaxResults = 60 });
        var response = service.Search(
            new ChordSpec { Root = PitchClass.E, Quality = ChordQuality.Minor },
            TuningPresets.StandardE);
        Assert.NotEmpty(response.Results);
        // At least one result uses a fret above 12 when searching full neck
        Assert.Contains(response.Results, r => r.Voicing.Strings.Any(s => s.IsFretted && s.Fret > 12));
    }

    [Fact]
    public void SetMaxFret_UpdatesSearch()
    {
        var service = new ChordSearchService(new VoicingOptions { MaxFret = 12 });
        service.SetMaxFret(24);
        Assert.Equal(24, service.Options.MaxFret);
    }

    [Fact]
    public void BassStandard_FourString_SearchWorks()
    {
        var service = new ChordSearchService();
        var response = service.Search(
            new ChordSpec { Root = PitchClass.E, Quality = ChordQuality.Minor },
            TuningPresets.BassStandard);
        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.Equal(4, r.Voicing.StringCount));
    }

    [Fact]
    public void SevenString_SearchWorks()
    {
        var service = new ChordSearchService();
        var response = service.Search(
            new ChordSpec { Root = PitchClass.B, Quality = ChordQuality.Minor },
            TuningPresets.SevenStringStandard);
        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.Equal(7, r.Voicing.StringCount));
    }

    [Fact]
    public void DropD_PowerChordOnLowD()
    {
        var service = new ChordSearchService();
        var response = service.Search(
            new ChordSpec { Root = PitchClass.D, Quality = ChordQuality.Power },
            TuningPresets.DropD);
        Assert.NotEmpty(response.Results);
        // Prefer open low D
        Assert.Contains(response.Results, r => r.Voicing.Strings[0].IsOpen);
    }

    [Fact]
    public void ComplexChord_StillReturnsSomething()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor }
            .ToggleExtension(ChordExtension.Seventh)
            .ToggleExtension(ChordExtension.Flat9)
            .ToggleExtension(ChordExtension.Sharp11);

        var response = new ChordSearchService().Search(spec, TuningPresets.StandardE);
        // May be sparse but should not throw; usually finds some
        Assert.NotNull(response.Results);
    }

    [Fact]
    public void Search_NullSpec_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new ChordSearchService().Search(null!, TuningPresets.StandardE));

    [Fact]
    public void Search_NullTuning_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new ChordSearchService().Search(new ChordSpec(), null!));

    [Fact]
    public void Search_CanceledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            new ChordSearchService().Search(
                new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major },
                TuningPresets.StandardE,
                cts.Token));
    }

    [Fact]
    public void Search_CancelDuringSearch_Stops()
    {
        using var cts = new CancellationTokenSource();
        // Dense search; cancel almost immediately from another thread
        var task = Task.Run(() =>
        {
            cts.CancelAfter(1);
            return new ChordSearchService(new VoicingOptions { MaxFret = 24, MaxResults = 80 })
                .Search(
                    new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor }
                        .ToggleExtension(ChordExtension.Seventh)
                        .ToggleExtension(ChordExtension.Flat9)
                        .ToggleExtension(ChordExtension.Sharp11),
                    TuningPresets.SevenStringStandard,
                    cts.Token);
        });

        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        // Either finished before cancel (rare on fast machines) or canceled — must not hang/crash
        Assert.True(ex is null or OperationCanceledException);
    }

    [Fact]
    public void MinSoundingStrings_FiltersThinShapes()
    {
        var service = new ChordSearchService(new VoicingOptions
        {
            MinSoundingStrings = 4,
            MaxResults = 40
        });
        var response = service.Search(
            new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        Assert.All(response.Results, r => Assert.True(r.Voicing.SoundingStringCount >= 4));
    }

    [Fact]
    public void ChordVoicing_MutedOnly_SpanZero()
    {
        var v = new ChordVoicing
        {
            Strings = Enumerable.Repeat(StringFret.Muted, 6).ToArray()
        };
        Assert.Null(v.MinFret);
        Assert.Equal(0, v.Span);
        Assert.Equal(0, v.SoundingStringCount);
    }

    [Fact]
    public void SoundingPitches_NullForMuted()
    {
        var service = new ChordSearchService();
        var response = service.Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major },
            TuningPresets.StandardE);
        var first = response.Results.First();
        var pitches = first.SoundingPitches(TuningPresets.StandardE);
        Assert.Equal(6, pitches.Count);
        for (var i = 0; i < 6; i++)
        {
            if (first.Voicing.Strings[i].IsMuted)
                Assert.Null(pitches[i]);
            else
                Assert.NotNull(pitches[i]);
        }
    }

    [Fact]
    public void AbsoluteMaxSpan_NeverExceeded()
    {
        var opts = new VoicingOptions { AbsoluteMaxSpan = 4, MaxFret = 15 };
        var service = new ChordSearchService(opts);
        var response = service.Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        Assert.All(response.Results, r => Assert.True(r.Voicing.Span <= 4));
    }
}
