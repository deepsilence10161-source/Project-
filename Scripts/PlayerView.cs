using Godot;
using NeonDash.Core;

/// <summary>
/// The player's visual representation. Reads authoritative values from
/// <see cref="LaneController.RenderX"/> and <see cref="JumpController.Height"/>
/// and never decides anything itself.
/// </summary>
public partial class PlayerView : Node2D
{
    private ColorRect _body = null!;
    private ColorRect _glow = null!;
    private ColorRect _shadow = null!;
    private float _squash;

    private static readonly Color Cyan = new(0.0f, 0.94f, 1.0f);
    private static readonly Color Magenta = new(1.0f, 0.17f, 0.84f);

    public override void _Ready()
    {
        _body = GetNode<ColorRect>("Body");
        _glow = GetNode<ColorRect>("Glow");
        _shadow = GetNode<ColorRect>("Shadow");
    }

    /// <param name="laneX">Metres from centre, from LaneController.RenderX.</param>
    /// <param name="height">Metres above ground, from JumpController.Height.</param>
    public void Apply(float laneX, float height, float pixelsPerMetre, float baseScreenY)
    {
        var x = laneX * pixelsPerMetre;
        var y = baseScreenY - (height * pixelsPerMetre);
        Position = new Vector2(x, y);

        // Shadow stays on the ground and shrinks as the player rises.
        var lift = Mathf.Clamp(height / 2.6f, 0f, 1f);
        _shadow.Position = new Vector2(-52f, 52f + (height * pixelsPerMetre));
        _shadow.Modulate = new Color(1f, 1f, 1f, 0.35f * (1f - lift));
        var s = 1f - (0.45f * lift);
        _shadow.Scale = new Vector2(s, s * 0.35f);

        // Stretch while rising, squash on landing.
        _squash = Mathf.MoveToward(_squash, 0f, 0.15f);
        var stretch = 1f + (Mathf.Clamp(height / 2.6f, 0f, 1f) * 0.22f) - _squash;
        _body.Scale = new Vector2(1f / stretch, stretch);
        _glow.Scale = _body.Scale;

        // Hue shifts with altitude for extra feedback.
        _glow.Modulate = Cyan.Lerp(Magenta, lift);
    }

    public void PlayCrash()
    {
        _squash = 0.55f;
        _body.Color = new Color(1f, 0.25f, 0.25f);
        _glow.Modulate = new Color(1f, 0.3f, 0.3f, 0.6f);
    }

    public void Reset()
    {
        _body.Color = new Color(1, 1, 1);
        _squash = 0f;
    }
}
