using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class SpeedControllerTests
{
    [Fact]
    public void StartsAtBaseSpeed()
    {
        var c = new SpeedController(baseSpeed: 8f);
        Assert.Equal(8f, c.CurrentSpeed);
        Assert.Equal(0f, c.NormalisedProgress);
    }

    [Fact]
    public void TimeRampIncreasesSpeed()
    {
        var c = new SpeedController(baseSpeed: 8f, maxSpeed: 26f, acceleration: 1f);
        c.Update(1f);
        Assert.Equal(9f, c.CurrentSpeed);
    }

    [Fact]
    public void SpeedNeverExceedsMax()
    {
        var c = new SpeedController(baseSpeed: 8f, maxSpeed: 20f, acceleration: 100f);
        for (var i = 0; i < 100; i++) c.Update(1f);
        Assert.Equal(20f, c.CurrentSpeed);
        Assert.Equal(1f, c.NormalisedProgress);
    }

    [Fact]
    public void DistanceRampIsClamped()
    {
        var c = new SpeedController(baseSpeed: 8f, maxSpeed: 20f, speedPerMetre: 1f);
        c.ApplyDistance(1000f);          // would be 1008 unclamped
        Assert.Equal(20f, c.CurrentSpeed);
    }

    [Fact]
    public void DistanceRampAtZeroMetresEqualsBase()
    {
        var c = new SpeedController(baseSpeed: 8f, maxSpeed: 20f, speedPerMetre: 0.5f);
        c.ApplyDistance(0f);
        Assert.Equal(8f, c.CurrentSpeed);
    }

    [Fact]
    public void NegativeDeltaThrows()
    {
        var c = new SpeedController();
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Update(-0.1f));
    }

    [Fact]
    public void ResetReturnsToBaseSpeed()
    {
        var c = new SpeedController(baseSpeed: 8f, maxSpeed: 26f, acceleration: 1f);
        c.Update(5f);
        c.Reset();
        Assert.Equal(8f, c.CurrentSpeed);
    }

    [Theory]
    [InlineData(26f, 8f)]     // max below base
    [InlineData(0f, 10f)]     // non-positive base
    [InlineData(10f, -1f)]    // negative max
    public void ConstructorRejectsBadRanges(float baseSpeed, float maxSpeed)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SpeedController(baseSpeed: baseSpeed, maxSpeed: maxSpeed));
    }
}
