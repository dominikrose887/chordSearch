using ChordSearch.Core.Chords;
using ChordSearch.Core.Fingering;
using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Services;

public sealed class ChordSearchService
{
    private readonly object _gate = new();
    private readonly FingeringEngine _fingeringEngine = new();
    private VoicingOptions _options;

    public ChordSearchService(VoicingOptions? options = null)
    {
        _options = options ?? new VoicingOptions();
    }

    public VoicingOptions Options
    {
        get { lock (_gate) return _options.Clone(); }
    }

    public void UpdateOptions(VoicingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate)
            _options = options.Clone();
    }

    public void SetMaxFret(int maxFret)
    {
        lock (_gate)
            _options.MaxFret = Math.Clamp(maxFret, 12, 24);
    }

    public ChordSearchResponse Search(
        ChordSpec spec,
        GuitarTuning tuning,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(tuning);

        // Snapshot so SetMaxFret / UpdateOptions cannot race mid-search
        VoicingOptions options;
        lock (_gate)
            options = _options.Clone();

        var formula = ChordFormula.FromSpec(spec);

        // Reject slash chords whose bass is not a chord tone — before the expensive search
        if (spec.Bass is not null && !ChordBassOptions.IsValidBass(spec))
        {
            return new ChordSearchResponse
            {
                Spec = spec,
                Formula = formula,
                DisplayName = spec.DisplayName,
                TheorySummary = BuildTheorySummary(spec, formula) + " Invalid bass (not a chord tone).",
                Intervals = formula.Intervals.OrderBy(i => i).ToArray(),
                PitchClasses = formula.PitchClasses
                    .Select(pc => PitchClassNames.ToDisplayName(pc, spec.AccidentalPreference))
                    .ToArray(),
                Results = []
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
        var voicingEngine = new VoicingEngine(options);
        var voicings = voicingEngine.FindVoicings(formula, tuning, cancellationToken);

        var results = new List<VoicingResult>();
        foreach (var (voicing, voiceScore) in voicings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fingering = _fingeringEngine.Assign(voicing);
            if (fingering is null)
                continue;

            if (fingering.DifficultyScore > options.MaxFingeringDifficulty)
                continue;

            var total = voiceScore + fingering.DifficultyScore * 0.85;
            results.Add(new VoicingResult
            {
                Voicing = voicing,
                Fingering = fingering,
                Score = total,
                Label = "",
                Detail = fingering.Notes
            });

            if (results.Count >= options.MaxResults * 2)
                break;
        }

        var ranked = results
            .OrderBy(r => r.Score)
            .Take(options.MaxResults)
            .ToList();

        foreach (var r in ranked)
            r.Label = BuildLabel(r);

        return new ChordSearchResponse
        {
            Spec = spec,
            Formula = formula,
            DisplayName = spec.DisplayName,
            TheorySummary = BuildTheorySummary(spec, formula),
            Intervals = formula.Intervals.OrderBy(i => i).ToArray(),
            PitchClasses = formula.PitchClasses
                .Select(pc => PitchClassNames.ToDisplayName(pc, spec.AccidentalPreference))
                .ToArray(),
            Results = ranked
        };
    }

    private static string BuildLabel(VoicingResult result)
    {
        var voicing = result.Voicing;
        var parts = new List<string>();

        if (result.IsOpenPosition)
            parts.Add("Open");
        else
            parts.Add($"{result.Position}. fret");

        parts.Add($"{voicing.SoundingStringCount} strings");

        if (result.HasBarre)
            parts.Add("barre");

        return string.Join(" · ", parts);
    }

    private static string BuildTheorySummary(ChordSpec spec, ChordFormula formula)
    {
        var root = PitchClassNames.ToDisplayName(spec.Root, spec.AccidentalPreference);
        var quality = ChordQualityInfo.DisplayName(spec.Quality);
        var intervals = string.Join(", ", formula.Intervals.OrderBy(i => i).Select(IntervalName));
        var tones = string.Join(" – ", formula.PitchClasses
            .OrderBy(pc => ((int)pc - (int)spec.Root + 12) % 12)
            .Select(pc => PitchClassNames.ToDisplayName(pc, spec.AccidentalPreference)));

        var summary = $"{root} {quality}: intervals [{intervals}]. Tones: {tones}.";
        if (spec.Bass is { } bass && bass != spec.Root)
            summary += $" Bass: {PitchClassNames.ToDisplayName(bass, spec.AccidentalPreference)}.";
        return summary;
    }

    private static string IntervalName(int semitones) => (semitones % 12) switch
    {
        0 => "1",
        1 => "b2",
        2 => "2",
        3 => "b3",
        4 => "3",
        5 => "4",
        6 => "b5/#11",
        7 => "5",
        8 => "#5/b13",
        9 => "6/bb7",
        10 => "b7",
        11 => "7",
        _ => semitones.ToString()
    };
}

public sealed class ChordSearchResponse
{
    public required ChordSpec Spec { get; init; }
    public required ChordFormula Formula { get; init; }
    public required string DisplayName { get; init; }
    public required string TheorySummary { get; init; }
    public required IReadOnlyList<int> Intervals { get; init; }
    public required IReadOnlyList<string> PitchClasses { get; init; }
    public required IReadOnlyList<VoicingResult> Results { get; init; }
}

file static class FingeringExtensions
{
    public static bool HasBarre(this ChordFingering f) => f.Barres.Count > 0;
}
