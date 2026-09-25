using Godot;

/// <summary>
/// Parallax-style scrolling grid that sells the sense of forward speed.
/// Draw-only: owns no game state, so it can never affect gameplay.
/// </summary>
public partial class ScrollingGrid : Node2D
{
    private const float LineSpacing = 170f;
    private const int LineCount = 12;
    private const float ViewWidth = 720f;
    private const float ViewHeight = 1280f;

    private float _offset;

    private static readonly Color LineColor = new(0.25f, 0.85f, 1.0f);
    private static readonly Color LaneColor = new(1.0f, 0.2f, 0.85f);

    /// <summary>Scrolls the grid by <paramref name="pixels"/>.</summary>
    public void AdvanceGrid(float pixels)
    {
        _offset = Mathf.PosMod(_offset + pixels, LineSpacing);
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var i = 0; i < LineCount; i++)
        {
            var y = Mathf.PosMod((i * LineSpacing) + _offset, LineSpacing * LineCount) - LineSpacing;
            var fade = 1f - (Mathf.Abs(y - (ViewHeight * 0.5f)) / (ViewHeight * 0.9f));
            var alpha = 0.05f + (0.16f * Mathf.Clamp(fade, 0f, 1f));
            DrawLine(new Vector2(-200f, y), new Vector2(ViewWidth + 200f, y),
                new Color(LineColor, alpha), 2f);
        }

        // Lane dividers at -150, 0, +150 px (i.e. 3 m spacing at 100 px/m).
        foreach (var x in new[] { -150f, 0f, 150f })
        {
            DrawLine(new Vector2(x, -100f), new Vector2(x, ViewHeight + 100f),
                new Color(LaneColor, 0.10f), 1.5f);
        }
    }
}
