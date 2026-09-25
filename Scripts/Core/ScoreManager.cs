namespace NeonDash.Core;

/// <summary>
/// Tracks distance, coins and the derived score.
/// Distance is accumulated in metres; coins are worth a fixed bonus.
/// Pure logic, no engine dependencies.
/// </summary>
public sealed class ScoreManager
{
    public const int CoinValue = 10;

    private int _coins;
    private float _distance;

    /// <summary>Total metres run in the current attempt.</summary>
    public float Distance => _distance;

    /// <summary>Coins collected in the current attempt.</summary>
    public int Coins => _coins;

    /// <summary>Score = whole metres run + coins x <see cref="CoinValue"/>.</summary>
    public int Score => (int)_distance + (_coins * CoinValue);

    /// <summary>Best score ever achieved. Updated automatically on <see cref="EndRun"/>.</summary>
    public int HighScore { get; private set; }

    /// <summary>Called when a new high score is set, so the host can persist it.</summary>
    public Action<int>? OnHighScore { get; init; }

    public void AddDistance(float metres)
    {
        if (metres < 0f)
            throw new ArgumentOutOfRangeException(nameof(metres), "Distance cannot be negative.");
        _distance += metres;
    }

    public void AddCoin(int amount = 1)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Coin amount cannot be negative.");
        _coins += amount;
    }

    /// <summary>Finalises the run, folding the score into <see cref="HighScore"/>.</summary>
    public int EndRun()
    {
        var score = Score;
        if (score > HighScore)
        {
            HighScore = score;
            OnHighScore?.Invoke(HighScore);
        }
        return score;
    }

    public void Reset()
    {
        _distance = 0f;
        _coins = 0;
    }

    /// <summary>Loads a persisted high score (e.g. from disk on startup).</summary>
    public void LoadHighScore(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "High score cannot be negative.");
        HighScore = value;
    }
}
