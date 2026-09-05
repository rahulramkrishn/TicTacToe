namespace TicTacToe.Application.Exceptions;

using System;

/// <summary>
/// Transport-neutral application exception thrown when a requested Game aggregate does not exist.
/// </summary>
public sealed class GameNotFoundException : Exception
{
    public Guid GameId { get; }

    public GameNotFoundException(Guid gameId)
        : base($"Game with ID '{gameId}' was not found.")
    {
        GameId = gameId;
    }
}
