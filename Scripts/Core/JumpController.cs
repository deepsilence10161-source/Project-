namespace NeonDash.Core;

/// <summary>
/// Vertical jump physics (height only - forward motion lives in
/// <see cref="SpeedController"/>). Uses a simple parabola so behaviour is
/// deterministic and testable without a physics engine.
/// Pure logic, no engine dependencies.
/// </summary>
public sealed class JumpController
{
    public float JumpHeight { get; }
    public float Gravity { get; }

    // _airborne is tracked separately from Height so a jump registers the
    // instant it starts, not one physics step later. Without it, IsGrounded
    // stays true until the first Update() and the player can double-jump.
    private bool _airborne;

    /// <summary>Current height above the ground plane, in metres.</summary>
    public float Height { get; private set; }

    /// <summary>Vertical velocity, metres per second. Negative = falling.</summary>
    public float VerticalVelocity { get; private set; }

    public bool IsGrounded => !_airborne && Height <= 0f;

    public bool IsJumping => _airborne || Height > 0f;

    public JumpController(float jumpHeight = 2.6f, float gravity = 22f)
    {
        if (jumpHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(jumpHeight));
        if (gravity <= 0f) throw new ArgumentOutOfRangeException(nameof(gravity));
        JumpHeight = jumpHeight;
        Gravity = gravity;
    }

    /// <summary>Starts a jump. Returns false when already airborne.</summary>
    public bool Jump()
    {
        if (!IsGrounded) return false;
        // v = sqrt(2*g*h) gives an apex of exactly JumpHeight.
        VerticalVelocity = MathF.Sqrt(2f * Gravity * JumpHeight);
        _airborne = true;
        return true;
    }

    /// <summary>
    /// Integrates one physics step. Returns the new <see cref="Height"/>.
    /// Height is clamped at 0 so the player can never sink through the floor.
    /// </summary>
    public float Update(float deltaTime)
    {
        if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

        if (!_airborne)
        {
            Height = 0f;
            VerticalVelocity = 0f;
            return Height;
        }

        VerticalVelocity -= Gravity * deltaTime;
        Height += VerticalVelocity * deltaTime;

        if (Height <= 0f)
        {
            Height = 0f;
            VerticalVelocity = 0f;
            _airborne = false;
        }

        return Height;
    }

    public void Reset()
    {
        Height = 0f;
        VerticalVelocity = 0f;
        _airborne = false;
    }
}
