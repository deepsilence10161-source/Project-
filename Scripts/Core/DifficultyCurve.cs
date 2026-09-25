namespace NeonDash.Core;

/// <summary>
/// Maps current speed to the world-space gap between obstacle waves.
/// Faster running = tighter spacing, but never below a floor so the game
/// stays playable. Pure logic, no engine dependencies.
/// </summary>
public sealed class DifficultyCurve
{
    public float MaxGap { get; }
    public float MinGap { get; }

    public DifficultyCurve(float maxGap = 26f, float minGap = 11f)
    {
        if (minGap <= 0f) throw new ArgumentOutOfRangeException(nameof(minGap));
        if (maxGap < minGap) throw new ArgumentOutOfRangeException(nameof(maxGap), "maxGap must be >= minGap");
        MaxGap = maxGap;
        MinGap = minGap;
    }

    /// <summary>
    /// Gap in metres for a given speed, using <paramref name="normalisedSpeed"/>
    /// where 0 = base speed and 1 = max speed (see
    /// <see cref="SpeedController.NormalisedProgress"/>).
    /// </summary>
    public float GapFor(float normalisedSpeed)
    {
        var t = Math.Clamp(normalisedSpeed, 0f, 1f);
        return MaxGap + ((MinGap - MaxGap) * t);
    }
}
