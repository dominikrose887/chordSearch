using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Fingering;

/// <summary>
/// Searches for a physically playable finger assignment (1=index … 4=pinky).
/// Shapes with no legal assignment are rejected (null); playable shapes get a difficulty score.
/// </summary>
public sealed class FingeringEngine
{
    /// <summary>Hard ceiling on fret span; beyond this no hand assignment is attempted.</summary>
    private const int AbsoluteMaxAssignableSpan = 6;

    /// <summary>Bound backtracking: stop after this many complete legal assignments scored.</summary>
    private const int MaxSolutionsToScore = 48;

    /// <summary>Bound node expansions in the search tree.</summary>
    private const int MaxSearchNodes = 8_000;

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

        var minFret = fretted.Min(f => f.Fret);
        var maxFret = fretted.Max(f => f.Fret);
        var span = maxFret - minFret;
        if (span > AbsoluteMaxAssignableSpan)
            return null;

        // Average fretted fret drives position-dependent stretch limits.
        var avgFret = fretted.Average(f => (double)f.Fret);

        // All fretted notes on one fret → classic barre with finger 1 when the span is clear.
        if (fretted.Select(f => f.Fret).Distinct().Count() == 1)
        {
            var fret = fretted[0].Fret;
            var minS = fretted.Min(f => f.String);
            var maxS = fretted.Max(f => f.String);
            if (BarreSpanClearOfLowerNotes(voicing, minS, maxS, fret))
            {
                var assignment = new int[voicing.StringCount];
                foreach (var (s, _) in fretted)
                    assignment[s] = 1;
                return BuildFingering(voicing, assignment, avgFret);
            }
        }

        // Search bass → treble; prefer index-ward fingers on thick strings.
        var ordered = fretted.OrderBy(f => f.String).ToList();
        var assignmentSearch = new int[voicing.StringCount];
        ChordFingering? best = null;
        var nodes = 0;
        var solutions = 0;

        Search(
            voicing,
            ordered,
            index: 0,
            assignmentSearch,
            avgFret,
            ref best,
            ref nodes,
            ref solutions);

        return best;
    }

    private static void Search(
        ChordVoicing voicing,
        List<(int String, int Fret)> notes,
        int index,
        int[] assignment,
        double avgFret,
        ref ChordFingering? best,
        ref int nodes,
        ref int solutions)
    {
        if (solutions >= MaxSolutionsToScore || nodes >= MaxSearchNodes)
            return;

        if (index == notes.Count)
        {
            nodes++;
            var built = BuildFingering(voicing, assignment, avgFret);
            if (built is null)
                return;

            solutions++;
            if (best is null || built.DifficultyScore < best.DifficultyScore)
                best = built;
            return;
        }

        nodes++;
        if (nodes >= MaxSearchNodes)
            return;

        var (str, fret) = notes[index];
        foreach (var finger in CandidateFingers(index, notes.Count, assignment, voicing, str, fret))
        {
            if (!PlacementLegal(voicing, assignment, str, fret, finger, avgFret))
                continue;

            assignment[str] = finger;
            Search(voicing, notes, index + 1, assignment, avgFret, ref best, ref nodes, ref solutions);
            assignment[str] = 0;

            if (solutions >= MaxSolutionsToScore || nodes >= MaxSearchNodes)
                return;
        }
    }

    /// <summary>
    /// Prefer lower (index-ward) fingers for bass-side notes and pinky-ward for treble.
    /// Legal same-fret barre extensions are tried early so full barre chords are found
    /// before the search budget is spent on impossible dedicated-finger trees.
    /// </summary>
    private static IEnumerable<int> CandidateFingers(
        int noteIndex,
        int noteCount,
        int[] assignment,
        ChordVoicing voicing,
        int str,
        int fret)
    {
        // Ideal finger from bass→treble progression across fretted notes.
        var ideal = noteCount <= 1
            ? 1
            : 1 + (int)Math.Round(noteIndex * 3.0 / (noteCount - 1));
        ideal = Math.Clamp(ideal, 1, 4);

        var used = new HashSet<int>();
        for (var s = 0; s < assignment.Length; s++)
        {
            if (assignment[s] != 0)
                used.Add(assignment[s]);
        }

        // Same-fret finger already placed → legal barre candidate when span is clear.
        int? barreFinger = null;
        for (var s = 0; s < assignment.Length; s++)
        {
            if (assignment[s] == 0) continue;
            if (voicing.Strings[s].Fret != fret) continue;
            if (MultiStringFingerSpanOk(voicing, assignment, assignment[s], str, fret))
            {
                barreFinger = assignment[s];
                break;
            }
        }

        var notesAtFret = 0;
        for (var s = 0; s < voicing.StringCount; s++)
        {
            if (voicing.Strings[s].Fret == fret)
                notesAtFret++;
        }

        var ranked = new List<(int Finger, int Rank)>();
        for (var f = 1; f <= 4; f++)
        {
            var rank = Math.Abs(f - ideal) * 10;
            if (used.Contains(f) && f != barreFinger)
                continue; // finger already on a different fret

            if (barreFinger == f)
            {
                // Prefer barre when many notes share the fret (classic barre chords);
                // otherwise keep it as a fallback after dedicated fingers.
                rank += notesAtFret >= 3 ? -40 : 15;
            }
            else if (!used.Contains(f))
            {
                rank -= 3;
            }

            ranked.Add((f, rank));
        }

        return ranked.OrderBy(x => x.Rank).Select(x => x.Finger);
    }

    private static bool PlacementLegal(
        ChordVoicing voicing,
        int[] assignment,
        int str,
        int fret,
        int finger,
        double avgFret)
    {
        // One finger, one fret.
        for (var s = 0; s < assignment.Length; s++)
        {
            if (assignment[s] != finger) continue;
            if (voicing.Strings[s].Fret != fret)
                return false;
        }

        // Extending a multi-string finger → barre span must be clear.
        if (!MultiStringFingerSpanOk(voicing, assignment, finger, str, fret))
            return false;

        // Finger order vs fret: lower finger number ⇒ same or lower fret.
        for (var s = 0; s < assignment.Length; s++)
        {
            var other = assignment[s];
            if (other == 0 || other == finger) continue;
            var otherFret = voicing.Strings[s].Fret;
            if (finger < other && fret > otherFret)
                return false;
            if (finger > other && fret < otherFret)
                return false;
        }

        // Position-dependent stretch vs already placed fingers.
        for (var s = 0; s < assignment.Length; s++)
        {
            var other = assignment[s];
            if (other == 0) continue;
            var otherFret = voicing.Strings[s].Fret;
            if (!StretchOk(finger, fret, other, otherFret, avgFret))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Adjacent-finger fret span: ~3 near the nut, scaling to ~6 by fret 12+.
    /// Non-adjacent fingers get +1 fret allowance per skipped finger.
    /// </summary>
    public static double MaxAdjacentStretch(double positionFret)
    {
        var t = Math.Clamp(positionFret / 12.0, 0.0, 1.0);
        return 3.0 + t * 3.0; // 3 at nut … 6 at 12+
    }

    /// <summary>Extra frets allowed per skipped finger; smaller near the nut.</summary>
    public static double SkipStretch(double positionFret)
    {
        var t = Math.Clamp(positionFret / 12.0, 0.0, 1.0);
        return 0.25 + t * 0.75; // ~0.25 near nut … 1.0 at 12+
    }

    public static bool StretchOk(int fingerA, int fretA, int fingerB, int fretB, double positionFret)
    {
        if (fingerA == fingerB)
            return fretA == fretB;

        var fingerDelta = Math.Abs(fingerA - fingerB);
        var fretDelta = Math.Abs(fretA - fretB);
        var maxAdj = MaxAdjacentStretch(positionFret);
        var maxAllowed = maxAdj + (fingerDelta - 1) * SkipStretch(positionFret);
        return fretDelta <= maxAllowed + 1e-9;
    }

    /// <summary>
    /// One finger fretting several strings at the same fret acts as a barre across the
    /// full span. Intermediate strings cannot sit closer to the nut (lower fret), be open,
    /// or be muted — the barre finger would press them at the barre fret.
    /// Notes in front of the barre (higher frets) are fine.
    /// </summary>
    private static bool MultiStringFingerSpanOk(
        ChordVoicing voicing,
        int[] assignment,
        int finger,
        int newString,
        int barreFret)
    {
        var strings = new List<int> { newString };
        for (var s = 0; s < assignment.Length; s++)
        {
            if (assignment[s] == finger)
                strings.Add(s);
        }

        if (strings.Count < 2)
            return true;

        return BarreSpanClearOfLowerNotes(voicing, strings.Min(), strings.Max(), barreFret);
    }

    /// <summary>
    /// Every string between <paramref name="fromString"/> and <paramref name="endString"/>
    /// (inclusive) must be fretted at >= <paramref name="barreFret"/>.
    /// </summary>
    private static bool BarreSpanClearOfLowerNotes(
        ChordVoicing voicing,
        int fromString,
        int endString,
        int barreFret)
    {
        if (fromString > endString)
            (fromString, endString) = (endString, fromString);

        for (var s = fromString; s <= endString; s++)
        {
            var sf = voicing.Strings[s];
            if (sf.IsMuted || sf.IsOpen)
                return false;
            if (sf.Fret < barreFret)
                return false;
        }

        return true;
    }

    /// <summary>
    /// True-twister check: a finger sitting behind higher frets on both bass and treble
    /// sides is only legal as a clean “pocket” (Em7-style). Extra notes at intermediate
    /// frets outside that pocket (e.g. A@8 with D@9 / G@7 / B@9) make the shape unplayable.
    /// </summary>
    private static bool PocketGeometryOk(ChordVoicing voicing, int[] assignment)
    {
        for (var s = 0; s < assignment.Length; s++)
        {
            if (assignment[s] == 0) continue;
            var fret = voicing.Strings[s].Fret;

            var bassHigher = false;
            var trebleHigher = false;
            var peak = fret;
            for (var b = 0; b < s; b++)
            {
                if (!voicing.Strings[b].IsFretted) continue;
                if (voicing.Strings[b].Fret > fret)
                {
                    bassHigher = true;
                    peak = Math.Max(peak, voicing.Strings[b].Fret);
                }
            }

            for (var t = s + 1; t < voicing.StringCount; t++)
            {
                if (!voicing.Strings[t].IsFretted) continue;
                if (voicing.Strings[t].Fret > fret)
                {
                    trebleHigher = true;
                    peak = Math.Max(peak, voicing.Strings[t].Fret);
                }
            }

            if (!bassHigher || !trebleHigher)
                continue;

            // Pocket finger must be the lowest-numbered among the sandwich region.
            var pocketFinger = assignment[s];
            for (var o = 0; o < assignment.Length; o++)
            {
                if (assignment[o] == 0) continue;
                var of = voicing.Strings[o].Fret;
                if (of <= fret) continue;
                // side notes at higher frets
                if ((o < s || o > s) && assignment[o] <= pocketFinger)
                    return false;
            }

            // No fretted note at an intermediate fret outside the pocket string.
            for (var o = 0; o < voicing.StringCount; o++)
            {
                if (o == s) continue;
                if (!voicing.Strings[o].IsFretted) continue;
                var of = voicing.Strings[o].Fret;
                if (of > fret && of < peak)
                    return false;
            }
        }

        return true;
    }

    private static ChordFingering? BuildFingering(
        ChordVoicing voicing,
        int[] assignment,
        double avgFret)
    {
        if (!PocketGeometryOk(voicing, assignment))
            return null;

        var fingers = new FingerId[voicing.StringCount];
        for (var s = 0; s < voicing.StringCount; s++)
        {
            if (voicing.Strings[s].IsMuted || voicing.Strings[s].IsOpen)
            {
                fingers[s] = FingerId.None;
                continue;
            }

            if (assignment[s] == 0)
                return null;

            fingers[s] = new FingerId(assignment[s]);
        }

        // Validate one-finger-one-fret + barre spans.
        var fingerFrets = new Dictionary<int, int>();
        var fingerStrings = new Dictionary<int, List<int>>();
        for (var s = 0; s < fingers.Length; s++)
        {
            if (!fingers[s].IsAssigned) continue;
            var f = fingers[s].Value;
            var fret = voicing.Strings[s].Fret;
            if (fingerFrets.TryGetValue(f, out var existing) && existing != fret)
                return null;
            fingerFrets[f] = fret;
            if (!fingerStrings.TryGetValue(f, out var list))
            {
                list = [];
                fingerStrings[f] = list;
            }
            list.Add(s);
        }

        var barres = new List<BarreSegment>();
        foreach (var (finger, strings) in fingerStrings)
        {
            if (strings.Count < 2) continue;
            var fret = fingerFrets[finger];
            if (!BarreSpanClearOfLowerNotes(voicing, strings.Min(), strings.Max(), fret))
                return null;

            barres.Add(new BarreSegment
            {
                Fret = fret,
                Finger = finger,
                FromString = strings.Min(),
                EndString = strings.Max()
            });
        }

        // Final stretch pass (pairs).
        var placed = Enumerable.Range(0, fingers.Length)
            .Where(s => fingers[s].IsAssigned)
            .Select(s => (Finger: fingers[s].Value, Fret: voicing.Strings[s].Fret, String: s))
            .OrderBy(p => p.Finger)
            .ToList();

        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                if (!StretchOk(placed[i].Finger, placed[i].Fret, placed[j].Finger, placed[j].Fret, avgFret))
                    return null;
            }
        }

        var difficulty = ScoreDifficulty(voicing, placed, barres, avgFret);

        return new ChordFingering
        {
            Fingers = fingers,
            Barres = barres,
            DifficultyScore = difficulty,
            Notes = barres.Count == 0 ? null : $"Barre fret {barres[0].Fret}"
        };
    }

    private static double ScoreDifficulty(
        ChordVoicing voicing,
        List<(int Finger, int Fret, int String)> placed,
        List<BarreSegment> barres,
        double avgFret)
    {
        // Em7 (5+7+7+open) should land medium/easy — not hard.
        var distinctFingers = placed.Select(p => p.Finger).Distinct().Count();
        double difficulty = voicing.Span * 0.2;
        difficulty += distinctFingers * 0.15;
        difficulty += avgFret * 0.02;

        foreach (var barre in barres)
        {
            var width = barre.EndString - barre.FromString + 1;
            difficulty += width * 0.2;
            if (barre.Finger == 1)
                difficulty -= 0.5; // index barre is the standard comfortable form
        }

        for (var i = 0; i < placed.Count - 1; i++)
        {
            var a = placed[i];
            var b = placed[i + 1];
            if (b.Finger == a.Finger) continue;

            var fretDelta = Math.Abs(b.Fret - a.Fret);
            var fingerDelta = Math.Max(1, b.Finger - a.Finger);
            var stringDelta = Math.Abs(b.String - a.String);

            if (fretDelta > fingerDelta)
                difficulty += (fretDelta - fingerDelta) * 0.35;

            // Wide string skip with stretch, especially higher on the neck.
            if (stringDelta >= 2 && fretDelta >= 2)
                difficulty += 0.15 * stringDelta * (1.0 + avgFret / 24.0);
        }

        return Math.Max(0, difficulty);
    }
}
