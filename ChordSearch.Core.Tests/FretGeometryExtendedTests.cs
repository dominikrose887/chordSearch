using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class FretGeometryExtendedTests
{
    [Fact]
    public void Fret24_IsThreeQuartersScale()
    {
        // 2^(24/12)=4 → 1 - 1/4 = 0.75
        var d = FretGeometry.DistanceFromNutMm(24);
        Assert.Equal(FretGeometry.StandardScaleLengthMm * 0.75, d, precision: 6);
    }

    [Fact]
    public void Fret18_OnClassicalNeck()
    {
        var d18 = FretGeometry.DistanceFromNutMm(18);
        var d12 = FretGeometry.DistanceFromNutMm(12);
        Assert.True(d18 > d12);
        Assert.True(d18 < FretGeometry.StandardScaleLengthMm);
    }

    [Fact]
    public void CellWidths_MonotonicallyDecrease_Through24()
    {
        for (var f = 1; f < 24; f++)
        {
            var a = FretGeometry.FretCellWidthMm(f);
            var b = FretGeometry.FretCellWidthMm(f + 1);
            Assert.True(a > b, $"Fret {f} width {a} should be > fret {f + 1} width {b}");
        }
    }

    [Fact]
    public void CellCenter_IsBetweenAdjacentWires()
    {
        for (var f = 1; f <= 24; f++)
        {
            var left = FretGeometry.DistanceFromNutMm(f - 1);
            var right = FretGeometry.DistanceFromNutMm(f);
            var mid = FretGeometry.FretCellCenterMm(f);
            Assert.InRange(mid, left, right);
        }
    }

    [Fact]
    public void SumOfCells_EqualsDistanceToMaxFret()
    {
        const int n = 24;
        var sum = Enumerable.Range(1, n).Sum(f => FretGeometry.FretCellWidthMm(f));
        Assert.Equal(FretGeometry.DistanceFromNutMm(n), sum, precision: 6);
    }

    [Fact]
    public void CustomScale_ScalesLinearly()
    {
        var d648 = FretGeometry.DistanceFromNutMm(12, 648);
        var d324 = FretGeometry.DistanceFromNutMm(12, 324);
        Assert.Equal(d648 / 2.0, d324, precision: 6);
    }

    [Fact]
    public void NegativeFret_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => FretGeometry.DistanceFromNutMm(-1));

    [Fact]
    public void CellWidth_Fret0_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => FretGeometry.FretCellWidthMm(0));
}
