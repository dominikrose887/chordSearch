namespace ChordSearch.Core.Voicing;

/// <summary>
/// Equal-temperament fret positions for a given scale length (menzúra).
/// Fret n distance from nut: L * (1 - 2^(-n/12)). Spacing shrinks toward the bridge.
/// </summary>
public static class FretGeometry
{
    /// <summary>Fender-style scale length in millimetres.</summary>
    public const double StandardScaleLengthMm = 648.0;

    /// <summary>Distance from the nut to fret wire <paramref name="fret"/> (0 = nut).</summary>
    public static double DistanceFromNutMm(int fret, double scaleLengthMm = StandardScaleLengthMm)
    {
        if (fret < 0) throw new ArgumentOutOfRangeException(nameof(fret));
        if (fret == 0) return 0;
        return scaleLengthMm * (1.0 - Math.Pow(2.0, -fret / 12.0));
    }

    /// <summary>Width of the playable cell behind fret <paramref name="fret"/> (between fret-1 and fret).</summary>
    public static double FretCellWidthMm(int fret, double scaleLengthMm = StandardScaleLengthMm)
    {
        if (fret < 1) throw new ArgumentOutOfRangeException(nameof(fret));
        return DistanceFromNutMm(fret, scaleLengthMm) - DistanceFromNutMm(fret - 1, scaleLengthMm);
    }

    /// <summary>Midpoint of the cell for fret <paramref name="fret"/> (typical finger placement).</summary>
    public static double FretCellCenterMm(int fret, double scaleLengthMm = StandardScaleLengthMm)
    {
        if (fret < 1) throw new ArgumentOutOfRangeException(nameof(fret));
        return (DistanceFromNutMm(fret - 1, scaleLengthMm) + DistanceFromNutMm(fret, scaleLengthMm)) * 0.5;
    }
}
