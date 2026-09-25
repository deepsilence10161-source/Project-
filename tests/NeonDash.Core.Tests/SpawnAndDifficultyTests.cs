using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class SpawnDirectorTests
{
    [Fact]
    public void WaveAlwaysLeavesOneLaneOpen()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var d = new SpawnDirector(seed);
            for (var w = 0; w < 50; w++)
            {
                var wave = d.NextWave(0f);
                var blocked = wave.Select(s => s.Lane).Distinct().Count();
                Assert.True(blocked < d.LaneCount,
                    $"seed {seed} wave {w}: all {blocked} lanes blocked - unwinnable");
            }
        }
    }

    [Fact]
    public void WaveNeverBlocksMoreThanLaneCountMinusOne()
    {
        var d = new SpawnDirector(42, laneCount: 3);
        for (var w = 0; w < 100; w++)
            Assert.True(d.NextWave(0f).Count <= 2);
    }

    [Fact]
    public void WaveHasAtLeastOneObstacle()
    {
        var d = new SpawnDirector(7);
        for (var w = 0; w < 100; w++)
            Assert.NotEmpty(d.NextWave(0f));
    }

    [Fact]
    public void SameSeedProducesSamePattern()
    {
        var a = new SpawnDirector(1234);
        var b = new SpawnDirector(1234);

        for (var w = 0; w < 20; w++)
            Assert.Equal(a.NextWave(w * 10f), b.NextWave(w * 10f));
    }

    [Fact]
    public void DifferentSeedsDiverge()
    {
        var a = new SpawnDirector(1);
        var b = new SpawnDirector(2);

        var same = 0;
        for (var w = 0; w < 20; w++)
            if (a.NextWave(w * 10f).SequenceEqual(b.NextWave(w * 10f))) same++;

        Assert.True(same < 20, "Different seeds produced identical sequences");
    }

    [Fact]
    public void SlotsCarryRequestedZ()
    {
        var d = new SpawnDirector(5);
        var wave = d.NextWave(123.5f);
        Assert.All(wave, s => Assert.Equal(123.5f, s.Z));
    }

    [Fact]
    public void LanesAreAlwaysValidIndices()
    {
        var d = new SpawnDirector(99, laneCount: 3);
        for (var w = 0; w < 200; w++)
            Assert.All(d.NextWave(0f), s => Assert.InRange(s.Lane, 0, 2));
    }

    [Fact]
    public void SingleLaneDirectorThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnDirector(1, laneCount: 1));
    }
}

public class DifficultyCurveTests
{
    [Fact]
    public void SlowSpeedGivesMaxGap()
    {
        var c = new DifficultyCurve(maxGap: 26f, minGap: 11f);
        Assert.Equal(26f, c.GapFor(0f));
    }

    [Fact]
    public void FastSpeedGivesMinGap()
    {
        var c = new DifficultyCurve(maxGap: 26f, minGap: 11f);
        Assert.Equal(11f, c.GapFor(1f));
    }

    [Fact]
    public void MidSpeedInterpolates()
    {
        var c = new DifficultyCurve(maxGap: 26f, minGap: 11f);
        Assert.Equal(18.5f, c.GapFor(0.5f), precision: 3);
    }

    [Theory]
    [InlineData(-5f)]
    [InlineData(2f)]
    public void OutOfRangeInputIsClamped(float input)
    {
        var c = new DifficultyCurve(maxGap: 26f, minGap: 11f);
        var gap = c.GapFor(input);
        Assert.InRange(gap, 11f, 26f);
    }

    [Fact]
    public void GapNeverBelowFloor()
    {
        var c = new DifficultyCurve(maxGap: 30f, minGap: 12f);
        for (var t = 0f; t <= 1f; t += 0.01f)
            Assert.True(c.GapFor(t) >= 12f);
    }

    [Fact]
    public void BadRangeThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DifficultyCurve(maxGap: 5f, minGap: 10f));
    }
}
