using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Core.Voicing;

public sealed class VoicingEngine
{
    private readonly VoicingOptions _options;

    public VoicingEngine(VoicingOptions? options = null)
        => _options = options ?? new VoicingOptions();

    public IReadOnlyList<(ChordVoicing Voicing, double Score)> FindVoicings(
        ChordFormula formula,
        GuitarTuning tuning,
        CancellationToken cancellationToken = default)
    {
        var scored = new List<(ChordVoicing Voicing, double Score)>();
        var seen = new HashSet<string>();

        // Search by sliding fret windows across the neck (keeps combos tractable).
        for (var windowStart = 0; windowStart <= _options.MaxFret; windowStart++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var windowEnd = Math.Min(_options.MaxFret, windowStart + _options.AbsoluteMaxSpan);
            EnumerateWindow(formula, tuning, windowStart, windowEnd, scored, seen, cancellationToken);
        }

        return scored
            .OrderBy(r => r.Score)
            .GroupBy(r => NormalizeKey(r.Voicing))
            .Select(g => g.First())
            .Take(_options.MaxResults * 3)
            .ToList();
    }

    private void EnumerateWindow(
        ChordFormula formula,
        GuitarTuning tuning,
        int windowStart,
        int windowEnd,
        List<(ChordVoicing Voicing, double Score)> scored,
        HashSet<string> seen,
        CancellationToken cancellationToken)
    {
        var stringCount = tuning.StringCount;
        var candidates = new List<int>[stringCount];

        for (var s = 0; s < stringCount; s++)
        {
            var list = new List<int> { -1 };
            // Open string if it fits the chord and window allows open (open-position windows)
            var openPc = tuning.OpenPitch(s).PitchClass;
            if (formula.PitchClasses.Contains(openPc))
                list.Add(0);

            for (var fret = Math.Max(1, windowStart); fret <= windowEnd; fret++)
            {
                var pc = tuning.PitchAt(s, fret).PitchClass;
                if (formula.PitchClasses.Contains(pc))
                    list.Add(fret);
            }

            candidates[s] = list.Distinct().ToList();
        }

        var frets = new int[stringCount];
        var local = new List<(ChordVoicing Voicing, double Score)>();
        Search(0, frets, candidates, formula, tuning, local, maxResults: 400, cancellationToken);

        foreach (var item in local)
        {
            if (seen.Add(item.Voicing.ShapeKey))
                scored.Add(item);
        }
    }

    private void Search(
        int stringIndex,
        int[] frets,
        List<int>[] candidates,
        ChordFormula formula,
        GuitarTuning tuning,
        List<(ChordVoicing Voicing, double Score)> results,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (results.Count >= maxResults)
            return;

        // Cheap cooperative cancel — once per string layer, not every leaf.
        if (stringIndex == 0)
            cancellationToken.ThrowIfCancellationRequested();

        if (stringIndex == frets.Length)
        {
            if (TryScore(frets, formula, tuning, out var score))
            {
                results.Add((new ChordVoicing
                {
                    Strings = frets.Select(f => new StringFret(f)).ToArray()
                }, score));
            }
            return;
        }

        foreach (var fret in candidates[stringIndex])
        {
            frets[stringIndex] = fret;
            if (!PartialSpanOk(frets, stringIndex))
                continue;
            Search(stringIndex + 1, frets, candidates, formula, tuning, results, maxResults, cancellationToken);
            if (results.Count >= maxResults)
                return;
        }
    }

    private bool PartialSpanOk(int[] frets, int lastIndex)
    {
        int? min = null, max = null;
        for (var i = 0; i <= lastIndex; i++)
        {
            if (frets[i] <= 0) continue;
            min = min is null ? frets[i] : Math.Min(min.Value, frets[i]);
            max = max is null ? frets[i] : Math.Max(max.Value, frets[i]);
        }
        if (min is null || max is null) return true;
        return max.Value - min.Value <= _options.AbsoluteMaxSpan;
    }

    private bool TryScore(int[] frets, ChordFormula formula, GuitarTuning tuning, out double score)
    {
        score = 0;
        var sounding = 0;
        int? minF = null, maxF = null;
        var covered = new HashSet<PitchClass>();
        PitchClass? lowestPc = null;
        int? lowestMidi = null;
        var firstSounding = -1;
        var lastSounding = -1;

        for (var s = 0; s < frets.Length; s++)
        {
            if (frets[s] < 0) continue;
            sounding++;
            if (firstSounding < 0) firstSounding = s;
            lastSounding = s;

            var pitch = tuning.PitchAt(s, frets[s]);
            covered.Add(pitch.PitchClass);
            if (lowestMidi is null || pitch.MidiNote < lowestMidi)
            {
                lowestMidi = pitch.MidiNote;
                lowestPc = pitch.PitchClass;
            }

            if (frets[s] > 0)
            {
                minF = minF is null ? frets[s] : Math.Min(minF.Value, frets[s]);
                maxF = maxF is null ? frets[s] : Math.Max(maxF.Value, frets[s]);
            }
        }

        if (sounding < _options.MinSoundingStrings)
            return false;

        foreach (var req in formula.RequiredPitchClasses)
        {
            if (!covered.Contains(req))
                return false;
        }

        // Slash bass must be chord tone (also enforced upstream in UI)
        if (formula.Bass is { } bass)
        {
            if (lowestPc != bass)
                return false;
            if (!formula.PitchClasses.Contains(bass))
                return false;
        }
        else if (lowestPc != formula.Root)
        {
            score += 1.5;
        }

        // Contiguous sounding block — no “dead” strings in the middle
        if (firstSounding >= 0)
        {
            for (var s = firstSounding; s <= lastSounding; s++)
            {
                if (frets[s] < 0)
                {
                    if (!_options.AllowMutedInteriorStrings)
                        return false;
                    score += 2.5;
                }
            }
        }

        var frettedCount = frets.Count(f => f > 0);
        var barreBonus = BarreBonus(frets);
        var maxSameFret = frets.Where(f => f > 0).GroupBy(f => f).Select(g => g.Count()).DefaultIfEmpty(0).Max();
        // A usable barre for stretch relief must sit on the lowest fretted fret
        // (index finger). A higher-fret mini-barre with notes behind it is unplayable.
        var lowBarreCount = minF is null ? 0 : frets.Count(f => f == minF.Value);

        // 5+ fretted notes need a repeated fret (barre opportunity)
        if (_options.RequireBarreForMoreThanFourFrets && frettedCount > 4 && maxSameFret < 2)
            return false;

        var span = (minF is null || maxF is null) ? 0 : maxF.Value - minF.Value;
        if (span > _options.AbsoluteMaxSpan)
            return false;

        // Wide stretch without a low-fret barre is impractical
        // (includes 2-finger stretches like fret 1 + fret 5 with opens in between)
        if (_options.RequireBarreForWideSpan
            && span > _options.ComfortableSpan
            && frettedCount >= 2
            && lowBarreCount < 2)
            return false;

        score += span * 1.4;
        if (span > _options.ComfortableSpan)
            score += (span - _options.ComfortableSpan) * 4.0;

        if (minF is > 0)
            score += minF.Value * 0.08;

        score -= frets.Count(f => f == 0) * 0.4;
        score += Math.Max(0, sounding - covered.Count) * 0.25;
        score += formula.PitchClasses.Count(pc => !covered.Contains(pc)) * 1.1;
        score -= barreBonus * 0.7;

        // Tiny sparse shapes high on the neck: soft penalty
        if (sounding == _options.MinSoundingStrings && minF is >= 8)
            score += 1.2;

        if (minF is null && sounding < formula.RequiredPitchClasses.Count)
            return false;

        return true;
    }

    private static double BarreBonus(int[] frets)
    {
        double bonus = 0;
        for (var s = 0; s < frets.Length - 1; s++)
        {
            if (frets[s] > 0 && frets[s] == frets[s + 1])
                bonus += 1;
        }
        return bonus;
    }

    private static string NormalizeKey(ChordVoicing v)
    {
        var min = v.MinFret;
        if (min is null or <= 1)
            return v.ShapeKey;

        return string.Join("-", v.Strings.Select(s =>
            s.IsMuted ? -1 : s.IsOpen ? 0 : s.Fret - min.Value + 1));
    }
}
