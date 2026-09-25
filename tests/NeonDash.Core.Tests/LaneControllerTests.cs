using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class LaneControllerTests
{
    [Fact]
    public void StartsCentredInMiddleLane()
    {
        var l = new LaneController();
        Assert.Equal(1, l.CurrentLane);
        Assert.Equal(0f, l.RenderX);          // middle lane is x = 0
        Assert.True(l.IsSettled);
        Assert.False(l.IsMoving);
    }

    [Fact]
    public void LaneToXSpacesLanesSymmetrically()
    {
        var l = new LaneController(laneCount: 3, laneSpacing: 3);
        Assert.Equal(-3f, l.LaneToX(0));
        Assert.Equal(0f, l.LaneToX(1));
        Assert.Equal(3f, l.LaneToX(2));
    }

    [Fact]
    public void MoveLeftChangesTargetLane()
    {
        var l = new LaneController();
        Assert.True(l.TryMove(LaneDirection.Left));
        Assert.Equal(0, l.TargetLane);
        Assert.True(l.IsMoving);
    }

    [Fact]
    public void CannotMovePastLeftEdge()
    {
        var l = new LaneController();
        Assert.True(l.TryMove(LaneDirection.Left));   // 1 -> 0
        l.Update(1f);                                  // settle
        Assert.False(l.TryMove(LaneDirection.Left));  // already at edge
        Assert.Equal(0, l.TargetLane);
    }

    [Fact]
    public void CannotMovePastRightEdge()
    {
        var l = new LaneController();
        Assert.True(l.TryMove(LaneDirection.Right));  // 1 -> 2
        l.Update(1f);
        Assert.False(l.TryMove(LaneDirection.Right));
        Assert.Equal(2, l.TargetLane);
    }

    [Fact]
    public void MoveIgnoredWhileStillSliding()
    {
        var l = new LaneController();
        Assert.True(l.TryMove(LaneDirection.Left));
        Assert.False(l.TryMove(LaneDirection.Left));   // mid-slide
    }

    [Fact]
    public void RenderXInterpolatesFromOldToNewLane()
    {
        var l = new LaneController(laneSpacing: 3);
        l.TryMove(LaneDirection.Right);                // target 2 -> x = 3

        l.Update(0.07f);                               // half of 0.14s
        Assert.Equal(1.5f, l.RenderX, precision: 3);

        l.Update(0.07f);                               // completes
        Assert.Equal(3f, l.RenderX, precision: 3);
    }

    [Fact]
    public void CurrentLaneCommitsOnlyWhenSettled()
    {
        var l = new LaneController();
        l.TryMove(LaneDirection.Left);

        l.Update(0.05f);                               // still moving
        Assert.Equal(1, l.CurrentLane);                // not committed yet
        Assert.Equal(0, l.TargetLane);

        l.Update(0.5f);                                // settle
        Assert.Equal(0, l.CurrentLane);                // committed
    }

    [Fact]
    public void FullSlideThenSettleKeepsPosition()
    {
        var l = new LaneController();
        l.TryMove(LaneDirection.Left);
        l.Update(1f);
        l.Update(1f);                                  // extra frame, no drift

        Assert.Equal(-3f, l.RenderX, precision: 3);
        Assert.True(l.IsSettled);
    }

    [Fact]
    public void ResetReturnsToRequestedLane()
    {
        var l = new LaneController();
        l.TryMove(LaneDirection.Right);
        l.Update(1f);

        l.Reset(startLane: 1);

        Assert.Equal(1, l.CurrentLane);
        Assert.Equal(0f, l.RenderX, precision: 3);
    }

    [Fact]
    public void InvalidStartLaneThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LaneController(startLane: 9));
    }

    [Theory]
    [InlineData(0.1f)]
    [InlineData(1f)]
    public void UpdateSnapsWhenDurationReached(float dt)
    {
        var l = new LaneController();
        l.TryMove(LaneDirection.Right);
        l.Update(dt, slideDuration: 0.14f);
        l.Update(dt, slideDuration: 0.14f);

        Assert.Equal(3f, l.RenderX, precision: 3);
        Assert.Equal(2, l.CurrentLane);
    }
}
