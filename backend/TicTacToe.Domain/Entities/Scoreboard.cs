namespace TicTacToe.Domain.Entities;

using System;
using System.Collections.Generic;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Scoreboard aggregate root tracking session-level game outcomes (X wins, O wins, Draws).
/// Thread-safe via internal synchronization.
/// Guarantees at-most-once mutation per unique EventId.
/// </summary>
public sealed class Scoreboard
{
    private readonly object _syncLock = new();
    private readonly HashSet<Guid> _processedEventIds = new();

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
    /// Atomically records a completed game event if it has not been previously processed.
    /// Thread-safe via internal synchronization.
    /// </summary>
    /// <param name="gameCompletedEvent">The completed game domain event.</param>
    /// <returns>True if the event was processed and counters incremented; false if it was already processed.</returns>
    public bool RecordGameCompleted(GameCompletedEvent gameCompletedEvent)
    {
        if (gameCompletedEvent == null)
        {
            throw new ArgumentNullException(nameof(gameCompletedEvent));
        }

        lock (_syncLock)
        {
            // Idempotency check: ignore if this exact event was already processed
            if (!_processedEventIds.Add(gameCompletedEvent.EventId))
            {
                return false;
            }

            if (gameCompletedEvent.Result == GameStatus.Won)
            {
                if (gameCompletedEvent.Winner == Player.X)
                {
                    XWins++;
                }
                else if (gameCompletedEvent.Winner == Player.O)
                {
                    OWins++;
                }
            }
            else if (gameCompletedEvent.Result == GameStatus.Draw)
            {
                Draws++;
            }

            return true;
        }
    }

    /// <summary>
    /// Resets score counters to 0 while preserving processed event history to guarantee idempotency.
    /// Thread-safe via internal synchronization.
    /// </summary>
    public void Reset()
    {
        lock (_syncLock)
        {
            XWins = 0;
            OWins = 0;
            Draws = 0;
            // NOTE: _processedEventIds is preserved to prevent double-counting redelivered historical events.
        }
    }
}
