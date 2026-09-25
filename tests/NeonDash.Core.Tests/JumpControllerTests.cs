using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class JumpControllerTests
{
    [Fact]
    public void StartsGrounded()
    {
        var j = new JumpController();
        Assert.True(j.IsGrounded);
        Assert.False(j.IsJumping);
        Assert.Equal(0f, j.Height);
    }

    [Fact]
    public void JumpLeavesTheGround()
    {
        var j = new JumpController();
        Assert.True(j.Jump());
        Assert.True(j.IsJumping);
        Assert.True(j.VerticalVelocity > 0f);
    }

    [Fact]
    public void CannotDoubleJump()
    {
        var j = new JumpController();
        Assert.True(j.Jump());
        Assert.False(j.Jump());      // already airborne
    }

    [Fact]
    public void ApexApproachesConfiguredHeight()
    {
        var j = new JumpController(jumpHeight: 2.6f, gravity: 22f);
        j.Jump();

        var peak = 0f;
        for (var i = 0; i < 600; i++)          // 10 seconds at 60fps
        {
            j.Update(1f / 60f);
            peak = MathF.Max(peak, j.Height);
        }

        Assert.InRange(peak, 2.4f, 2.65f);
    }

    [Fact]
    public void ReturnsToGroundAfterJump()
    {
        var j = new JumpController();
        j.Jump();

        for (var i = 0; i < 600; i++) j.Update(1f / 60f);

        Assert.True(j.IsGrounded);
        Assert.Equal(0f, j.Height);
        Assert.Equal(0f, j.VerticalVelocity);
    }

    [Fact]
    public void HeightNeverGoesNegative()
    {
        var j = new JumpController();
        j.Jump();

        for (var i = 0; i < 1200; i++)
        {
            j.Update(1f / 60f);
            Assert.True(j.Height >= 0f);
        }
    }

    [Fact]
    public void CanJumpAgainAfterLanding()
    {
        var j = new JumpController();
        j.Jump();
        for (var i = 0; i < 600; i++) j.Update(1f / 60f);

        Assert.True(j.Jump());
    }

    [Fact]
    public void NegativeDeltaThrows()
    {
        var j = new JumpController();
        Assert.Throws<ArgumentOutOfRangeException>(() => j.Update(-1f));
    }

    [Fact]
    public void ResetLandsImmediately()
    {
        var j = new JumpController();
        j.Jump();
        j.Update(0.05f);
        j.Reset();

        Assert.True(j.IsGrounded);
        Assert.Equal(0f, j.Height);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void ConstructorRejectsBadValues(float gravity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JumpController(gravity: gravity));
    }
}
