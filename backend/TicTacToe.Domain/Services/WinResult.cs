namespace TicTacToe.Domain.Services;

using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Immutable value type representing the outcome of evaluating a board for a win.
/// </summary>
public sealed record WinResult(bool IsWin, Player? Winner, IReadOnlyList<int> WinningCells)
{
    /// <summary>
    /// Represents no winning condition on the board.
    /// </summary>
    public static readonly WinResult None = new(false, null, Array.Empty<int>());
}
