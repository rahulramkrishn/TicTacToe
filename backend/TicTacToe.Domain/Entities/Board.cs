namespace TicTacToe.Domain.Entities;

using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Domain entity representing the 3x3 (9-cell) game board.
/// Invariant: Exactly 9 cells indexed 0..8. Occupied cells cannot be overwritten.
/// </summary>
public sealed class Board
{
    public const int CellCount = 9;

    private readonly Player?[] _cells;

    public Board()
    {
        _cells = new Player?[CellCount];
    }

    public Board(IReadOnlyList<Player?> initialCells)
    {
        if (initialCells == null)
        {
            throw new ArgumentNullException(nameof(initialCells));
        }

        if (initialCells.Count != CellCount)
        {
            throw new ArgumentException($"Board must contain exactly {CellCount} cells.", nameof(initialCells));
        }

        _cells = new Player?[CellCount];
        for (int i = 0; i < CellCount; i++)
        {
            _cells[i] = initialCells[i];
        }
    }

    /// <summary>
    /// Returns an immutable view of the 9 board cells.
    /// </summary>
    public IReadOnlyList<Player?> Cells => Array.AsReadOnly(_cells);

    /// <summary>
    /// Gets the player occupying the specified cell, or null if empty.
    /// </summary>
    public Player? GetCell(CellIndex index) => _cells[index.Value];

    /// <summary>
    /// Indicates whether the specified cell is occupied.
    /// </summary>
    public bool IsOccupied(CellIndex index) => _cells[index.Value].HasValue;

    /// <summary>
    /// Places a player's mark on an empty cell.
    /// Invariant: Occupied cells cannot be overwritten.
    /// </summary>
    public void PlaceMark(CellIndex index, Player player)
    {
        if (IsOccupied(index))
        {
            throw new CellOccupiedException(index.Value);
        }

        _cells[index.Value] = player;
    }

    /// <summary>
    /// Creates a deep copy of the board.
    /// </summary>
    public Board Clone() => new(_cells);
}
