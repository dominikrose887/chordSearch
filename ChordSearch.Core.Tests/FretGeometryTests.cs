using ChordSearch.Core.Voicing;

namespace ChordSearch.Core.Tests;

public class FretGeometryTests
{
    [Fact]
    public void TwelfthFret_IsHalfScale()
    {
        var d12 = FretGeometry.DistanceFromNutMm(12);
        Assert.Equal(FretGeometry.StandardScaleLengthMm / 2.0, d12, precision: 6);
    }

    [Fact]
    public void FretSpacing_DecreasesTowardBridge()
    {
        var w1 = FretGeometry.FretCellWidthMm(1);
        var w2 = FretGeometry.FretCellWidthMm(2);
        var w12 = FretGeometry.FretCellWidthMm(12);

        Assert.True(w1 > w2);
        Assert.True(w2 > w12);
        // 1st fret ≈ 36.4 mm on 648 mm scale
        Assert.InRange(w1, 35.0, 37.5);
    }

    [Fact]
    public void Nut_IsZero()
    {
        Assert.Equal(0, FretGeometry.DistanceFromNutMm(0));
    }
}
