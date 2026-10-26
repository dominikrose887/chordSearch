using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Core.Tests;

public class VoicingEngineTests
{
    private readonly ChordSearchService _service = new();

    [Fact]
    public void CMajor_FindsOpenShapes()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Major };
        var response = _service.Search(spec, TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);
        Assert.Contains(response.Results, r => r.Voicing.Strings.Any(s => s.IsOpen) || r.Position <= 3);
    }

    [Fact]
    public void FMajor_FindsBarreFriendlyShapes()
    {
        var spec = new ChordSpec { Root = PitchClass.F, Quality = ChordQuality.Major };
        var response = _service.Search(spec, TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);
        Assert.Contains(response.Results, r => r.HasBarre || r.Position >= 1);
    }

    [Fact]
    public void Cm7_ReturnsMultipleVoicings()
    {
        var spec = new ChordSpec { Root = PitchClass.C, Quality = ChordQuality.Minor }
            .ToggleExtension(ChordExtension.Seventh);

        var response = _service.Search(spec, TuningPresets.StandardE);
        Assert.True(response.Results.Count >= 3);
        Assert.Equal("Cm7", response.DisplayName);
    }

    [Fact]
    public void SlashBass_LowestPitchMatches()
    {
        var spec = new ChordSpec
        {
            Root = PitchClass.D,
            Quality = ChordQuality.Major,
            Bass = PitchClass.Fs
        };

        var response = _service.Search(spec, TuningPresets.StandardE);
        Assert.NotEmpty(response.Results);

        foreach (var result in response.Results.Take(5))
        {
            var pitches = result.SoundingPitches(TuningPresets.StandardE)
                .Where(p => p is not null)
                .Select(p => p!.Value)
                .ToList();
            Assert.NotEmpty(pitches);
            var lowest = pitches.MinBy(p => p.MidiNote);
            Assert.Equal(PitchClass.Fs, lowest.PitchClass);
        }
    }

    [Fact]
    public void Fingering_RejectsImpossibleStretch_AdjacentFingersFarApart()
    {
        // Engine should not return shapes where adjacent fingers span 5+ frets
        var spec = new ChordSpec { Root = PitchClass.A, Quality = ChordQuality.Minor };
        var response = _service.Search(spec, TuningPresets.StandardE);

        foreach (var result in response.Results)
        {
            var placed = Enumerable.Range(0, result.Voicing.StringCount)
                .Where(s => result.Fingering.Fingers[s].IsAssigned)
                .Select(s => (Finger: result.Fingering.Fingers[s].Value, Fret: result.Voicing.Strings[s].Fret))
                .ToList();

            for (var i = 0; i < placed.Count; i++)
            {
                for (var j = i + 1; j < placed.Count; j++)
                {
                    if (Math.Abs(placed[i].Finger - placed[j].Finger) == 1)
                        Assert.True(Math.Abs(placed[i].Fret - placed[j].Fret) < 5);
                }
            }
        }
    }

    [Fact]
    public void Pitch_Parse_RoundTrip()
    {
        var p = Pitch.Parse("D#4");
        Assert.Equal(PitchClass.Ds, p.PitchClass);
        Assert.Equal(4, p.Octave);
        Assert.Equal("E2", Pitch.Parse("E2").ToDisplayName());
    }
}
