using Godot;
using NeonDash.Core;

/// <summary>
/// Root game controller. Owns the pure logic systems (all unit-tested in
/// <c>NeonDash.Core.Tests</c>) and maps their state onto 2D visuals.
///
/// Coordinate mapping used here:
///   world Z (metres)  -> screen Y (pixels, scrolling down toward the player)
///   lane X  (metres)  -> screen X (pixels, centred)
/// </summary>
public partial class Main : Node2D
{
    // ---- pure, fully unit-tested systems -------------------------------------
    private readonly GameStateMachine _state = new();
    private readonly ScoreManager _score = new();
    private readonly SpeedController _speed = new();
    private readonly LaneController _lanes = new();
    private readonly JumpController _jump = new();
    private readonly DifficultyCurve _difficulty = new();
    private SpawnDirector _spawner = new(seed: 20260925);

    // ---- visuals -------------------------------------------------------------
    private PlayerView _player = null!;
    private Hud _hud = null!;
    private ScrollingGrid _grid = null!;
    private readonly List<ObstacleView> _obstacles = new();

    // ---- run state -----------------------------------------------------------
    private float _worldZ;
    private float _nextWaveZ = 30f;
    private int _runSeed = 20260925;

    private const float PixelsPerMetre = 100f;
    private const float PlayerScreenY = 1010f;
    private const float DespawnScreenY = 1500f;
    private const float SwipeThresholdPx = 42f;

    private const string SavePath = "user://neon_dash.cfg";

    private Vector2 _touchStart;
    private bool _tracking;
    private float _tickTimer;

    public override void _Ready()
    {
        _player = GetNode<PlayerView>("World/Player");
        _hud = GetNode<Hud>("Hud");
        _grid = GetNode<ScrollingGrid>("World");

        _score.LoadHighScore(LoadSavedHighScore());
        _hud.SetHighScore(_score.HighScore);
        _hud.ShowTitle();

        _state.StateChanged += (from, to) => TestProbe.Emit("state", $"from={from} to={to}");
        TestProbe.Emit("ready", $"best={_score.HighScore} viewport={GetViewportRect().Size.X:F0}x{GetViewportRect().Size.Y:F0}");
    }

    private int LoadSavedHighScore()
    {
        var cfg = new ConfigFile();
        return cfg.Load(SavePath) == Error.Ok
            ? (int)cfg.GetValue("progress", "high_score", 0)
            : 0;
    }

    private void SaveHighScore(int value)
    {
        var cfg = new ConfigFile();
        cfg.SetValue("progress", "high_score", value);
        cfg.Save(SavePath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _touchStart = touch.Position;
                _tracking = true;

                if (!_state.IsPlaying)
                    BeginOrRetry();
            }
            else
            {
                _tracking = false;
            }
        }
        else if (@event is InputEventScreenDrag drag && _tracking)
        {
            var delta = drag.Position - _touchStart;
            if (delta.Length() < SwipeThresholdPx) return;

            if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            {
                var dir = delta.X < 0 ? LaneDirection.Left : LaneDirection.Right;
                if (_lanes.TryMove(dir))
                    TestProbe.Emit("lane", $"dir={dir} to={_lanes.TargetLane}");
            }
            else if (delta.Y < 0 && _jump.Jump())
            {
                TestProbe.Emit("jump");
            }

            _touchStart = drag.Position;   // allow chained swipes
        }
    }

    private void BeginOrRetry()
    {
        if (_state.Current == GameState.GameOver)
        {
            _score.Reset();
            _speed.Reset();
            _lanes.Reset();
            _jump.Reset();
            ClearObstacles();
            _runSeed++;
            _spawner = new SpawnDirector(_runSeed);
            _worldZ = 0f;
            _nextWaveZ = 30f;
        }

        _state.Start();
        _player.Reset();
        _hud.ShowPlaying();
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;

        // Background scrolls even on the title screen, so the menu feels alive.
        var scrollSpeed = _state.IsPlaying ? _speed.CurrentSpeed : 2f;
        _grid.AdvanceGrid(scrollSpeed * dt * 100f);

        _tickTimer += dt;
        if (_tickTimer >= 2f)
        {
            _tickTimer = 0f;
            TestProbe.Emit("tick",
                $"state={_state.Current} score={_score.Score} lane={_lanes.CurrentLane} " +
                $"obstacles={_obstacles.Count} fps={Engine.GetFramesPerSecond():F0}");
        }

        if (!_state.IsPlaying) return;

        // --- forward motion -------------------------------------------------
        var speed = _speed.Update(dt);
        var travelled = speed * dt;
        _worldZ += travelled;
        _score.AddDistance(travelled);
        _speed.ApplyDistance(_score.Distance);

        // --- player ---------------------------------------------------------
        _lanes.Update(dt);
        var height = _jump.Update(dt);

        // --- spawning -------------------------------------------------------
        if (_worldZ + 70f >= _nextWaveZ)
        {
            var gap = _difficulty.GapFor(_speed.NormalisedProgress);
            foreach (var slot in _spawner.NextWave(_nextWaveZ))
                SpawnObstacle(slot);
            _nextWaveZ += gap;
        }

        // --- scroll + cull --------------------------------------------------
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            var o = _obstacles[i];
            o.Advance(_worldZ, PixelsPerMetre, PlayerScreenY);
            if (o.Position.Y > DespawnScreenY)
            {
                o.QueueFree();
                _obstacles.RemoveAt(i);
            }
        }

        // --- collision ------------------------------------------------------
        var playerBox = new Box(
            _lanes.RenderX, height + 0.9f, _worldZ,
            1.2f, 1.8f, 1.0f);

        foreach (var o in _obstacles)
        {
            if (!o.IsNearPlayer) continue;
            if (CollisionDetector.Intersects(playerBox, o.WorldBox))
            {
                Crash();
                return;
            }
        }

        _player.Apply(_lanes.RenderX, height, PixelsPerMetre, PlayerScreenY);
        _hud.SetScore(_score.Score, _speed.NormalisedProgress);
    }

    private void SpawnObstacle(SpawnSlot slot)
    {
        var scene = GD.Load<PackedScene>("res://Scenes/Obstacle.tscn");
        var view = scene.Instantiate<ObstacleView>();
        // AddChild BEFORE Configure. ObstacleView._Ready() is what resolves the
        // Body/Edge nodes, and _Ready() only runs once the node enters the
        // tree. Configuring first left those fields null and threw a
        // NullReferenceException from ObstacleView.Configure on every single
        // spawn (seen in the emulator logcat during the E2E run).
        _grid.AddChild(view);
        view.Configure(slot, _lanes.LaneToX(slot.Lane), PixelsPerMetre);
        _obstacles.Add(view);
    }

    private void Crash()
    {
        var previousBest = _score.HighScore;
        var final = _score.EndRun();
        var isNewBest = final > previousBest;
        _state.End();
        _player.PlayCrash();
        _hud.ShowGameOver(final, _score.HighScore, isNewBest);
        SaveHighScore(_score.HighScore);
        TestProbe.Emit("crash", $"score={final} best={_score.HighScore} newbest={isNewBest} lane={_lanes.CurrentLane}");
    }

    private void ClearObstacles()
    {
        foreach (var o in _obstacles) o.QueueFree();
        _obstacles.Clear();
    }
}
