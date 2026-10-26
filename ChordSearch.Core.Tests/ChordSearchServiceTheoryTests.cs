using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Core.Tests;

public class ChordSearchServiceTheoryTests
{
    private readonly ChordSearchService _service = new();

    [Fact]
    public void Response_ContainsTheorySummary_AndIntervals()
    {
        var response = _service.Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor }
                .ToggleExtension(ChordExtension.Seventh),
            TuningPresets.StandardE);

        Assert.Equal("Cm7", response.DisplayName);
        Assert.Contains("Minor", response.TheorySummary);
        Assert.NotEmpty(response.Intervals);
        Assert.NotEmpty(response.PitchClasses);
        Assert.Contains("C", response.PitchClasses);
        // C root defaults to sharp spelling → D# / A# for m7 tones
        Assert.True(
            response.PitchClasses.Contains("D#") || response.PitchClasses.Contains("Eb"),
            "Expected minor third spelling");
    }

    [Fact]
    public void EveryResult_HasMatchingFingeringLength()
    {
        var response = _service.Search(
            new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        Assert.All(response.Results, r =>
        {
            Assert.Equal(r.Voicing.StringCount, r.Fingering.Fingers.Count);
            Assert.True(double.IsFinite(r.Score));
            Assert.False(string.IsNullOrWhiteSpace(r.Label));
        });
    }

    [Fact]
    public void Results_AreSortedByScoreAscending()
    {
        var response = _service.Search(
            new ChordSpec { Root = PitchClass.D, Quality = ChordQuality.Major },
            TuningPresets.StandardE);

        var scores = response.Results.Select(r => r.Score).ToList();
        Assert.Equal(scores.OrderBy(s => s), scores);
    }

    [Fact]
    public void OpenG_Tuning_FindsGMajor()
    {
        var response = _service.Search(
            new ChordSpec { Root = PitchClass.G, Quality = ChordQuality.Major },
            TuningPresets.OpenG);
        Assert.NotEmpty(response.Results);
        // Open G should favor many open strings
        Assert.Contains(response.Results, r => r.Voicing.Strings.Count(s => s.IsOpen) >= 3);
    }

    [Fact]
    public void Augmented_FindsVoicings()
    {
        var response = _service.Search(
            new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Augmented },
            TuningPresets.StandardE);
        Assert.NotEmpty(response.Results);
    }

    [Fact]
    public void Sus2_And_Sus4_Distinct()
    {
        var sus2 = _service.Search(new ChordSpec { Root = PitchClass.D, Quality = ChordQuality.Sus2 }, TuningPresets.StandardE);
        var sus4 = _service.Search(new ChordSpec { Root = PitchClass.D, Quality = ChordQuality.Sus4 }, TuningPresets.StandardE);
        Assert.NotEqual(sus2.DisplayName, sus4.DisplayName);
        Assert.NotEmpty(sus2.Results);
        Assert.NotEmpty(sus4.Results);
    }
}
