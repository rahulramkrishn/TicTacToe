namespace TicTacToe.Domain.Exceptions;

/// <summary>
/// Thrown when a cell index is provided outside the canonical 0..8 range.
/// </summary>
public sealed class InvalidCellIndexException : DomainException
{
    public int AttemptedIndex { get; }

    public InvalidCellIndexException(int attemptedIndex)
        : base($"Cell index {attemptedIndex} is invalid. Canonical board indices must be between 0 and 8 inclusive.")
    {
        AttemptedIndex = attemptedIndex;
    }
}
