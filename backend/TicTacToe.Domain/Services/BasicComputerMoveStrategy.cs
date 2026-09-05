namespace TicTacToe.Domain.Services;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Deterministic Computer move strategy for Player O implementing the frozen 5-tier priority hierarchy:
/// 1. Winning move for Player O (deterministic lowest index if multiple).
/// 2. Block winning move for Player X (deterministic lowest threatened index if multiple).
/// 3. Center cell (4).
/// 4. Corner cells in deterministic order: 0, 2, 6, 8.
/// 5. First available cell in natural order: 0..8.
/// 
/// Note on forks: If multiple immediate X-winning cells exist, the deterministic strategy selects the
/// lowest-index threatened cell. The strategy does not claim to solve unavoidable fork positions
/// where one block cannot prevent another winning threat.
/// </summary>
public sealed class BasicComputerMoveStrategy : IComputerMoveStrategy
{
    private static readonly int[] CornerOrder = { 0, 2, 6, 8 };

    /// <summary>
    /// Inspects the current board and selects the next legal move for Computer (Player O).
    /// Pure candidate simulation: uses detached cloned boards and delegates evaluation to WinDetector.CheckWin.
    /// Does not mutate the provided board or any external state.
    /// </summary>
    public CellIndex SelectMove(Board board)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        // Priority 1: Winning move for Player O
        for (int i = 0; i < Board.CellCount; i++)
        {
            var cell = new CellIndex(i);
            if (!board.IsOccupied(cell))
            {
                var simulated = board.Clone();
                simulated.PlaceMark(cell, Player.O);
                var winResult = WinDetector.CheckWin(simulated);
                if (winResult.IsWin && winResult.Winner == Player.O)
                {
                    return cell;
                }
            }
        }

        // Priority 2: Block winning move for Player X
        for (int i = 0; i < Board.CellCount; i++)
        {
            var cell = new CellIndex(i);
            if (!board.IsOccupied(cell))
            {
                var simulated = board.Clone();
                simulated.PlaceMark(cell, Player.X);
                var winResult = WinDetector.CheckWin(simulated);
                if (winResult.IsWin && winResult.Winner == Player.X)
                {
                    return cell;
                }
            }
        }

        // Priority 3: Center cell (4)
        var center = new CellIndex(4);
        if (!board.IsOccupied(center))
        {
            return center;
        }

        // Priority 4: Corners (0, 2, 6, 8)
        for (int i = 0; i < CornerOrder.Length; i++)
        {
            var corner = new CellIndex(CornerOrder[i]);
            if (!board.IsOccupied(corner))
            {
                return corner;
            }
        }

        // Priority 5: First available cell in 0..8
        for (int i = 0; i < Board.CellCount; i++)
        {
            var cell = new CellIndex(i);
            if (!board.IsOccupied(cell))
            {
                return cell;
            }
        }

        throw new InvalidOperationException("No legal moves available on the board.");
    }
}
