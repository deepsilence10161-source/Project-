namespace NeonDash.Core;

/// <summary>Horizontal movement direction.</summary>
public enum LaneDirection { Left = -1, Right = 1 }

/// <summary>
/// Three-lane movement with smooth interpolation between lanes.
/// Keeps the logical lane index separate from the rendered X position so the
/// visual side can lerp while the collision side stays exact.
/// Pure logic, no engine dependencies.
/// </summary>
public sealed class LaneController
{
    public const int DefaultLaneCount = 3;

    public int LaneCount { get; }
    public int LaneSpacing { get; }

    /// <summary>Lane the player currently occupies (authoritative for collisions).</summary>
    public int CurrentLane { get; private set; }

    /// <summary>Lane the player is sliding toward.</summary>
    public int TargetLane { get; private set; }

    /// <summary>Rendered X position, interpolated toward the target lane.</summary>
    public float RenderX { get; private set; }

    /// <summary>0 = settled in a lane, 1 = just started moving.</summary>
    public float MoveProgress { get; private set; }

    public bool IsMoving => MoveProgress < 1f;

    public LaneController(int laneCount = DefaultLaneCount, int laneSpacing = 3, int startLane = 1)
    {
        if (laneCount < 1) throw new ArgumentOutOfRangeException(nameof(laneCount));
        if (laneSpacing <= 0) throw new ArgumentOutOfRangeException(nameof(laneSpacing));
        if (startLane < 0 || startLane >= laneCount)
            throw new ArgumentOutOfRangeException(nameof(startLane), "startLane must be a valid lane index");

        LaneCount = laneCount;
        LaneSpacing = laneSpacing;
        CurrentLane = startLane;
        TargetLane = startLane;
        RenderX = LaneToX(startLane);
        MoveProgress = 1f;
    }

    /// <summary>X coordinate of a lane index (centred on 0).</summary>
    public float LaneToX(int lane) => (lane - (LaneCount - 1) * 0.5f) * LaneSpacing;

    /// <summary>
    /// Requests a lane change. Returns false when the move is illegal
    /// (already at the edge, or a move is still in progress).
    /// </summary>
    public bool TryMove(LaneDirection direction)
    {
        if (!IsSettled) return false;

        var next = TargetLane + (int)direction;
        if (next < 0 || next >= LaneCount) return false;

        TargetLane = next;
        MoveProgress = 0f;
        return true;
    }

    public bool IsSettled => MoveProgress >= 1f;

    /// <summary>
    /// Advances the lane-change animation.
    /// <paramref name="slideDuration"/> is the seconds a full lane change takes.
    /// Returns the updated <see cref="RenderX"/>.
    /// </summary>
    public float Update(float deltaTime, float slideDuration = 0.14f)
    {
        if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (slideDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(slideDuration));

        if (IsSettled)
        {
            RenderX = LaneToX(TargetLane);
            return RenderX;
        }

        MoveProgress = MathF.Min(1f, MoveProgress + (deltaTime / slideDuration));
        var from = LaneToX(CurrentLane);
        var to = LaneToX(TargetLane);
        RenderX = from + ((to - from) * MoveProgress);

        if (IsSettled)
            CurrentLane = TargetLane;   // commit only once fully arrived

        return RenderX;
    }

    public void Reset(int startLane = 1)
    {
        if (startLane < 0 || startLane >= LaneCount)
            throw new ArgumentOutOfRangeException(nameof(startLane));

        CurrentLane = startLane;
        TargetLane = startLane;
        RenderX = LaneToX(startLane);
        MoveProgress = 1f;
    }
}
