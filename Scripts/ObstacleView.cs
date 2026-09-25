using Godot;
using NeonDash.Core;

/// <summary>
/// A single obstacle. Its world position comes from
/// <see cref="SpawnDirector"/>; the view only converts world Z to screen Y and
/// exposes the same box the collision test uses, so what you see is exactly
/// what the logic checks.
/// </summary>
public partial class ObstacleView : Node2D
{
    private ColorRect _body = null!;
    private ColorRect _edge = null!;

    private float _worldZ;
    private float _laneX;
    private ObstacleKind _kind;

    private static readonly Color Danger = new(1.0f, 0.18f, 0.42f);
    private static readonly Color Warning = new(1.0f, 0.72f, 0.1f);

    /// <summary>Box used by <see cref="CollisionDetector"/> - identical to what is drawn.</summary>
    public Box WorldBox { get; private set; }

    /// <summary>True once the obstacle is close enough to matter for collisions.</summary>
    public bool IsNearPlayer { get; private set; }

    public override void _Ready()
    {
        _body = GetNode<ColorRect>("Body");
        _edge = GetNode<ColorRect>("Edge");
    }

    public void Configure(SpawnSlot slot, float laneX, float pixelsPerMetre)
    {
        _worldZ = slot.Z;
        _laneX = laneX;
        _kind = slot.Kind;

        var centreY = _kind == ObstacleKind.LowBar ? 0.45f : 1.1f;
        var halfHeight = _kind == ObstacleKind.LowBar ? 0.45f : 1.1f;
        var pixels = pixelsPerMetre * halfHeight * 2f;

        _body.Size = new Vector2(150f, pixels);
        _body.Position = new Vector2(-75f, -pixels * 0.5f);
        _body.Color = _kind == ObstacleKind.LowBar ? Warning : Danger;

        _edge.Size = new Vector2(150f, 6f);
        _edge.Position = new Vector2(-75f, -pixels * 0.5f);
        _edge.Color = new Color(1, 1, 1, 0.85f);

        WorldBox = new Box(laneX, centreY, slot.Z, 1.6f, halfHeight * 2f, 0.8f);
    }

    public void Advance(float playerZ, float pixelsPerMetre, float playerScreenY)
    {
        var relative = _worldZ - playerZ;
        Position = new Vector2(
            _laneX * pixelsPerMetre,
            playerScreenY - (relative * pixelsPerMetre));

        // Only run the (relatively expensive) overlap test in the danger window.
        IsNearPlayer = relative is > -6f and < 6f;
    }
}
