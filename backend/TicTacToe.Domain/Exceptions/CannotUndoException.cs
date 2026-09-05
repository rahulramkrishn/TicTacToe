namespace TicTacToe.Domain.Exceptions;

/// <summary>
/// Thrown when an undo operation cannot be performed (e.g. no moves played or game already completed under Option A).
/// </summary>
public sealed class CannotUndoException : DomainException
{
    public CannotUndoException(string message) : base(message)
    {
    }

    public CannotUndoException() : base("Cannot undo: move history is empty or game has completed.")
    {
    }
}
