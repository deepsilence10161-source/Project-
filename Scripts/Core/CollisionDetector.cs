namespace NeonDash.Core;

/// <summary>A 3D-ish axis-aligned box used for collision tests.</summary>
public readonly record struct Box(float X, float Y, float Z, float Width, float Height, float Depth)
{
    public float MinX => X - (Width * 0.5f);
    public float MaxX => X + (Width * 0.5f);
    public float MinY => Y - (Height * 0.5f);
    public float MaxY => Y + (Height * 0.5f);
    public float MinZ => Z - (Depth * 0.5f);
    public float MaxZ => Z + (Depth * 0.5f);
}

/// <summary>
/// Static AABB overlap helpers. Separation-based (no penetration resolution)
/// is all a runner needs: either you hit the obstacle or you don't.
/// Pure logic, no engine dependencies.
/// </summary>
public static class CollisionDetector
{
    public static bool Intersects(in Box a, in Box b) =>
        a.MinX <= b.MaxX && a.MaxX >= b.MinX &&
        a.MinY <= b.MaxY && a.MaxY >= b.MinY &&
        a.MinZ <= b.MaxZ && a.MaxZ >= b.MinZ;

    /// <summary>
    /// True when the player box overlaps any obstacle box.
    /// </summary>
    public static bool HitsAny(in Box player, IEnumerable<Box> obstacles)
    {
        foreach (var o in obstacles)
            if (Intersects(player, o)) return true;
        return false;
    }
}
