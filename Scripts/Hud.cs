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

    public override void _Ready()
    {
        _score = GetNode<Label>("Top/Score");
        _high = GetNode<Label>("Top/High");
        _centre = GetNode<Label>("Centre/Box/CentreLabel");
        _sub = GetNode<Label>("Centre/Box/SubLabel");
        _speedBar = GetNode<ProgressBar>("Top/SpeedBar");
    }

    public void SetScore(int value, float normalisedSpeed)
    {
        _score.Text = value.ToString("D6");
        _speedBar.Value = normalisedSpeed * 100f;
    }

    public void SetHighScore(int value) => _high.Text = $"BEST {value:D6}";

    public void ShowTitle()
    {
        _centre.Text = "NEON DASH";
        _sub.Text = "TAP TO START";
        _centre.Modulate = new Color(0, 0.94f, 1f);
        _sub.Visible = true;
    }

    public void ShowPlaying()
    {
        _centre.Text = "";
        _sub.Text = "";
    }

    public void ShowGameOver(int final, int best)
    {
        _centre.Text = "GAME OVER";
        _centre.Modulate = new Color(1f, 0.25f, 0.35f);
        _sub.Text = best >= final
            ? $"SCORE {final}   ·   TAP TO RETRY"
            : $"NEW BEST {final}!   ·   TAP TO RETRY";
    }
}
