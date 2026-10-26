using ChordSearch.Core.Chords;
using ChordSearch.Core.Fingering;
using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class VoicingPresentationTests
{
    [Fact]
    public void DifficultyLabel_Bands()
    {
        Assert.Equal("Easy", new VoicingResult { Score = 0.5 }.DifficultyLabel);
        Assert.Equal("Easy", new VoicingResult { Score = 2.4 }.DifficultyLabel);
        Assert.Equal("Medium", new VoicingResult { Score = 2.5 }.DifficultyLabel);
        Assert.Equal("Medium", new VoicingResult { Score = 5.9 }.DifficultyLabel);
        Assert.Equal("Harder", new VoicingResult { Score = 6.0 }.DifficultyLabel);
    }

    [Fact]
    public void IsOpenPosition_ByMaxFret()
    {
        var open = new VoicingResult
        {
            Voicing = new ChordVoicing
            {
                Strings = new[] { -1, 3, 2, 0, 1, 0 }.Select(f => new StringFret(f)).ToArray()
            }
        };
        Assert.True(open.IsOpenPosition);

        var fifth = new VoicingResult
        {
            Voicing = new ChordVoicing
            {
                Strings = new[] { -1, -1, 5, 5, 5, 0 }.Select(f => new StringFret(f)).ToArray()
            }
        };
        Assert.False(fifth.IsOpenPosition);
    }

    [Fact]
    public void FingerCount_DistinctAssigned()
    {
        var r = new VoicingResult
        {
            Fingering = new ChordFingering
            {
                Fingers =
                [
                    FingerId.None, FingerId.None,
                    new(1), new(1), new(2), FingerId.None
                ],
                Barres = [new BarreSegment { Fret = 5, Finger = 1, FromString = 2, EndString = 3 }]
            }
        };
        Assert.Equal(2, r.FingerCount);
        Assert.True(r.HasBarre);
    }

    [Fact]
    public void Search_LabelsHaveNoShapeKeyOrRawScore()
    {
        var response = new ChordSearchService().Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);
        foreach (var r in response.Results)
        {
            Assert.DoesNotContain("-1", r.Label);
            Assert.DoesNotContain(r.Score.ToString(), r.Label);
            Assert.False(string.IsNullOrWhiteSpace(r.Label));
            Assert.Contains(r.DifficultyLabel, new[] { "Easy", "Medium", "Harder" });
        }

        // Sorted easy → harder (by score)
        for (var i = 1; i < response.Results.Count; i++)
            Assert.True(response.Results[i].Score >= response.Results[i - 1].Score);
    }

    [Fact]
    public void Search_RootLow_ForcesRootBass()
    {
        var response = new ChordSearchService().Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major, Bass = PitchClass.C },
            TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);
        foreach (var r in response.Results.Take(15))
        {
            var pitches = r.SoundingPitches(TuningPresets.StandardE)
                .Where(p => p is not null).Select(p => p!.Value).ToList();
            Assert.Equal(PitchClass.C, pitches.MinBy(p => p.MidiNote).PitchClass);
        }
    }
}
