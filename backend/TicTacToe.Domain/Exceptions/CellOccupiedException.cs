namespace TicTacToe.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to play a move on a cell that is already occupied.
/// </summary>
public sealed class CellOccupiedException : DomainException
{
    public int CellIndex { get; }

    public CellOccupiedException(int cellIndex)
        : base($"Cell {cellIndex} is already occupied. Moves can only be played on empty cells.")
    {
        CellIndex = cellIndex;
    }
}
