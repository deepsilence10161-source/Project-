using Godot;

/// <summary>
/// Score / state overlay. Purely presentational - all numbers come from
/// <c>ScoreManager</c> and <c>SpeedController</c> in <c>Main</c>.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _score = null!;
    private Label _high = null!;
    private Label _centre = null!;
    private Label _sub = null!;
    private ProgressBar _speedBar = null!;

    private static readonly Color TitleColour = new(0f, 0.94f, 1f);
    private static readonly Color GameOverColour = new(1f, 0.25f, 0.35f);

    /// <summary>Best score currently shown in the HUD (read by the E2E probe).</summary>
    public int ShownHighScore { get; private set; }

    /// <summary>Colour the centre label is actually drawn in: font colour x modulate.</summary>
    public Color CentreDrawColour => _centre.GetThemeColor("font_color") * _centre.Modulate;

    private TextureRect _portrait = null!;
    public bool PortraitLoaded { get; private set; }

    public override void _Ready()
    {
        _score = GetNode<Label>("Top/Score");
        _high = GetNode<Label>("Top/High");
        _centre = GetNode<Label>("Centre/Box/CentreLabel");
        _sub = GetNode<Label>("Centre/Box/SubLabel");
        _speedBar = GetNode<ProgressBar>("Top/SpeedBar");

        // The runner girl, front view, above the title / game-over text.
        var tex = GD.Load<Texture2D>("res://Art/portrait.png");
        PortraitLoaded = tex != null;
        _portrait = new TextureRect
        {
            Name = "Portrait",
            Texture = tex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(340f, 506f),
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        var box = GetNode<VBoxContainer>("Centre/Box");
        box.AddChild(_portrait);
        box.MoveChild(_portrait, 0);
    }

    public void SetScore(int value, float normalisedSpeed)
    {
        _score.Text = value.ToString("D6");
        _speedBar.Value = normalisedSpeed * 100f;
    }

    public void SetHighScore(int value)
    {
        ShownHighScore = value;
        _high.Text = $"BEST {value:D6}";
    }

    // Bug fix: these used Modulate, which MULTIPLIES the label's cyan font
    // colour. Red x cyan = (0, 0.24, 0.35): a dark blue, barely readable
    // "GAME OVER". Setting the font colour itself draws the intended colour.
    private void SetCentreColour(Color c)
    {
        _centre.Modulate = Colors.White;
        _centre.AddThemeColorOverride("font_color", c);
    }

    public void ShowTitle()
    {
        _centre.Text = "NEON DASH";
        _sub.Text = "TAP TO START";
        SetCentreColour(TitleColour);
        _sub.Visible = true;
        _portrait.Visible = true;
    }

    public void ShowPlaying()
    {
        _centre.Text = "";
        _sub.Text = "";
        _portrait.Visible = false;
    }

    public void ShowGameOver(int final, int best, bool isNewBest)
    {
        _centre.Text = "GAME OVER";
        SetCentreColour(GameOverColour);
        _portrait.Visible = true;
        // Bug fix: this used to test `best >= final`, which is always true
        // (best already includes this run), so "NEW BEST" could never show.
        _sub.Text = isNewBest
            ? $"NEW BEST {final}!   ·   TAP TO RETRY"
            : $"SCORE {final}  ·  BEST {best}   ·   TAP TO RETRY";
    }
}
