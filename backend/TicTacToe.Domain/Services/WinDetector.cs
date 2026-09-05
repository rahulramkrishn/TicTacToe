namespace TicTacToe.Domain.Services;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Stateless domain service evaluating board win conditions against the 8 canonical winning lines.
/// </summary>
public static class WinDetector
{
    /// <summary>
    /// The 8 canonical winning lines on a 3x3 grid (3 rows, 3 columns, 2 diagonals).
    /// Private to prevent external mutation of winning rule definitions.
    /// </summary>
    private static readonly int[][] WinningLines =
    {
        new[] { 0, 1, 2 }, // Row 0
        new[] { 3, 4, 5 }, // Row 1
        new[] { 6, 7, 8 }, // Row 2
        new[] { 0, 3, 6 }, // Column 0
        new[] { 1, 4, 7 }, // Column 1
        new[] { 2, 5, 8 }, // Column 2
        new[] { 0, 4, 8 }, // Main diagonal
        new[] { 2, 4, 6 }  // Anti diagonal
    };

    /// <summary>
    /// Evaluates the board cells to determine if any player has completed a winning line.
    /// Returns WinResult with winner and an immutable view of the exact 3 winning cell indices, or WinResult.None.
    /// </summary>
    public static WinResult CheckWin(Board board)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        var cells = board.Cells;

        for (int i = 0; i < WinningLines.Length; i++)
        {
            var line = WinningLines[i];
            var first = cells[line[0]];

            if (first.HasValue && first == cells[line[1]] && first == cells[line[2]])
            {
                return new WinResult(true, first.Value, Array.AsReadOnly((int[])line.Clone()));
            }
        }

        return WinResult.None;
    }
}
