using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class GameStateMachineTests
{
    [Fact]
    public void StartsInMenu()
    {
        var sm = new GameStateMachine();
        Assert.Equal(GameState.Menu, sm.Current);
        Assert.False(sm.IsPlaying);
    }

    [Fact]
    public void MenuToPlayingIsLegal()
    {
        var sm = new GameStateMachine();
        Assert.True(sm.CanTransitionTo(GameState.Playing));
        Assert.True(sm.Start());
        Assert.Equal(GameState.Playing, sm.Current);
        Assert.True(sm.IsPlaying);
    }

    [Fact]
    public void PlayingToMenuIsIllegal_AndChangesNothing()
    {
        var sm = new GameStateMachine();
        sm.Start();

        Assert.False(sm.CanTransitionTo(GameState.Menu));
        Assert.False(sm.ToMenu());
        Assert.Equal(GameState.Playing, sm.Current);   // unchanged
    }

    [Fact]
    public void PlayingToGameOverIsLegal()
    {
        var sm = new GameStateMachine();
        sm.Start();

        Assert.True(sm.End());
        Assert.Equal(GameState.GameOver, sm.Current);
        Assert.False(sm.IsPlaying);
    }

    [Fact]
    public void GameOverToPlayingRetries()
    {
        var sm = new GameStateMachine();
        sm.Start();
        sm.End();

        Assert.True(sm.Retry());
        Assert.Equal(GameState.Playing, sm.Current);
    }

    [Fact]
    public void IllegalTransitionDoesNotRaiseStateChanged()
    {
        var sm = new GameStateMachine();
        var raised = 0;
        sm.StateChanged += (_, _) => raised++;

        sm.Start();          // legal   -> 1
        sm.ToMenu();         // illegal -> still 1
        sm.End();            // legal   -> 2

        Assert.Equal(2, raised);
    }

    [Fact]
    public void StateChangedCarriesPreviousAndNext()
    {
        var sm = new GameStateMachine();
        GameState? from = null, to = null;
        sm.StateChanged += (p, n) => { from = p; to = n; };

        sm.Start();

        Assert.Equal(GameState.Menu, from);
        Assert.Equal(GameState.Playing, to);
    }

    [Theory]
    [InlineData(GameState.Menu, GameState.Menu, true)]
    [InlineData(GameState.Menu, GameState.Playing, true)]
    [InlineData(GameState.Menu, GameState.GameOver, false)]
    [InlineData(GameState.Playing, GameState.Playing, false)]
    [InlineData(GameState.Playing, GameState.GameOver, true)]
    [InlineData(GameState.Playing, GameState.Menu, false)]
    [InlineData(GameState.GameOver, GameState.Playing, true)]
    [InlineData(GameState.GameOver, GameState.Menu, true)]
    [InlineData(GameState.GameOver, GameState.GameOver, false)]
    public void TransitionTableIsCorrect(GameState from, GameState to, bool expected)
    {
        var sm = new GameStateMachine(from);
        Assert.Equal(expected, sm.CanTransitionTo(to));
    }
}
