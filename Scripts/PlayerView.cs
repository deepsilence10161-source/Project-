using Godot;
using NeonDash.Core;

/// <summary>
/// The player's visual: the anime runner girl (Art/Player/*.png, seen from
/// behind). Reads authoritative values from <see cref="LaneController.RenderX"/>
/// and <see cref="JumpController.Height"/> and never decides anything itself.
///
/// Node origin = the point between her feet on the ground line (moved up
/// by the jump height). The ground shadow is drawn here, so it stays on the
/// ground while she is in the air.
/// </summary>
public partial class PlayerView : Node2D
{
    private const float SpriteScale = 0.75f;     // 339 px texture -> ~254 px on screen
    private const float FeetY = 48f;             // ground line, relative to the node origin
    private const float RunFrameSeconds = 0.13f; // at base speed; faster when running faster

    private Sprite2D _sprite = null!;
    private Texture2D[] _run = null!;
    private Texture2D _jump = null!;

    private float _frameTimer;
    private int _runIndex;
    private bool _inAir;
    private float _height;
    private float _pixelsPerMetre = 100f;
    private float _crashTimer = -1f;

    /// <summary>How many run-frame swaps happened (read by the E2E probe).</summary>
    public int RunSwaps { get; private set; }
    public int FramesLoaded { get; private set; }

    public override void _Ready()
    {
        _run = new[]
        {
            GD.Load<Texture2D>("res://Art/Player/run_0.png"),
            GD.Load<Texture2D>("res://Art/Player/run_1.png"),
        };
        _jump = GD.Load<Texture2D>("res://Art/Player/jump.png");
        FramesLoaded = (_run[0] != null ? 1 : 0) + (_run[1] != null ? 1 : 0) + (_jump != null ? 1 : 0);

        _sprite = new Sprite2D
        {
            Name = "Sprite",
            Texture = _run[0],
            Centered = false,
            Scale = new Vector2(SpriteScale, SpriteScale),
            TextureFilter = TextureFilterEnum.Linear,
        };
        AddChild(_sprite);
        PlaceSprite(_run[0]);
    }

    // Feet on the ground line, horizontally centred on the node origin.
    private void PlaceSprite(Texture2D tex)
    {
        var size = tex.GetSize() * SpriteScale;
        _sprite.Position = new Vector2(-size.X * 0.5f, FeetY - size.Y);
    }

    private void SetFrame(Texture2D tex)
    {
        if (_sprite.Texture == tex) return;
        _sprite.Texture = tex;
        PlaceSprite(tex);
    }

    /// <summary>Advances the run cycle. Call every physics frame while playing.</summary>
    public void Animate(float delta, float speedFactor)
    {
        if (_inAir || _crashTimer >= 0f) return;
        _frameTimer += delta * Mathf.Clamp(speedFactor, 0.6f, 2.2f);
        if (_frameTimer < RunFrameSeconds) return;
        _frameTimer = 0f;
        _runIndex = 1 - _runIndex;
        RunSwaps++;
        SetFrame(_run[_runIndex]);
    }

    /// <param name="laneX">Metres from centre, from LaneController.RenderX.</param>
    /// <param name="height">Metres above ground, from JumpController.Height.</param>
    public void Apply(float laneX, float height, float pixelsPerMetre, float baseScreenY)
    {
        _pixelsPerMetre = pixelsPerMetre;
        _height = height;
        Position = new Vector2(laneX * pixelsPerMetre, baseScreenY - (height * pixelsPerMetre));

        var inAir = height > 0.05f;
        if (inAir != _inAir)
        {
            _inAir = inAir;
            SetFrame(inAir ? _jump : _run[_runIndex]);
            if (inAir) TestProbe.Emit("pose", "name=jump");
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        // Soft oval shadow on the ground; shrinks and fades as she rises.
        var lift = Mathf.Clamp(_height / 2.6f, 0f, 1f);
        var ground = FeetY + (_height * _pixelsPerMetre);
        var w = 62f * (1f - (0.45f * lift));
        DrawSetTransform(new Vector2(0f, ground - 4f), 0f, new Vector2(1f, 0.28f));
        DrawCircle(Vector2.Zero, w, new Color(0f, 0.94f, 1f, 0.22f * (1f - lift)));
        DrawCircle(Vector2.Zero, w * 0.62f, new Color(0f, 0f, 0f, 0.45f * (1f - lift)));
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    public override void _Process(double delta)
    {
        if (_crashTimer < 0f) return;
        // Crash: red flash + stumble tilt, easing out.
        _crashTimer += (float)delta;
        var t = Mathf.Clamp(_crashTimer / 0.35f, 0f, 1f);
        _sprite.Modulate = new Color(1f, 0.35f + (0.3f * t), 0.4f + (0.3f * t));
        Rotation = Mathf.Lerp(0f, 0.22f, t);
    }

    public void PlayCrash()
    {
        _crashTimer = 0f;
        TestProbe.Emit("pose", "name=crash");
    }

    public void Reset()
    {
        _crashTimer = -1f;
        _sprite.Modulate = Colors.White;
        Rotation = 0f;
        _inAir = false;
        _height = 0f;
        _runIndex = 0;
        SetFrame(_run[0]);
        QueueRedraw();
    }
}
