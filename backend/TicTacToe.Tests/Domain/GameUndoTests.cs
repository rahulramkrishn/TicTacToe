using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class GameUndoTests
{
    [Fact]
    public void CanUndo_OnNewGame_ReturnsFalse()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Assert
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Undo_OnNewGame_ThrowsCannotUndoExceptionAndPreservesState()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Act & Assert
        var ex = Assert.Throws<CannotUndoException>(() => game.Undo());
        Assert.Contains("move history is empty", ex.Message);

        // Verify initial state is intact
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.MoveHistory);
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
    }

    [Fact]
    public void Undo_AfterSingleMove_RestoresInitialEmptyBoardAndPlayerX()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(4));
        Assert.True(game.CanUndo);

        // Act
        game.Undo();

        // Assert
        Assert.False(game.CanUndo);
        Assert.Empty(game.MoveHistory);
        Assert.Null(game.Board.GetCell(new CellIndex(4)));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(0, game.Board.OccupiedCount);
    }

    [Fact]
    public void Undo_AfterTwoMoves_RestoresFirstMoveAndPlayerOTurn()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0)); // Move 1
        game.MakeMove(Player.O, new CellIndex(1)); // Move 2

        // Act
        game.Undo();

        // Assert
        Assert.True(game.CanUndo);
        Assert.Single(game.MoveHistory);
        Assert.Equal(1, game.MoveHistory[0].MoveNumber);
        Assert.Equal(Player.X, game.MoveHistory[0].Player);
        Assert.Equal(new CellIndex(0), game.MoveHistory[0].CellIndex);

        Assert.Equal(Player.X, game.Board.GetCell(new CellIndex(0)));
        Assert.Null(game.Board.GetCell(new CellIndex(1)));
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void Undo_MultipleSequentialUndos_RestoresPriorStatesSequentiallyUntilExhaustion()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0)); // Move 1
        game.MakeMove(Player.O, new CellIndex(1)); // Move 2
        game.MakeMove(Player.X, new CellIndex(2)); // Move 3

        // Act 1: Undo move 3
        game.Undo();
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Board.GetCell(new CellIndex(2)));
        Assert.True(game.CanUndo);

        // Act 2: Undo move 2
        game.Undo();
        Assert.Single(game.MoveHistory);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Null(game.Board.GetCell(new CellIndex(1)));
        Assert.True(game.CanUndo);

        // Act 3: Undo move 1
        game.Undo();
        Assert.Empty(game.MoveHistory);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Board.GetCell(new CellIndex(0)));
        Assert.False(game.CanUndo);

        // Act 4: Attempt undo when exhausted
        Assert.Throws<CannotUndoException>(() => game.Undo());
    }

    [Fact]
    public void Undo_FollowedByNewMove_EstablishesNewBranchCorrectly()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));

        // Undo move 2 (O at 1)
        game.Undo();
        Assert.Equal(Player.O, game.CurrentPlayer);

        // Act: Play a different move for O at cell 4
        game.MakeMove(Player.O, new CellIndex(4));

        // Assert: New branch has O at 4, cell 1 is empty
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(Player.O, game.MoveHistory[1].Player);
        Assert.Equal(new CellIndex(4), game.MoveHistory[1].CellIndex);
        Assert.Null(game.Board.GetCell(new CellIndex(1)));
        Assert.Equal(Player.O, game.Board.GetCell(new CellIndex(4)));
        Assert.Equal(Player.X, game.CurrentPlayer);

        // Further undo reverts O at 4
        game.Undo();
        Assert.Single(game.MoveHistory);
        Assert.Null(game.Board.GetCell(new CellIndex(4)));
        Assert.Equal(Player.O, game.CurrentPlayer);
    }

    [Fact]
    public void MakeMove_WhenRejected_DoesNotCreateMementoOrAlterUndoStack()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        Assert.True(game.CanUndo);

        // Act 1: Invalid move - wrong player
        Assert.Throws<InvalidTurnException>(() => game.MakeMove(Player.X, new CellIndex(1)));
        Assert.Single(game.MoveHistory);

        // Act 2: Invalid move - occupied cell
        Assert.Throws<CellOccupiedException>(() => game.MakeMove(Player.O, new CellIndex(0)));
        Assert.Single(game.MoveHistory);

        // Undo should cleanly revert move 1
        game.Undo();
        Assert.Empty(game.MoveHistory);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Undo_AfterGameWon_ThrowsCannotUndoExceptionAndPreservesTerminalState()
    {
        // Arrange: Row win for X (0, 1, 2)
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // X wins!

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);

        // Option A check: CanUndo must be false after completion
        Assert.False(game.CanUndo);

        // Act & Assert: Undo must be rejected
        var ex = Assert.Throws<CannotUndoException>(() => game.Undo());
        Assert.Contains("completed game", ex.Message);

        // Verify state remains terminal and intact
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
        Assert.Equal(5, game.MoveHistory.Count);
    }

    [Fact]
    public void Undo_AfterGameDraw_ThrowsCannotUndoExceptionAndPreservesTerminalState()
    {
        // Arrange: Full board draw game
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(4));
        game.MakeMove(Player.O, new CellIndex(6));
        game.MakeMove(Player.X, new CellIndex(5));
        game.MakeMove(Player.O, new CellIndex(8));
        game.MakeMove(Player.X, new CellIndex(7)); // Draw!

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.False(game.CanUndo);

        // Act & Assert
        var ex = Assert.Throws<CannotUndoException>(() => game.Undo());
        Assert.Contains("completed game", ex.Message);
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Equal(9, game.MoveHistory.Count);
    }

    [Fact]
    public void Undo_MaintainsMoveHistoryCountEqualsBoardOccupiedCountInvariant()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));

        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);

        // Act & Assert at each undo step
        game.Undo();
        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);
        Assert.Equal(2, game.MoveHistory.Count);

        game.Undo();
        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);
        Assert.Single(game.MoveHistory);

        game.Undo();
        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);
        Assert.Empty(game.MoveHistory);
    }

    [Fact]
    public void Memento_DefensivelyIsolatesStateFromExternalMutations()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));

        // Get board reference and verify that changing a cloned board cannot affect future undos
        var externalBoard = game.Board.Clone();
        externalBoard.PlaceMark(new CellIndex(8), Player.O);

        // Play move 2 and undo
        game.MakeMove(Player.O, new CellIndex(1));
        game.Undo();

        // State restored from memento should have only X at 0, not O at 8
        Assert.Equal(Player.X, game.Board.GetCell(new CellIndex(0)));
        Assert.Null(game.Board.GetCell(new CellIndex(8)));
        Assert.Null(game.Board.GetCell(new CellIndex(1)));
    }

    [Fact]
    public void Undo_InComputerMode_RevertsHumanAndComputerPair()
    {
        // Arrange: Game in Computer Mode
        var game = Game.Create(GameMode.Computer);

        // Move 1: Human X plays at 0 (starts pair -> snapshot captured)
        game.MakeMove(Player.X, new CellIndex(0));

        // Move 2: Computer O plays at 4 (finishes pair -> no second snapshot pushed)
        game.MakeMove(Player.O, new CellIndex(4));

        Assert.Equal(2, game.MoveHistory.Count);
        Assert.True(game.CanUndo);

        // Act: Undo in Computer mode reverts the human/computer pair back to initial state
        game.Undo();

        // Assert: Restored state is completely empty with Player X's turn!
        Assert.Empty(game.MoveHistory);
        Assert.Null(game.Board.GetCell(new CellIndex(0)));
        Assert.Null(game.Board.GetCell(new CellIndex(4)));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.False(game.CanUndo);
    }
}
