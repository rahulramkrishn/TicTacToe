namespace TicTacToe.Domain.Aggregates;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Aggregate Root representing a Tic Tac Toe game session.
/// Enforces all business invariants regarding game lifecycle, turn alternation, move legality, win detection, and draw detection.
/// </summary>
public sealed class Game
{
    private readonly List<Move> _moveHistory;

    public GameId Id { get; }
    public GameMode Mode { get; }
    public GameStatus Status { get; private set; }
    public Player CurrentPlayer { get; private set; }
    public Player? Winner { get; private set; }
    public IReadOnlyList<int> WinningCells { get; private set; }
    public Board Board { get; }

    public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();

    public Game(GameId id, GameMode mode)
    {
        Id = id;
        Mode = mode;
        Status = GameStatus.InProgress;
        CurrentPlayer = Player.X;
        Winner = null;
        WinningCells = Array.Empty<int>();
        Board = new Board();
        _moveHistory = new List<Move>();
    }

    /// <summary>
    /// Factory method to create a fresh game session.
    /// Invariant: Starts in InProgress status with Player X.
    /// </summary>
    public static Game Create(GameMode mode) => new(GameId.New(), mode);

    /// <summary>
    /// Executes a player move within the aggregate boundary.
    /// Invariants enforced:
    /// 1. Game must be InProgress.
    /// 2. Player must match CurrentPlayer.
    /// 3. Cell must be unoccupied.
    /// 4. Move is appended to history.
    /// 5. Win detection is evaluated first against the 8 canonical lines.
    /// 6. If won: status transitions to Won, Winner and WinningCells are recorded, and turn progression halts.
    /// 7. If not won and board is full: status transitions to Draw, and turn progression halts.
    /// 8. If non-terminal: turn alternates to the other player.
    /// </summary>
    public void MakeMove(Player player, CellIndex cellIndex)
    {
        if (Status != GameStatus.InProgress)
        {
            throw new GameAlreadyCompletedException(Status);
        }

        if (player != CurrentPlayer)
        {
            throw new InvalidTurnException(player, CurrentPlayer);
        }

        Board.PlaceMark(cellIndex, player);

        var moveNumber = _moveHistory.Count + 1;
        _moveHistory.Add(new Move(moveNumber, player, cellIndex));

        var winResult = WinDetector.CheckWin(Board);
        if (winResult.IsWin)
        {
            Status = GameStatus.Won;
            Winner = winResult.Winner;
            WinningCells = winResult.WinningCells;
            return;
        }

        if (Board.IsFull)
        {
            Status = GameStatus.Draw;
            return;
        }

        CurrentPlayer = CurrentPlayer.Other();
    }
}
