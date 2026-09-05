namespace TicTacToe.Domain.Exceptions;

using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Thrown when a player attempts to make a move when it is not their turn.
/// </summary>
public sealed class InvalidTurnException : DomainException
{
    public Player AttemptedPlayer { get; }
    public Player ExpectedPlayer { get; }

    public InvalidTurnException(Player attemptedPlayer, Player expectedPlayer)
        : base($"It is player {expectedPlayer}'s turn. Player {attemptedPlayer} cannot move.")
    {
        AttemptedPlayer = attemptedPlayer;
        ExpectedPlayer = expectedPlayer;
    }
}
