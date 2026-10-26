using ChordSearch.Core.Fingering;
using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class FingeringEngineTests
{
    private readonly FingeringEngine _engine = new();

    private static ChordVoicing V(params int[] frets)
        => new() { Strings = frets.Select(f => new StringFret(f)).ToArray() };

    [Fact]
    public void Assign_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => _engine.Assign(null!));

    [Fact]
    public void Assign_EmptyStrings_ReturnsNull()
        => Assert.Null(_engine.Assign(new ChordVoicing { Strings = [] }));

    [Fact]
    public void OpenOnly_NoFingers()
    {
        // Em-ish open: 0 2 2 0 0 0 but all open case
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
    }

    [Fact]
    public void FullBarre_SameFret_UsesFinger1()
    {
        // All six strings same fret → classic barre
        var barre = V(5, 5, 5, 5, 5, 5);
        var f = _engine.Assign(barre);
        Assert.NotNull(f);
        Assert.NotEmpty(f!.Barres);
        Assert.Equal(5, f.Barres[0].Fret);
        Assert.All(f.Fingers, x => Assert.Equal(1, x.Value));
    }

    [Fact]
    public void PartialBarre_FShape_GetsAssignmentOrNull()
    {
        // Real F major shape can be hard for heuristics — must not throw
        var fShape = V(1, 3, 3, 2, 1, 1);
        var f = _engine.Assign(fShape);
        if (f is not null)
        {
            Assert.Equal(6, f.Fingers.Count);
            Assert.True(f.Fingers.Count(x => x.IsAssigned) >= 3);
        }
    }

    [Fact]
    public void ImpossibleSpan_ReturnsNull()
    {
        // frets spanning 8 — engine rejects span > 6
        var bad = V(-1, 1, 9, -1, -1, -1);
        Assert.Null(_engine.Assign(bad));
    }

    [Fact]
    public void AdjacentFingers_HugeStretch_RejectedInResults()
    {
        // 1 and 8 on different strings — span 7 already null from span check
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
        var v = V(-1, 0, 2, 2, 2, 0); // A power-ish
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        Assert.False(f!.Fingers[0].IsAssigned);
        Assert.False(f.Fingers[1].IsAssigned);
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
    public void BarreSpan_LowerFretOnIntermediateString_Rejected()
    {
        // Reported impossible C#dim6-like shape (Standard C):
        // frets 9 on both sides of a fret-7 note — barre or sandwich, unplayable.
        var impossible = V(-1, 8, 9, 7, 9, -1);
        Assert.Null(_engine.Assign(impossible));
    }

    [Fact]
    public void BarreSpan_MuteInsideSpan_SameFingerNotUsed()
    {
        // Two notes at fret 5 with a mute between: OK with two fingers, never one barre finger
        var v = V(-1, 5, -1, 5, -1, -1);
        var f = _engine.Assign(v);
        if (f is not null)
        {
            AssertMultiStringSpansAreLegal(v, f);
            Assert.NotEqual(f.Fingers[1].Value, f.Fingers[3].Value);
        }
    }

    [Fact]
    public void SandwichedBehindHigherFrets_Rejected()
    {
        // Index-region note trapped: higher frets on bass AND treble side
        var trapped = V(-1, 9, 7, 9, -1, -1);
        Assert.Null(_engine.Assign(trapped));
    }

    [Fact]
    public void BarreSpan_HigherFretsInFront_Allowed()
    {
        // Classic-ish F major: barre at 1 with notes in front of the barre
        var fShape = V(1, 3, 3, 2, 1, 1);
        var f = _engine.Assign(fShape);
        // May still be null for other heuristic reasons, but must not accept an illegal span
        if (f is not null)
        {
            AssertMultiStringSpansAreLegal(fShape, f);
        }
    }

    [Fact]
    public void ContiguousMiniBarre_NoIntermediate_Allowed()
    {
        // Two adjacent strings at the same fret + one note in front — legal mini-barre
        var v = V(-1, -1, 5, 5, 7, -1);
        var f = _engine.Assign(v);
        Assert.NotNull(f);
        AssertMultiStringSpansAreLegal(v, f!);
    }

    [Fact]
    public void AllAssignments_HaveLegalMultiStringSpans()
    {
        // Smoke: common shapes never invent Twister barres
        int[][] shapes =
        [
            [-1, 3, 2, 0, 1, 0],       // C
            [-1, 0, 2, 2, 2, 0],       // A
            [0, 2, 2, 1, 0, 0],        // Em
            [3, 3, 5, 5, 5, 3],        // open-ish G barre form
            [1, 3, 3, 2, 1, 1],        // F
        ];

        foreach (var frets in shapes)
        {
            var v = V(frets);
            var f = _engine.Assign(v);
            if (f is not null)
                AssertMultiStringSpansAreLegal(v, f);
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
