using ChordSearch.Core.Chords;
using ChordSearch.Core.Fingering;
using ChordSearch.Core.Music;
using ChordSearch.Core.Services;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class FingeringEngineTests
{
    private readonly FingeringEngine _engine = new();

    private static ChordVoicing V(params int[] frets)
        => new() { Strings = frets.Select(f => new StringFret(f)).ToArray() };

    private static ChordSpec Em7Spec()
        => new ChordSpec { Root = PitchClass.E, Quality = ChordQuality.Minor }
            .WithExtensions([ChordExtension.Seventh]);

    [Fact]
    public void Assign_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => _engine.Assign(null!));

    [Fact]
    public void Assign_EmptyStrings_ReturnsNull()
        => Assert.Null(_engine.Assign(new ChordVoicing { Strings = [] }));

    [Fact]
    public void OpenOnly_NoFingers()
    {
        var open = V(0, 0, 0, 0, 0, 0);
        var f = _engine.Assign(open);
        Assert.NotNull(f);
        Assert.All(f!.Fingers, x => Assert.False(x.IsAssigned));
        Assert.Equal("Open strings only", f.Notes);
    }

    [Fact]
    public void OpenC_Shape_GetsFingers()
    {
        // x 3 2 0 1 0
        var c = V(-1, 3, 2, 0, 1, 0);
        var f = _engine.Assign(c);
        Assert.NotNull(f);
        Assert.True(f!.Fingers[1].IsAssigned);
        Assert.True(f.Fingers[2].IsAssigned);
        Assert.True(f.Fingers[4].IsAssigned);
        Assert.False(f.Fingers[0].IsAssigned);
        Assert.False(f.Fingers[3].IsAssigned);
        AssertMultiStringSpansAreLegal(c, f);
    }

    [Fact]
    public void OpenEm_Shape_GetsFingers()
    {
        var em = V(0, 2, 2, 0, 0, 0);
        var f = _engine.Assign(em);
        Assert.NotNull(f);
        Assert.True(f!.Fingers[1].IsAssigned);
        Assert.True(f.Fingers[2].IsAssigned);
        AssertMultiStringSpansAreLegal(em, f);
    }

    [Fact]
    public void FullBarre_SameFret_UsesFinger1()
    {
        var barre = V(5, 5, 5, 5, 5, 5);
        var f = _engine.Assign(barre);
        Assert.NotNull(f);
        Assert.NotEmpty(f!.Barres);
        Assert.Equal(5, f.Barres[0].Fret);
        Assert.All(f.Fingers, x => Assert.Equal(1, x.Value));
    }

    [Fact]
    public void BarreF_Shape_GetsAssignment()
    {
        // Classic F major barre
        var fShape = V(1, 3, 3, 2, 1, 1);
        var f = _engine.Assign(fShape);
        Assert.NotNull(f);
        Assert.Equal(6, f!.Fingers.Count);
        Assert.True(f.Fingers.Count(x => x.IsAssigned) >= 4);
        AssertMultiStringSpansAreLegal(fShape, f);
    }

    [Fact]
    public void ImpossibleSpan_ReturnsNull()
    {
        var bad = V(-1, 1, 9, -1, -1, -1);
        Assert.Null(_engine.Assign(bad));
    }

    [Fact]
    public void AdjacentFingers_HugeStretch_Rejected()
    {
        var bad = V(1, 8, -1, -1, -1, -1);
        Assert.Null(_engine.Assign(bad));
    }

    [Fact]
    public void SingleFrettedNote_GetsOneFinger()
    {
        var v = V(-1, -1, -1, -1, -1, 3);
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        Assert.Equal(1, f!.Fingers.Count(x => x.IsAssigned));
        Assert.True(f.Fingers[5].IsAssigned);
    }

    [Fact]
    public void MutedAndOpen_Mix_Ok()
    {
        var v = V(-1, 0, 2, 2, 2, 0);
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        Assert.False(f!.Fingers[0].IsAssigned);
        Assert.False(f.Fingers[1].IsAssigned);
        AssertMultiStringSpansAreLegal(v, f);
    }

    [Fact]
    public void SameFinger_DifferentFrets_NotAllowedInFinal()
    {
        var c = V(-1, 3, 2, 0, 1, 0);
        var f = _engine.Assign(c);
        Assert.NotNull(f);

        var fingerFrets = new Dictionary<int, int>();
        for (var s = 0; s < c.StringCount; s++)
        {
            if (!f!.Fingers[s].IsAssigned) continue;
            var finger = f.Fingers[s].Value;
            var fret = c.Strings[s].Fret;
            if (fingerFrets.TryGetValue(finger, out var existing))
                Assert.Equal(existing, fret);
            else
                fingerFrets[finger] = fret;
        }
    }

    [Fact]
    public void Em7_PocketAt5And7_IsAssignable()
    {
        // User-approved: A@7, D@5, G@7, B open — index@5, ring/pinky@7.
        var em7 = V(-1, 7, 5, 7, 0, -1);
        var f = _engine.Assign(em7);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(em7, f!);
        // Medium/easy — not a hard reject and not scored as extreme.
        Assert.True(f.DifficultyScore < 6.0, $"Expected easy/medium difficulty, got {f.DifficultyScore}");
    }

    [Fact]
    public void Em7_WideReachAt7And12_IsAssignable()
    {
        var em7 = V(-1, 7, 12, 12, 0, -1);
        var f = _engine.Assign(em7);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(em7, f!);
    }

    [Fact]
    public void Em7_Shapes_AppearInSearchResults()
    {
        var response = new ChordSearchService(new VoicingOptions { MaxResults = 40, MaxFret = 24 })
            .Search(Em7Spec(), TuningPresets.StandardE);

        Assert.NotEmpty(response.Results);

        static bool Matches(int[] want, ChordVoicing v)
        {
            if (v.StringCount != want.Length) return false;
            for (var i = 0; i < want.Length; i++)
            {
                if (v.Strings[i].Fret != want[i]) return false;
            }
            return true;
        }

        var keys = response.Results.Select(r => r.Voicing.ShapeKey).ToHashSet();
        var hasPocket = response.Results.Any(r => Matches([-1, 7, 5, 7, 0, -1], r.Voicing));
        var hasWide = response.Results.Any(r => Matches([-1, 7, 12, 12, 0, -1], r.Voicing));

        Assert.True(
            hasPocket || hasWide,
            "Expected Em7 pocket (-1,7,5,7,0,-1) or wide (-1,7,12,12,0,-1) in top 40. Got: "
            + string.Join(", ", keys.Take(15)));
    }

    [Fact]
    public void IllegalBarre_OverLowerIntermediate_Rejected()
    {
        // True twister: cannot barre the 9s over G@7, and A@8 blocks a clean pocket.
        var impossible = V(-1, 8, 9, 7, 9, -1);
        Assert.Null(_engine.Assign(impossible));
    }

    [Fact]
    public void BarreSpan_MuteInsideSpan_SameFingerNotUsed()
    {
        var v = V(-1, 5, -1, 5, -1, -1);
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(v, f!);
        Assert.NotEqual(f.Fingers[1].Value, f.Fingers[3].Value);
    }

    [Fact]
    public void CleanPocket_WithoutExtraIntermediate_Assignable()
    {
        // Same U-shape family as Em7 (no extra intermediate fret on an outer string).
        var pocket = V(-1, 9, 7, 9, -1, -1);
        var f = _engine.Assign(pocket);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(pocket, f!);
    }

    [Fact]
    public void BarreSpan_HigherFretsInFront_Allowed()
    {
        var fShape = V(1, 3, 3, 2, 1, 1);
        var f = _engine.Assign(fShape);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(fShape, f!);
    }

    [Fact]
    public void ContiguousMiniBarre_NoIntermediate_Allowed()
    {
        var v = V(-1, -1, 5, 5, 7, -1);
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(v, f!);
    }

    [Fact]
    public void PositionStretch_FailsNearNut_PassesHigher()
    {
        // Adjacent-finger span of 4 frets: too wide near the nut, OK around fret 12.
        Assert.False(FingeringEngine.StretchOk(1, 1, 2, 5, positionFret: 2.0));
        Assert.True(FingeringEngine.StretchOk(1, 10, 2, 14, positionFret: 12.0));

        // Even with pinky-ward skip, a 5-fret reach fails near the nut but passes high up.
        Assert.False(FingeringEngine.StretchOk(1, 1, 4, 6, positionFret: 2.0));
        Assert.True(FingeringEngine.StretchOk(1, 10, 4, 15, positionFret: 12.0));

        var low = V(-1, 1, 6, -1, -1, -1);
        var high = V(-1, 10, 15, -1, -1, -1);
        Assert.Null(_engine.Assign(low));
        Assert.NotNull(_engine.Assign(high));
    }

    [Fact]
    public void MaxAdjacentStretch_ScalesWithPosition()
    {
        Assert.Equal(3.0, FingeringEngine.MaxAdjacentStretch(0), 1);
        Assert.InRange(FingeringEngine.MaxAdjacentStretch(6), 4.0, 5.0);
        Assert.Equal(6.0, FingeringEngine.MaxAdjacentStretch(12), 1);
        Assert.Equal(6.0, FingeringEngine.MaxAdjacentStretch(18), 1);
    }

    [Fact]
    public void AllAssignments_HaveLegalMultiStringSpans()
    {
        int[][] shapes =
        [
            [-1, 3, 2, 0, 1, 0],       // C
            [-1, 0, 2, 2, 2, 0],       // A
            [0, 2, 2, 0, 0, 0],        // Em
            [3, 3, 5, 5, 5, 3],        // G barre form
            [1, 3, 3, 2, 1, 1],        // F
            [-1, 7, 5, 7, 0, -1],      // Em7 pocket
            [-1, 7, 12, 12, 0, -1],    // Em7 wide
        ];

        foreach (var frets in shapes)
        {
            var v = V(frets);
            var f = _engine.Assign(v);
            Assert.NotNull(f);
            AssertMultiStringSpansAreLegal(v, f!);
        }
    }

    private static void AssertMultiStringSpansAreLegal(ChordVoicing voicing, ChordFingering fingering)
    {
        var byFinger = new Dictionary<int, List<int>>();
        for (var s = 0; s < fingering.Fingers.Count; s++)
        {
            if (!fingering.Fingers[s].IsAssigned) continue;
            var finger = fingering.Fingers[s].Value;
            if (!byFinger.TryGetValue(finger, out var list))
            {
                list = [];
                byFinger[finger] = list;
            }
            list.Add(s);
        }

        foreach (var (_, strings) in byFinger)
        {
            if (strings.Count < 2) continue;
            var fret = voicing.Strings[strings[0]].Fret;
            Assert.All(strings, s => Assert.Equal(fret, voicing.Strings[s].Fret));

            var from = strings.Min();
            var to = strings.Max();
            for (var s = from; s <= to; s++)
            {
                var sf = voicing.Strings[s];
                Assert.False(sf.IsMuted, $"Muted string {s} under multi-finger span {from}-{to}");
                Assert.False(sf.IsOpen, $"Open string {s} under multi-finger span {from}-{to}");
                Assert.True(sf.Fret >= fret, $"String {s} fret {sf.Fret} is behind barre fret {fret}");
            }
        }
    }
}
