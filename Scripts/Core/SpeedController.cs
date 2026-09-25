namespace NeonDash.Core;

/// <summary>
/// Forward run speed that ramps up with distance and is clamped to a maximum.
/// Pure logic, no engine dependencies.
/// </summary>
public sealed class SpeedController
{
    public float BaseSpeed { get; }
    public float MaxSpeed { get; }
    public float Acceleration { get; }

    /// <summary>Speed gained per metre travelled.</summary>
    public float SpeedPerMetre { get; }

    public float CurrentSpeed { get; private set; }

    public SpeedController(
        float baseSpeed = 8f,
        float maxSpeed = 26f,
        float acceleration = 0.06f,
        float speedPerMetre = 0.012f)
    {
        if (baseSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(baseSpeed));
        if (maxSpeed < baseSpeed) throw new ArgumentOutOfRangeException(nameof(maxSpeed), "maxSpeed must be >= baseSpeed");
        if (acceleration < 0f) throw new ArgumentOutOfRangeException(nameof(acceleration));
        if (speedPerMetre < 0f) throw new ArgumentOutOfRangeException(nameof(speedPerMetre));

        BaseSpeed = baseSpeed;
        MaxSpeed = maxSpeed;
        Acceleration = acceleration;
        SpeedPerMetre = speedPerMetre;
        CurrentSpeed = baseSpeed;
    }

    /// <summary>Advances speed by <paramref name="deltaTime"/> seconds of running.</summary>
    public float Update(float deltaTime)
    {
        if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        CurrentSpeed = MathF.Min(MaxSpeed, CurrentSpeed + (Acceleration * deltaTime));
        return CurrentSpeed;
    }

    /// <summary>
    /// Applies the distance-based component of the ramp.
    /// Both ramps (time and distance) are combined and clamped, so the result
    /// is always within [BaseSpeed, MaxSpeed].
    /// </summary>
    public float ApplyDistance(float metres)
    {
        if (metres < 0f) throw new ArgumentOutOfRangeException(nameof(metres));
        CurrentSpeed = MathF.Min(MaxSpeed, BaseSpeed + (metres * SpeedPerMetre));
        return CurrentSpeed;
    }

    public void Reset() => CurrentSpeed = BaseSpeed;

    /// <summary>0 = at base speed, 1 = at max speed.</summary>
    public float NormalisedProgress =>
        MaxSpeed <= BaseSpeed ? 1f : (CurrentSpeed - BaseSpeed) / (MaxSpeed - BaseSpeed);
}
