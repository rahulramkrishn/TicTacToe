namespace TicTacToe.Domain.Aggregates;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.Mementos;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Aggregate Root representing a Tic Tac Toe game session.
/// Enforces all business invariants regarding game lifecycle, turn alternation, move legality, win detection, draw detection, and Memento-based undo.
/// Acts as the Memento Originator and internal Caretaker in the GoF Memento Pattern.
/// </summary>
public sealed class Game
{
    private readonly List<Move> _moveHistory;
    private readonly Stack<GameMemento> _undoStack;
    private readonly List<IDomainEvent> _domainEvents;

    public GameId Id { get; }
    public GameMode Mode { get; }
    public GameStatus Status { get; private set; }
    public Player CurrentPlayer { get; private set; }
    public Player? Winner { get; private set; }
    public IReadOnlyList<int> WinningCells { get; private set; }
    public Board Board { get; private set; }

    public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Indicates whether an Undo operation can be performed.
    /// Invariant (Option A per ADR-004): True only when the game is InProgress and at least one snapshot exists.
    /// </summary>
    public bool CanUndo => Status == GameStatus.InProgress && _undoStack.Count > 0;

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
        _undoStack = new Stack<GameMemento>();
        _domainEvents = new List<IDomainEvent>();
    }

    /// <summary>
    /// Clears all pending domain events after successful dispatch by the application layer.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Resets the game play state for a new round while preserving the GameId (FR-12).
    /// Invariant (P008.2): MUST NOT clear DomainEvents. Pending events are retained until successfully dispatched and cleared by the application layer.
    /// </summary>
    public void Reset()
    {
        Board = new Board();
        CurrentPlayer = Player.X;
        Status = GameStatus.InProgress;
        Winner = null;
        WinningCells = Array.Empty<int>();
        _moveHistory.Clear();
        _undoStack.Clear();
        // NOTE: _domainEvents is intentionally NOT cleared here per P008.2 event lifecycle specification.
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
    /// 4. Memento snapshot is captured and pushed to the undo stack before mutation.
    /// 5. Move is placed on the board and appended to history.
    /// 6. Win detection is evaluated first against the 8 canonical lines.
    /// 7. If won: status transitions to Won, Winner and WinningCells are recorded, GameCompletedEvent is emitted, and turn progression halts.
    /// 8. If not won and board is full: status transitions to Draw, GameCompletedEvent is emitted, and turn progression halts.
    /// 9. If non-terminal: turn alternates to the other player.
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

        if (Board.IsOccupied(cellIndex))
        {
            throw new CellOccupiedException(cellIndex.Value);
        }

        // Logical boundary: TwoPlayer captures every move; Computer mode captures before the human (X) move
        if (Mode == GameMode.TwoPlayer || player == Player.X)
        {
            _undoStack.Push(CreateMemento());
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

            _domainEvents.Add(new GameCompletedEvent(
                Guid.NewGuid(),
                Id,
                GameStatus.Won,
                Winner,
                WinningCells,
                _moveHistory.Count,
                DateTimeOffset.UtcNow));
            return;
        }

        if (Board.IsFull)
        {
            Status = GameStatus.Draw;

            _domainEvents.Add(new GameCompletedEvent(
                Guid.NewGuid(),
                Id,
                GameStatus.Draw,
                null,
                Array.Empty<int>(),
                _moveHistory.Count,
                DateTimeOffset.UtcNow));
            return;
        }

        CurrentPlayer = CurrentPlayer.Other();
    }

    /// <summary>
    /// Executes a computer move using the provided strategy.
    /// Invariants enforced:
    /// 1. Strategy must not be null (ArgumentNullException).
    /// 2. Mode must be Computer (InvalidOperationException).
    /// 3. Game must be InProgress (GameAlreadyCompletedException).
    /// 4. Current player must be Player O (InvalidTurnException).
    /// </summary>
    public CellIndex PlayComputerMove(IComputerMoveStrategy strategy)
    {
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        if (Mode != GameMode.Computer)
        {
            throw new InvalidOperationException("Cannot execute a computer move in TwoPlayer mode.");
        }

        if (Status != GameStatus.InProgress)
        {
            throw new GameAlreadyCompletedException(Status);
        }

        if (CurrentPlayer != Player.O)
        {
            throw new InvalidTurnException(Player.O, CurrentPlayer);
        }

        var cell = strategy.SelectMove(Board);
        MakeMove(Player.O, cell);
        return cell;
    }

    /// <summary>
    /// Orchestrates a logical turn in Computer Mode:
    /// 1. Executes the human X move.
    /// 2. If the human move concludes the game (Win or Draw), halts immediately without invoking the computer.
    /// 3. If the game remains InProgress, invokes the computer strategy to execute Player O's response.
    /// Returns the executed human move and optional computer move (null if game ended on the human move).
    /// </summary>
    public (CellIndex HumanMove, CellIndex? ComputerMove) ExecuteTurn(CellIndex humanMove, IComputerMoveStrategy strategy)
    {
        MakeMove(Player.X, humanMove);

        if (Status != GameStatus.InProgress)
        {
            return (humanMove, null);
        }

        var computerMove = PlayComputerMove(strategy);
        return (humanMove, computerMove);
    }

    /// <summary>
    /// Restores the aggregate state to the exact state immediately preceding the last logical move boundary.
    /// Invariants enforced:
    /// 1. Undo is disabled after game completion under Option A (ADR-004).
    /// 2. Undo is disabled when move history is empty.
    /// 3. Restored state completely replaces active board, turn, history, and status without heuristic recalculation.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo)
        {
            if (Status != GameStatus.InProgress)
            {
                throw new CannotUndoException($"Cannot undo a completed game with status {Status}.");
            }

            throw new CannotUndoException("Cannot undo: move history is empty.");
        }

        var memento = _undoStack.Pop();
        RestoreFromMemento(memento);
    }

    private GameMemento CreateMemento()
    {
        return new GameMemento(
            Board.Clone(),
            CurrentPlayer,
            Status,
            Winner,
            Array.AsReadOnly(WinningCells.ToArray()),
            Array.AsReadOnly(_moveHistory.ToArray()));
    }

    private void RestoreFromMemento(GameMemento memento)
    {
        Board = memento.BoardSnapshot.Clone();
        CurrentPlayer = memento.CurrentPlayer;
        Status = memento.Status;
        Winner = memento.Winner;
        WinningCells = memento.WinningCells;
        _moveHistory.Clear();
        _moveHistory.AddRange(memento.MoveHistory);
    }
}
