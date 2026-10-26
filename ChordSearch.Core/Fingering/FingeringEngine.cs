using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Fingering;

public sealed class FingeringEngine
{
    private const int MaxFingerStretch = 3; // frets between finger N and N+1 comfortably; absolute checked separately

    public ChordFingering? Assign(ChordVoicing voicing)
    {
        ArgumentNullException.ThrowIfNull(voicing);
        if (voicing.Strings.Count == 0)
            return null;

        var fretted = new List<(int String, int Fret)>();
        for (var s = 0; s < voicing.StringCount; s++)
        {
            if (voicing.Strings[s].IsFretted)
                fretted.Add((s, voicing.Strings[s].Fret));
        }

        if (fretted.Count == 0)
        {
            return new ChordFingering
            {
                Fingers = Enumerable.Repeat(FingerId.None, voicing.StringCount).ToArray(),
                DifficultyScore = 0,
                Notes = "Open strings only"
            };
        }

        var barres = DetectBarres(fretted, voicing.StringCount);
        var best = FindBestAssignment(voicing, fretted, barres);
        return best;
    }

    private static List<BarreSegment> DetectBarres(List<(int String, int Fret)> fretted, int stringCount)
    {
        var barres = new List<BarreSegment>();
        var byFret = fretted.GroupBy(f => f.Fret).OrderBy(g => g.Key);

        foreach (var group in byFret)
        {
            var strings = group.Select(g => g.String).OrderBy(s => s).ToList();
            // Find contiguous runs of length >= 2
            var runStart = 0;
            for (var i = 1; i <= strings.Count; i++)
            {
                var unbroken = i < strings.Count && strings[i] == strings[i - 1] + 1;
                if (unbroken) continue;

                var runLen = i - runStart;
                if (runLen >= 2)
                {
                    barres.Add(new BarreSegment
                    {
                        Fret = group.Key,
                        Finger = 1, // typically index; may be remapped
                        FromString = strings[runStart],
                        EndString = strings[i - 1]
                    });
                }
                runStart = i;
            }
        }

        // Prefer the lowest fret barre as primary (classic barre chords)
        return barres.OrderBy(b => b.Fret).ThenByDescending(b => b.EndString - b.FromString).ToList();
    }

    private ChordFingering? FindBestAssignment(
        ChordVoicing voicing,
        List<(int String, int Fret)> fretted,
        List<BarreSegment> barres)
    {
        var minFret = fretted.Min(f => f.Fret);
        var maxFret = fretted.Max(f => f.Fret);
        var span = maxFret - minFret;

        if (span > 6)
            return null;

        // Only barres with no fretted notes behind them are usable (can't fret nut-side of index).
        var usableBarres = barres
            .Where(b => fretted.All(f => f.Fret >= b.Fret))
            .ToList();

        ChordFingering? best = null;
        foreach (var primaryBarre in usableBarres.Select(b => (BarreSegment?)b).Append(null))
        {
            var coveredByBarre = new HashSet<int>();
            if (primaryBarre is not null)
            {
                for (var s = primaryBarre.FromString; s <= primaryBarre.EndString; s++)
                {
                    if (voicing.Strings[s].Fret == primaryBarre.Fret)
                        coveredByBarre.Add(s);
                }
            }

            var remaining = fretted
                .Where(f => !coveredByBarre.Contains(f.String))
                .OrderBy(f => f.Fret)
                .ThenBy(f => f.String)
                .ToList();

            // Finger 1 reserved for barre if present
            var availableFingers = primaryBarre is not null
                ? new[] { 2, 3, 4 }
                : new[] { 1, 2, 3, 4 };

            if (remaining.Count > availableFingers.Length)
                continue;

            AssignRemaining(
                voicing,
                remaining,
                availableFingers,
                0,
                new int[voicing.StringCount],
                primaryBarre,
                coveredByBarre,
                minFret,
                ref best);
        }

        if (best is null)
            best = TryBruteForce(voicing, fretted, minFret);

        return best;
    }

    private void AssignRemaining(
        ChordVoicing voicing,
        List<(int String, int Fret)> remaining,
        int[] fingers,
        int index,
        int[] assignment,
        BarreSegment? barre,
        HashSet<int> barreStrings,
        int minFret,
        ref ChordFingering? best)
    {
        if (index == remaining.Count)
        {
            var fingering = BuildFingering(voicing, assignment, barre, barreStrings, minFret);
            if (fingering is null) return;
            if (best is null || fingering.DifficultyScore < best.DifficultyScore)
                best = fingering;
            return;
        }

        var (str, fret) = remaining[index];
        foreach (var finger in fingers)
        {
            if (assignment.Contains(finger))
            {
                // Same finger only allowed on the same fret (mini-barre)
                var existing = Array.IndexOf(assignment, finger);
                if (existing >= 0 && voicing.Strings[existing].Fret != fret)
                    continue;
            }

            // Stretch vs lower fingers already placed
            if (!StretchOk(voicing, assignment, str, fret, finger, minFret))
                continue;

            assignment[str] = finger;
            AssignRemaining(voicing, remaining, fingers, index + 1, assignment, barre, barreStrings, minFret, ref best);
            assignment[str] = 0;
        }
    }

    private static bool StretchOk(
        ChordVoicing voicing,
        int[] assignment,
        int str,
        int fret,
        int finger,
        int minFret)
    {
        for (var s = 0; s < assignment.Length; s++)
        {
            var otherFinger = assignment[s];
            if (otherFinger == 0) continue;
            var otherFret = voicing.Strings[s].Fret;
            if (otherFret <= 0) continue;

            var fingerDelta = Math.Abs(finger - otherFinger);
            var fretDelta = Math.Abs(fret - otherFret);

            // Same finger different fret impossible (unless barre — not here)
            if (finger == otherFinger && fret != otherFret)
                return false;

            // Adjacent fingers: at most MaxFingerStretch frets apart
            if (fingerDelta == 1 && fretDelta > MaxFingerStretch)
                return false;

            // Non-adjacent: allow roughly one extra fret per skipped finger
            if (fingerDelta > 1 && fretDelta > MaxFingerStretch + (fingerDelta - 1))
                return false;

            // Crossing: higher finger on lower fret is awkward
            if (finger > otherFinger && fret < otherFret - 1)
                return false;
            if (finger < otherFinger && fret > otherFret + 1)
                return false;
        }

        // Prefer finger 1 near min fret when no barre
        if (finger == 1 && fret > minFret + 1)
            return false;

        return true;
    }

    private ChordFingering? TryBruteForce(
        ChordVoicing voicing,
        List<(int String, int Fret)> fretted,
        int minFret)
    {
        // Greedy: sort by fret then assign lowest available finger
        var assignment = new int[voicing.StringCount];
        var used = new HashSet<int>();
        var ordered = fretted.OrderBy(f => f.Fret).ThenBy(f => f.String).ToList();

        // Detect if all at same fret → full barre with finger 1
        if (ordered.Select(o => o.Fret).Distinct().Count() == 1)
        {
            var fret = ordered[0].Fret;
            var minS = ordered.Min(o => o.String);
            var maxS = ordered.Max(o => o.String);
            for (var s = minS; s <= maxS; s++)
            {
                if (voicing.Strings[s].Fret == fret)
                    assignment[s] = 1;
            }

            return BuildFingering(voicing, assignment, new BarreSegment
            {
                Fret = fret,
                Finger = 1,
                FromString = minS,
                EndString = maxS
            }, Enumerable.Range(minS, maxS - minS + 1).ToHashSet(), minFret);
        }

        foreach (var (str, fret) in ordered)
        {
            var relative = fret - minFret;
            var preferred = Math.Clamp(relative + 1, 1, 4);
            var chosen = 0;
            for (var delta = 0; delta <= 3; delta++)
            {
                foreach (var cand in new[] { preferred + delta, preferred - delta }.Where(c => c is >= 1 and <= 4))
                {
                    if (used.Contains(cand)) continue;
                    if (!StretchOk(voicing, assignment, str, fret, cand, minFret)) continue;
                    chosen = cand;
                    break;
                }
                if (chosen != 0) break;
            }

            if (chosen == 0)
                return null;

            assignment[str] = chosen;
            used.Add(chosen);
        }

        return BuildFingering(voicing, assignment, null, [], minFret);
    }

    private static ChordFingering? BuildFingering(
        ChordVoicing voicing,
        int[] assignment,
        BarreSegment? barre,
        HashSet<int> barreStrings,
        int minFret)
    {
        var fingers = new FingerId[voicing.StringCount];
        for (var s = 0; s < voicing.StringCount; s++)
        {
            if (voicing.Strings[s].IsMuted || voicing.Strings[s].IsOpen)
            {
                fingers[s] = FingerId.None;
                continue;
            }

            if (barre is not null && barreStrings.Contains(s) && voicing.Strings[s].Fret == barre.Fret)
            {
                fingers[s] = new FingerId(barre.Finger);
                continue;
            }

            if (assignment[s] == 0)
                return null;

            fingers[s] = new FingerId(assignment[s]);
        }

        // Validate no finger used on two different frets
        var fingerFrets = new Dictionary<int, int>();
        for (var s = 0; s < fingers.Length; s++)
        {
            if (!fingers[s].IsAssigned) continue;
            var f = fingers[s].Value;
            var fret = voicing.Strings[s].Fret;
            if (fingerFrets.TryGetValue(f, out var existing) && existing != fret)
                return null;
            fingerFrets[f] = fret;
        }

        // Crossing penalty / difficulty
        double difficulty = voicing.Span * 1.5;
        difficulty += minFret * 0.05;

        if (barre is not null)
        {
            var width = barre.EndString - barre.FromString + 1;
            difficulty += width * 0.4; // barre is harder with width
            difficulty -= 0.5; // but often the correct shape
        }

        // Finger stretch costs
        var placed = Enumerable.Range(0, fingers.Length)
            .Where(s => fingers[s].IsAssigned)
            .Select(s => (Finger: fingers[s].Value, Fret: voicing.Strings[s].Fret, String: s))
            .OrderBy(p => p.Finger)
            .ToList();

        for (var i = 0; i < placed.Count - 1; i++)
        {
            var a = placed[i];
            var b = placed[i + 1];
            if (b.Finger == a.Finger) continue;
            var fretDelta = Math.Abs(b.Fret - a.Fret);
            var fingerDelta = b.Finger - a.Finger;
            if (fingerDelta > 0 && fretDelta > fingerDelta + 1)
                difficulty += (fretDelta - fingerDelta) * 1.2;
        }

        // Hard reject extreme stretches that slipped past StretchOk
        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                var fingerDelta = Math.Abs(placed[i].Finger - placed[j].Finger);
                var fretDelta = Math.Abs(placed[i].Fret - placed[j].Fret);
                if (fingerDelta == 1 && fretDelta > MaxFingerStretch)
                    return null;
                if (fingerDelta > 1 && fretDelta > MaxFingerStretch + (fingerDelta - 1))
                    return null;
            }
        }

        var barres = barre is null ? Array.Empty<BarreSegment>() : new[] { barre };

        return new ChordFingering
        {
            Fingers = fingers,
            Barres = barres,
            DifficultyScore = difficulty,
            Notes = barre is null ? null : $"Barre fret {barre.Fret}"
        };
    }
}
