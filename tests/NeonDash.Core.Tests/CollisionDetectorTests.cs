using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class CollisionDetectorTests
{
    private static Box At(float x, float y, float z) => new(x, y, z, 1f, 1f, 1f);

    [Fact]
    public void IdenticalBoxesCollide()
    {
        Assert.True(CollisionDetector.Intersects(At(0, 0, 0), At(0, 0, 0)));
    }

    [Fact]
    public void FarApartBoxesDoNotCollide()
    {
        Assert.False(CollisionDetector.Intersects(At(0, 0, 0), At(50, 0, 0)));
    }

    [Fact]
    public void DifferentLanesDoNotCollide()
    {
        // Player in lane 0 (x=-3), obstacle in lane 2 (x=+3), same depth.
        Assert.False(CollisionDetector.Intersects(At(-3, 0, 0), At(3, 0, 0)));
    }

    [Fact]
    public void JumpingOverLowBarClearsIt()
    {
        var player = At(0, 2.6f, 0);      // airborne, high
        var bar = At(0, 0.5f, 0);         // low obstacle
        Assert.False(CollisionDetector.Intersects(player, bar));
    }

    [Fact]
    public void NotJumpingHighEnoughStillHits()
    {
        var player = At(0, 1.0f, 0);
        var bar = At(0, 0.5f, 0);
        Assert.True(CollisionDetector.Intersects(player, bar));
    }

    [Fact]
    public void ObstacleBehindPlayerDoesNotCollide()
    {
        Assert.False(CollisionDetector.Intersects(At(0, 0, 0), At(0, 0, -10)));
    }

    [Fact]
    public void HitsAnyDetectsFirstOverlap()
    {
        var player = At(0, 0, 0);
        var obstacles = new[] { At(10, 0, 0), At(-10, 0, 0), At(0, 0, 0) };
        Assert.True(CollisionDetector.HitsAny(player, obstacles));
    }

    [Fact]
    public void HitsAnyReturnsFalseWhenClear()
    {
        var player = At(0, 0, 0);
        var obstacles = new[] { At(10, 0, 0), At(-10, 0, 0) };
        Assert.False(CollisionDetector.HitsAny(player, obstacles));
    }

    [Fact]
    public void BoxBoundsAreCentred()
    {
        var b = new Box(0, 0, 0, 2f, 4f, 6f);
        Assert.Equal(-1f, b.MinX);
        Assert.Equal(1f, b.MaxX);
        Assert.Equal(-2f, b.MinY);
        Assert.Equal(2f, b.MaxY);
        Assert.Equal(-3f, b.MinZ);
        Assert.Equal(3f, b.MaxZ);
    }
}
