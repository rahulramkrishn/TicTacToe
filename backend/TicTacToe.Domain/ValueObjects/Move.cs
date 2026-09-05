namespace TicTacToe.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single played move in a game session.
/// Invariant: MoveNumber &gt;= 1.
/// </summary>
public sealed record Move
{
    public int MoveNumber { get; }
    public Player Player { get; }
    public CellIndex CellIndex { get; }

    public Move(int moveNumber, Player player, CellIndex cellIndex)
    {
        if (moveNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveNumber), moveNumber, "MoveNumber must be greater than or equal to 1.");
        }

        MoveNumber = moveNumber;
        Player = player;
        CellIndex = cellIndex;
    }
}
