using Godot;

// Render Lab probe: a tiny scene with known colours so a screenshot can be
// judged automatically (red box, blue sphere, green ground = 3D pipeline;
// yellow bar = 2D/canvas pipeline). It also prints what renderer/driver/GPU
// the engine actually ended up on, because Godot silently falls back.
public partial class RenderProbe : Node3D
{
    private MeshInstance3D _box = null!;
    private Label _label = null!;
    private double _acc;
    private int _sec;
    private string? _shotDir;
    private int _quitAt = -1;

    public override void _Ready()
    {
        GetNode<Camera3D>("Camera").LookAt(new Vector3(0, 0.5f, 0), Vector3.Up);
        GetNode<DirectionalLight3D>("Sun").RotationDegrees = new Vector3(-50, 30, 0);
        _box = GetNode<MeshInstance3D>("Box");
        _label = GetNode<Label>("HUD/Label");

        foreach (string a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith("--shots=")) _shotDir = a.Substring("--shots=".Length);
            if (a.StartsWith("--quit-at=")) _quitAt = int.Parse(a.Substring("--quit-at=".Length));
        }

        string method = RenderingServer.GetCurrentRenderingMethod();
        string driver = RenderingServer.GetCurrentRenderingDriverName();
        GD.Print($"PROBE_READY method={method} driver={driver} " +
                 $"adapter={RenderingServer.GetVideoAdapterName()} " +
                 $"vendor={RenderingServer.GetVideoAdapterVendor()} " +
                 $"api={RenderingServer.GetVideoAdapterApiVersion()} os={OS.GetName()}");
        _label.Text = $"PROBE OK\n{method} / {driver}";
    }

    public override void _Process(double delta)
    {
        _box.RotateY((float)delta);
        _acc += delta;
        if (_acc < 1.0) return;
        _acc -= 1.0;
        _sec++;
        GD.Print($"PROBE_TICK sec={_sec} fps={Engine.GetFramesPerSecond()}");
        if (_shotDir != null && (_sec == 2 || _sec == 4)) SaveShot($"{_shotDir}/desktop-{_sec}s.png");
        if (_quitAt > 0 && _sec >= _quitAt) GetTree().Quit();
    }

    private async void SaveShot(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image img = GetViewport().GetTexture().GetImage();
        Error err = img.SavePng(path);
        GD.Print($"PROBE_SHOT path={path} err={err}");
    }
}
