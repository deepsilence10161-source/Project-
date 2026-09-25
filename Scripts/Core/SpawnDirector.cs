namespace NeonDash.Core;

/// <summary>What a spawn slot contains.</summary>
public enum ObstacleKind
{
    /// <summary>Must be dodged sideways.</summary>
    Block,
    /// <summary>Low enough to jump over.</summary>
    LowBar
}

public readonly record struct SpawnSlot(int Lane, ObstacleKind Kind, float Z);

/// <summary>
/// Deterministic obstacle pattern generator. Takes an explicit seed so tests
/// can assert exact patterns, and guarantees at least one lane stays passable
/// so the game is never unwinnable.
/// Pure logic, no engine dependencies.
/// </summary>
public sealed class SpawnDirector
{
    private readonly Random _random;

    public int LaneCount { get; }

    public SpawnDirector(int seed, int laneCount = LaneController.DefaultLaneCount)
    {
        if (laneCount < 2)
            throw new ArgumentOutOfRangeException(nameof(laneCount), "Need at least 2 lanes to guarantee a passable gap.");

        _random = new Random(seed);
        LaneCount = laneCount;
    }

    /// <summary>
    /// Builds one wave of obstacles at world position <paramref name="z"/>.
    /// With 3 lanes it blocks at most <c>LaneCount - 1</c> lanes, leaving a gap.
    /// </summary>
    public IReadOnlyList<SpawnSlot> NextWave(float z)
    {
        var blockedCount = _random.Next(1, LaneCount);      // 1 .. LaneCount-1
        var lanes = Enumerable.Range(0, LaneCount).OrderBy(_ => _random.Next()).Take(blockedCount).OrderBy(l => l);

        var slots = new List<SpawnSlot>(blockedCount);
        foreach (var lane in lanes)
        {
            var kind = _random.Next(2) == 0 ? ObstacleKind.Block : ObstacleKind.LowBar;
            slots.Add(new SpawnSlot(lane, kind, z));
        }

        return slots;
    }
}
