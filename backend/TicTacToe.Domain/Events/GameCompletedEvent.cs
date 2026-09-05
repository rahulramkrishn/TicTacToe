namespace TicTacToe.Domain.Events;

using System;
using System.Collections.Generic;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Immutable domain event raised when a Game reaches a terminal state (Won or Draw).
/// Contains a self-contained payload sufficient for downstream handlers without re-evaluating or querying the Game aggregate.
/// </summary>
public sealed record GameCompletedEvent : IDomainEvent
{
    public Guid EventId { get; }
    public GameId GameId { get; }
    public GameStatus Result { get; }
    public Player? Winner { get; }
    public IReadOnlyList<int> WinningCells { get; }
    public int MoveCount { get; }
    public DateTimeOffset OccurredOn { get; }

    public GameCompletedEvent(
        Guid eventId,
        GameId gameId,
        GameStatus result,
        Player? winner,
        IReadOnlyList<int> winningCells,
        int moveCount,
        DateTimeOffset occurredOn)
    {
        if (result == GameStatus.InProgress)
        {
            throw new ArgumentException("GameCompletedEvent cannot be created with InProgress status.", nameof(result));
        }

        if (result == GameStatus.Won)
        {
            if (winner == null)
            {
                throw new ArgumentException("Winner must be specified when result is Won.", nameof(winner));
            }

            if (winningCells == null || winningCells.Count != 3)
            {
                throw new ArgumentException("WinningCells must contain exactly 3 cells when result is Won.", nameof(winningCells));
            }
        }
        else if (result == GameStatus.Draw)
        {
            if (winner != null)
            {
                throw new ArgumentException("Winner must be null when result is Draw.", nameof(winner));
            }

            if (winningCells != null && winningCells.Count > 0)
            {
                throw new ArgumentException("WinningCells must be empty when result is Draw.", nameof(winningCells));
            }
        }

        if (gameId.Value == Guid.Empty)
        {
            throw new ArgumentException("GameId cannot be empty.", nameof(gameId));
        }

        EventId = eventId;
        GameId = gameId;
        Result = result;
        Winner = winner;
        WinningCells = winningCells ?? Array.Empty<int>();
        MoveCount = moveCount;
        OccurredOn = occurredOn;
    }
}
