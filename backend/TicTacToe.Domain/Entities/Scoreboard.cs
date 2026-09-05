namespace TicTacToe.Domain.Entities;

using System;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Scoreboard aggregate root tracking session-level game outcomes (X wins, O wins, Draws).
/// Thread-safe via internal synchronization (_syncLock) across parallel game completions.
/// </summary>
public sealed class Scoreboard
{
    private readonly object _syncLock = new();

    public int XWins { get; private set; }
    public int OWins { get; private set; }
    public int Draws { get; private set; }

    public int TotalGames
    {
        get
        {
            lock (_syncLock)
            {
                return XWins + OWins + Draws;
            }
        }
    }

    public Scoreboard() : this(0, 0, 0)
    {
    }

    public Scoreboard(int xWins, int oWins, int draws)
    {
        XWins = Math.Max(0, xWins);
        OWins = Math.Max(0, oWins);
        Draws = Math.Max(0, draws);
    }

    /// <summary>
    /// Atomically increments the win counter for the winning player.
    /// Thread-safe via internal synchronization.
    /// </summary>
    /// <param name="winner">The player who won (X or O).</param>
    public void RecordWin(Player winner)
    {
        lock (_syncLock)
        {
            if (winner == Player.X)
            {
                XWins++;
            }
            else if (winner == Player.O)
            {
                OWins++;
            }
        }
    }

    /// <summary>
    /// Atomically increments the draw counter.
    /// Thread-safe via internal synchronization.
    /// </summary>
    public void RecordDraw()
    {
        lock (_syncLock)
        {
            Draws++;
        }
    }

    /// <summary>
    /// Resets score counters to 0.
    /// Thread-safe via internal synchronization.
    /// </summary>
    public void Reset()
    {
        lock (_syncLock)
        {
            XWins = 0;
            OWins = 0;
            Draws = 0;
        }
    }
}
