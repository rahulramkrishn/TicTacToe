namespace TicTacToe.Domain.Exceptions;

using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Thrown when attempting to make a move on a game that is already in a terminal state (Won or Draw).
/// </summary>
public sealed class GameAlreadyCompletedException : DomainException
{
    public GameStatus CurrentStatus { get; }

    public GameAlreadyCompletedException(GameStatus currentStatus)
        : base($"Cannot make a move on a completed game. Current status: {currentStatus}.")
    {
        CurrentStatus = currentStatus;
    }
}
