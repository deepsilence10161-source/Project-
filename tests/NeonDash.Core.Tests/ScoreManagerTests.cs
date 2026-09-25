using NeonDash.Core;
using Xunit;

namespace NeonDash.Core.Tests;

public class ScoreManagerTests
{
    [Fact]
    public void NewGameScoresZero()
    {
        var s = new ScoreManager();
        Assert.Equal(0, s.Score);
        Assert.Equal(0, s.Coins);
        Assert.Equal(0f, s.Distance);
    }

    [Fact]
    public void ScoreIsDistancePlusCoins()
    {
        var s = new ScoreManager();
        s.AddDistance(12.7f);   // truncates to 12
        s.AddCoin(3);           // 30

        Assert.Equal(12 + 30, s.Score);
    }

    [Fact]
    public void DistanceTruncatesNotRounds()
    {
        var s = new ScoreManager();
        s.AddDistance(9.99f);
        Assert.Equal(9, s.Score);
    }

    [Fact]
    public void CoinsAreWorthTenEach()
    {
        var s = new ScoreManager();
        s.AddCoin(5);
        Assert.Equal(ScoreManager.CoinValue * 5, s.Score);
    }

    [Fact]
    public void NegativeDistanceThrows()
    {
        var s = new ScoreManager();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.AddDistance(-1f));
    }

    [Fact]
    public void EndRunSetsHighScore()
    {
        var s = new ScoreManager();
        s.AddDistance(100f);
        s.AddCoin(2);

        Assert.Equal(120, s.EndRun());
        Assert.Equal(120, s.HighScore);
    }

    [Fact]
    public void HighScoreOnlyGoesUp()
    {
        var s = new ScoreManager();

        s.AddDistance(100f);
        s.EndRun();                       // 100

        s.Reset();
        s.AddDistance(5f);
        s.EndRun();                       // 5 -> must not lower the record

        Assert.Equal(100, s.HighScore);
    }

    [Fact]
    public void ResetClearsRunButKeepsHighScore()
    {
        var s = new ScoreManager();
        s.AddDistance(50f);
        s.AddCoin(4);
        s.EndRun();

        s.Reset();

        Assert.Equal(0, s.Score);
        Assert.Equal(0, s.Coins);
        Assert.Equal(0f, s.Distance);
        Assert.Equal(90, s.HighScore);
    }

    [Fact]
    public void OnHighScoreFiresOnlyOnImprovement()
    {
        var fired = new List<int>();
        var s = new ScoreManager { OnHighScore = v => fired.Add(v) };

        s.AddDistance(10f); s.EndRun();   // fires 10
        s.Reset();
        s.AddDistance(3f);  s.EndRun();   // no fire

        Assert.Equal(new[] { 10 }, fired);
    }

    [Fact]
    public void LoadHighScoreRestoresPersistedValue()
    {
        var s = new ScoreManager();
        s.LoadHighScore(4321);
        Assert.Equal(4321, s.HighScore);
    }

    [Fact]
    public void LoadNegativeHighScoreThrows()
    {
        var s = new ScoreManager();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.LoadHighScore(-5));
    }
}
