namespace TicTacToe.Domain.Services;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Strategy pattern interface defining move selection for the automated Computer player (Player O).
/// Implementations must be pure and deterministic, evaluating board state without mutating aggregate or board instances.
/// </summary>
public interface IComputerMoveStrategy
{
    /// <summary>
    /// Inspects the current board and selects the next legal move for Computer (Player O).
    /// Invariants:
    /// 1. Must be strictly deterministic.
    /// 2. Must never return an occupied cell.
    /// 3. Must return a valid CellIndex in 0..8.
    /// 4. Must not mutate the provided board or any aggregate state.
    /// 5. Throws InvalidOperationException if no legal moves exist.
    /// </summary>
    /// <param name="board">The current game board.</param>
    /// <returns>The chosen CellIndex for Player O.</returns>
    CellIndex SelectMove(Board board);
}
