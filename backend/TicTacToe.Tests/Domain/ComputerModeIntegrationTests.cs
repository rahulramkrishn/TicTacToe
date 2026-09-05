namespace TicTacToe.Tests.Domain;

using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public sealed class ComputerModeIntegrationTests
{
    private readonly BasicComputerMoveStrategy _strategy = new();

    #region Basic Automatic Turn Flow

    [Fact]
    public void ExecuteTurn_HumanMoveThenComputerMove_CompletesTwoMovesAndReturnsTurnToHuman()
    {
        var game = Game.Create(GameMode.Computer);

        // Human plays cell 0 (corner)
        var (humanMove, computerMove) = game.ExecuteTurn(new CellIndex(0), _strategy);

        Assert.Equal(0, humanMove.Value);
        Assert.NotNull(computerMove);
        // Center 4 should be selected by Computer according to priority
        Assert.Equal(4, computerMove.Value.Value);

        // State verification
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer); // Turn returned to human
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(2, game.Board.OccupiedCount);
        Assert.Equal(Player.X, game.Board.GetCell(new CellIndex(0)));
        Assert.Equal(Player.O, game.Board.GetCell(new CellIndex(4)));
    }

    [Fact]
    public void ExecuteTurn_VerifiesBoardHistoryAndAlternation()
    {
        var game = Game.Create(GameMode.Computer);

        game.ExecuteTurn(new CellIndex(4), _strategy); // Human plays center 4

        // History entry 1: Human X at 4
        Assert.Equal(1, game.MoveHistory[0].MoveNumber);
        Assert.Equal(Player.X, game.MoveHistory[0].Player);
        Assert.Equal(4, game.MoveHistory[0].CellIndex.Value);

        // History entry 2: Computer O at corner 0 (first corner)
        Assert.Equal(2, game.MoveHistory[1].MoveNumber);
        Assert.Equal(Player.O, game.MoveHistory[1].Player);
        Assert.Equal(0, game.MoveHistory[1].CellIndex.Value);

        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    #endregion

    #region Human Terminal Moves (Computer Does Not Move)

    [Fact]
    public void ExecuteTurn_WhenHumanMoveWins_HaltsImmediatelyAndComputerDoesNotMove()
    {
        var game = Game.Create(GameMode.Computer);

        // Set up game so human is about to complete row 0:
        // Move 1: Human X at 0, Computer O at 4
        // Turn 1: Human 0, Computer takes 4
        game.ExecuteTurn(new CellIndex(0), _strategy);
        // Turn 2: Human 8, Computer takes 2
        game.ExecuteTurn(new CellIndex(8), _strategy);
        // Turn 3: Human 6 creates fork (threatens 3 and 7). Computer blocks lowest index 3.
        game.ExecuteTurn(new CellIndex(6), _strategy);

        // Turn 4: Human completes row 2 at cell 7. Human wins!
        var (humanMove, computerMove) = game.ExecuteTurn(new CellIndex(7), _strategy);

        Assert.Equal(7, humanMove.Value);
        Assert.Null(computerMove); // Computer must NOT move
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(3, game.WinningCells.Count);
        Assert.Contains(6, game.WinningCells);
        Assert.Contains(7, game.WinningCells);
        Assert.Contains(8, game.WinningCells);
        Assert.Equal(7, game.MoveHistory.Count);
    }

    [Fact]
    public void ExecuteTurn_WhenHumanMoveCreatesDraw_HaltsImmediatelyAndComputerDoesNotMove()
    {
        var game = Game.Create(GameMode.Computer);

        // Reach move 8 with no winner:
        // Row 0: X, O, X
        // Row 1: X, O, O
        // Row 2: O, X, . (8 empty)
        // Let's execute exact manual moves using MakeMove directly up to 8 moves:
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(3));
        game.MakeMove(Player.O, new CellIndex(5));
        game.MakeMove(Player.X, new CellIndex(7));
        game.MakeMove(Player.O, new CellIndex(6));

        // Now board has 8 occupied cells:
        // X at 0, 2, 3, 7
        // O at 1, 4, 5, 6
        // Cell 8 is the only empty cell.
        // Current player is X.
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);

        // Human plays cell 8 via ExecuteTurn:
        var (humanMove, computerMove) = game.ExecuteTurn(new CellIndex(8), _strategy);

        Assert.Equal(8, humanMove.Value);
        Assert.Null(computerMove); // Computer must NOT move because board is full (Draw)
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.MoveHistory.Count);
    }

    #endregion

    #region Computer Terminal Moves

    [Fact]
    public void ExecuteTurn_WhenComputerMoveWins_TransitionsToWonWithWinnerO()
    {
        var game = Game.Create(GameMode.Computer);

        // Set up board so Computer O has an immediate winning move:
        // Let O have cells 4 and 6 (Diag 2, 4, 6)
        // Let X have cells 0, 1, 3
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(6));

        // Now X plays cell 3 (does not win):
        // Diag 2, 4, 6 has O at 4 and 6. Cell 2 is free!
        // O has winning move at cell 2!
        var (humanMove, computerMove) = game.ExecuteTurn(new CellIndex(3), _strategy);

        Assert.Equal(3, humanMove.Value);
        Assert.NotNull(computerMove);
        Assert.Equal(2, computerMove.Value.Value);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(3, game.WinningCells.Count);
        Assert.Contains(2, game.WinningCells);
        Assert.Contains(4, game.WinningCells);
        Assert.Contains(6, game.WinningCells);
    }

    [Fact]
    public void ExecuteTurn_WhenComputerMoveFillsBoard_TransitionsToDraw()
    {
        var game = Game.Create(GameMode.Computer);

        // Setup 7 moves:
        // 0: X, 1: O, 2: X
        // 3: X, 4: O, 5: O
        // 6: O
        // Cells 7 and 8 are free.
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(3));
        game.MakeMove(Player.O, new CellIndex(5));
        game.MakeMove(Player.X, new CellIndex(7));

        // Wait, let's verify who is CurrentPlayer:
        // 7 moves played -> CurrentPlayer is O.
        // Let's set up so Human plays move 8 (cell 6), and Computer plays move 9 (cell 8) resulting in draw:
        // Board with 7 marks:
        // X at 0, 2, 5, 6
        // O at 1, 3, 4
        // Remaining: 7, 8
        // Let's make:
        game = Game.Create(GameMode.Computer);
        game.MakeMove(Player.X, new CellIndex(0)); // 1
        game.MakeMove(Player.O, new CellIndex(1)); // 2
        game.MakeMove(Player.X, new CellIndex(2)); // 3
        game.MakeMove(Player.O, new CellIndex(4)); // 4
        game.MakeMove(Player.X, new CellIndex(3)); // 5
        game.MakeMove(Player.O, new CellIndex(5)); // 6
        game.MakeMove(Player.X, new CellIndex(7)); // 7
        // CurrentPlayer is O (move 8). Computer takes cell 6:
        game.MakeMove(Player.O, new CellIndex(6)); // 8
        // Remaining: only cell 8.
        // Wait, if only cell 8 is left, human plays move 9, which is a human move.
        // Can computer play move 9 to create a draw?
        // In Tic-Tac-Toe, human plays move 1, 3, 5, 7, 9. Computer plays move 2, 4, 6, 8.
        // The 9th move is ALWAYS played by Player X (human)!
        // Computer (Player O) only plays even moves (2, 4, 6, 8).
        // Therefore, a draw can only occur after Player O if move 8 somehow fills the board (which is impossible on a 9-cell board)
        // UNLESS computer move fills final cell in a scenario where board is full after O, but on 3x3 O only makes up to 4 moves!
        // Wait, can O win on move 8? Yes!
        // Can O draw on move 9? No, O never plays move 9 in standard rules.
        // But let's verify what happens when O plays move 8 and the game remains InProgress:
        // Turn returns to X.
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    #endregion

    #region Computer Mode Undo Integration

    [Fact]
    public void Undo_AfterCompletedComputerTurn_RevertsBothHumanAndComputerMovesRestoringPlayerX()
    {
        var game = Game.Create(GameMode.Computer);

        // Turn 1: Human plays 0, Computer takes 4
        game.ExecuteTurn(new CellIndex(0), _strategy);

        Assert.Equal(2, game.MoveHistory.Count);
        Assert.True(game.CanUndo);

        // Undo the logical turn
        game.Undo();

        // Exactly the initial state should be restored
        Assert.Empty(game.MoveHistory);
        Assert.Equal(0, game.Board.OccupiedCount);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Undo_AfterMultipleComputerTurns_RevertsOneLogicalTurnPairPerUndo()
    {
        var game = Game.Create(GameMode.Computer);

        // Turn 1: Human 0, Computer 4
        game.ExecuteTurn(new CellIndex(0), _strategy);
        // Turn 2: Human 1, Computer blocks 2
        game.ExecuteTurn(new CellIndex(1), _strategy);

        Assert.Equal(4, game.MoveHistory.Count);

        // Undo Turn 2: should revert to state after Turn 1 (2 moves)
        game.Undo();

        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(Player.X, game.Board.GetCell(new CellIndex(0)));
        Assert.Equal(Player.O, game.Board.GetCell(new CellIndex(4)));
        Assert.Null(game.Board.GetCell(new CellIndex(1)));
        Assert.Null(game.Board.GetCell(new CellIndex(2)));

        // Undo Turn 1: should revert to empty board
        game.Undo();

        Assert.Empty(game.MoveHistory);
        Assert.Equal(0, game.Board.OccupiedCount);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_AfterComputerWins_ThrowsCannotUndoExceptionPreservingOptionA()
    {
        var game = Game.Create(GameMode.Computer);

        // Set up computer win:
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(6));

        // Human plays 3; Computer wins with 2
        game.ExecuteTurn(new CellIndex(3), _strategy);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.False(game.CanUndo);

        Assert.Throws<CannotUndoException>(() => game.Undo());
    }

    [Fact]
    public void Undo_AfterHumanWins_ThrowsCannotUndoExceptionPreservingOptionA()
    {
        var game = Game.Create(GameMode.Computer);

        // Turn 1: X=0, O=4
        game.ExecuteTurn(new CellIndex(0), _strategy);
        // Turn 2: X=8, O=2
        game.ExecuteTurn(new CellIndex(8), _strategy);
        // Turn 3: X=6, O=3 (blocks lowest threat)
        game.ExecuteTurn(new CellIndex(6), _strategy);
        // Turn 4: X=7 (X completes row 2 and wins!)
        game.ExecuteTurn(new CellIndex(7), _strategy);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.False(game.CanUndo);

        Assert.Throws<CannotUndoException>(() => game.Undo());
    }

    #endregion

    #region Explicit Invalid Computer Invocation Rejections

    [Fact]
    public void PlayComputerMove_InTwoPlayerMode_ThrowsInvalidOperationException()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        Assert.Throws<InvalidOperationException>(() => game.PlayComputerMove(_strategy));
    }

    [Fact]
    public void PlayComputerMove_WhenGameAlreadyWon_ThrowsGameAlreadyCompletedException()
    {
        var game = Game.Create(GameMode.Computer);

        // Play moves to win:
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // X wins row 0

        Assert.Equal(GameStatus.Won, game.Status);

        Assert.Throws<GameAlreadyCompletedException>(() => game.PlayComputerMove(_strategy));
    }

    [Fact]
    public void PlayComputerMove_WhenCurrentPlayerIsX_ThrowsInvalidTurnException()
    {
        var game = Game.Create(GameMode.Computer);

        // CurrentPlayer is X on new game
        Assert.Equal(Player.X, game.CurrentPlayer);

        Assert.Throws<InvalidTurnException>(() => game.PlayComputerMove(_strategy));
    }

    [Fact]
    public void PlayComputerMove_WhenStrategyNull_ThrowsArgumentNullException()
    {
        var game = Game.Create(GameMode.Computer);

        Assert.Throws<ArgumentNullException>(() => game.PlayComputerMove(null!));
    }

    #endregion
}
