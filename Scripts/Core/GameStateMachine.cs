namespace NeonDash.Core;

/// <summary>Game lifecycle states.</summary>
public enum GameState
{
    Menu,
    Playing,
    GameOver
}

/// <summary>
/// Explicit, validated state machine for the game loop.
/// Illegal transitions are rejected so the UI can never end up in an
/// impossible combination (e.g. "GameOver" while still "Playing").
/// Pure logic: no engine dependencies, fully unit-testable.
/// </summary>
public sealed class GameStateMachine
{
    public GameState Current { get; private set; } = GameState.Menu;

    /// <summary>Raised whenever <see cref="Current"/> changes.</summary>
    public event Action<GameState, GameState>? StateChanged;

    public GameStateMachine(GameState initial = GameState.Menu)
    {
        Current = initial;
    }

    /// <summary>Returns true when a transition from the current state is legal.</summary>
    public bool CanTransitionTo(GameState next) => (Current, next) switch
    {
        (GameState.Menu, GameState.Playing) => true,
        (GameState.Playing, GameState.GameOver) => true,
        (GameState.GameOver, GameState.Playing) => true,   // retry
        (GameState.GameOver, GameState.Menu) => true,      // back to title
        (GameState.Menu, GameState.Menu) => true,
        (_, _) => false
    };

    /// <summary>
    /// Attempts a transition. Returns false (and changes nothing) when illegal.
    /// </summary>
    public bool TryTransition(GameState next)
    {
        if (!CanTransitionTo(next))
            return false;

        var previous = Current;
        Current = next;
        StateChanged?.Invoke(previous, next);
        return true;
    }

    public bool Start() => TryTransition(GameState.Playing);
    public bool End() => TryTransition(GameState.GameOver);
    public bool Retry() => TryTransition(GameState.Playing);
    public bool ToMenu() => TryTransition(GameState.Menu);

    public bool IsPlaying => Current == GameState.Playing;
}
