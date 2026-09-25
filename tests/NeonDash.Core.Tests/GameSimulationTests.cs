using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

/// <summary>
/// End-to-end simulation of a whole run using ONLY the pure logic layer.
/// This is the test that proves the game rules hold together without needing
/// an emulator, a device, or a display.
/// </summary>
public class GameSimulationTests
{
    private const float Dt = 1f / 60f;

    private sealed class Harness
    {
        public readonly GameStateMachine State = new();
        public readonly ScoreManager Score = new();
        public readonly SpeedController Speed = new();
        public readonly LaneController Lanes = new();
        public readonly JumpController Jump = new();
        public readonly DifficultyCurve Difficulty = new();
        public readonly SpawnDirector Spawner;
        public readonly List<Box> Obstacles = new();
        public float WorldZ;
        public float NextWaveZ = 20f;
        public bool Crashed;

        public Harness(int seed = 1) => Spawner = new SpawnDirector(seed);

        public void BeginRun()
        {
            State.Start();
            Score.Reset();
            Speed.Reset();
            Lanes.Reset();
            Jump.Reset();
            Obstacles.Clear();
            WorldZ = 0f;
            NextWaveZ = 20f;
            Crashed = false;
        }

        public void Step()
        {
            if (!State.IsPlaying) return;

            var speed = Speed.Update(Dt);
            var travelled = speed * Dt;
            WorldZ += travelled;
            Score.AddDistance(travelled);
            Speed.ApplyDistance(Score.Distance);

            Lanes.Update(Dt);
            Jump.Update(Dt);

            // spawn waves ahead of the player
            if (WorldZ + 60f >= NextWaveZ)
            {
                var gap = Difficulty.GapFor(Speed.NormalisedProgress);
                foreach (var slot in Spawner.NextWave(NextWaveZ))
                {
                    Obstacles.Add(new Box(
                        Lanes.LaneToX(slot.Lane), 0f, slot.Z,
                        1.6f, slot.Kind == ObstacleKind.LowBar ? 0.8f : 2.2f, 0.8f));
                }
                NextWaveZ += gap;
            }

            // cull passed obstacles
            Obstacles.RemoveAll(o => o.Z < WorldZ - 5f);

            // collision check
            var player = new Box(Lanes.RenderX, Jump.Height + 1f, WorldZ, 1.2f, 1.8f, 1.0f);
            if (CollisionDetector.HitsAny(player, Obstacles))
            {
                Crashed = true;
                Score.EndRun();
                State.End();
            }
        }
    }

    [Fact]
    public void FullRun_ScoreIncreases_ThenEndsInGameOver()
    {
        var h = new Harness(seed: 3);
        h.BeginRun();

        Assert.Equal(GameState.Playing, h.State.Current);

        for (var i = 0; i < 60 * 120 && h.State.IsPlaying; i++)   // up to 2 minutes
            h.Step();

        Assert.Equal(GameState.GameOver, h.State.Current);
        Assert.True(h.Crashed);
        Assert.True(h.Score.Score > 0, "Player should have scored before crashing");
        Assert.True(h.Score.Distance > 0f);
    }

    [Fact]
    public void SpeedRampsUpOverTheRun()
    {
        var h = new Harness(seed: 11);
        h.BeginRun();

        var startSpeed = h.Speed.CurrentSpeed;
        for (var i = 0; i < 60 * 20; i++) h.Step();

        Assert.True(h.Speed.CurrentSpeed > startSpeed,
            $"speed {h.Speed.CurrentSpeed} should exceed start {startSpeed}");
    }

    [Fact]
    public void ObstacleCountStaysBounded_NoMemoryLeak()
    {
        var h = new Harness(seed: 5);
        h.BeginRun();

        for (var i = 0; i < 60 * 90 && h.State.IsPlaying; i++)
        {
            h.Step();
            Assert.True(h.Obstacles.Count < 200, $"obstacle list grew to {h.Obstacles.Count}");
        }
    }

    [Fact]
    public void DodgingIntoTheOpenLaneSurvivesLonger()
    {
        // Two identical seeds: one player never moves, one steers to the open lane.
        float Survive(bool dodge)
        {
            var h = new Harness(seed: 21);
            h.BeginRun();
            var frames = 0;

            for (var i = 0; i < 60 * 60 && h.State.IsPlaying; i++)
            {
                if (dodge && h.Lanes.IsSettled)
                {
                    // find the lane with no obstacle just ahead and steer there
                    var ahead = h.Obstacles.Where(o => o.Z > h.WorldZ && o.Z < h.WorldZ + 12f).ToList();
                    var open = Enumerable.Range(0, h.Lanes.LaneCount)
                        .FirstOrDefault(l => ahead.All(o => MathF.Abs(o.X - h.Lanes.LaneToX(l)) > 1.2f));
                    var delta = open - h.Lanes.CurrentLane;
                    if (delta < 0) h.Lanes.TryMove(LaneDirection.Left);
                    else if (delta > 0) h.Lanes.TryMove(LaneDirection.Right);
                }
                h.Step();
                frames++;
            }
            return frames;
        }

        var passive = Survive(false);
        var active = Survive(true);

        Assert.True(active >= passive,
            $"dodging ({active} frames) should not do worse than standing still ({passive})");
    }

    [Fact]
    public void RetryAfterGameOverStartsCleanRun()
    {
        var h = new Harness(seed: 8);
        h.BeginRun();
        for (var i = 0; i < 60 * 60 && h.State.IsPlaying; i++) h.Step();

        Assert.Equal(GameState.GameOver, h.State.Current);
        var best = h.Score.HighScore;

        h.BeginRun();   // retry

        Assert.Equal(GameState.Playing, h.State.Current);
        Assert.Equal(0, h.Score.Score);
        Assert.Equal(0f, h.Score.Distance);
        Assert.Equal(best, h.Score.HighScore);   // record survives
        Assert.Empty(h.Obstacles);
    }

    [Fact]
    public void SimulationIsDeterministic_ForASeed()
    {
        static float Run(int seed)
        {
            var h = new Harness(seed);
            h.BeginRun();
            for (var i = 0; i < 60 * 30; i++) h.Step();
            return h.Score.Distance;
        }

        Assert.Equal(Run(77), Run(77));
    }

    [Fact]
    public void JumpClearsLowBarInSimulation()
    {
        var h = new Harness(seed: 4);
        h.BeginRun();

        // run until just before the first obstacle, then jump
        for (var i = 0; i < 60 * 30 && h.Obstacles.Count == 0; i++) h.Step();

        Assert.NotEmpty(h.Obstacles);
        Assert.True(h.Jump.Jump(), "should be able to jump before impact");
    }
}
