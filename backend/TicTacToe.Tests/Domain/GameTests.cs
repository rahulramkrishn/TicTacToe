using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class GameTests
{
    [Fact]
    public void Create_InitializesGameInStandardInitialState()
    {
        // Act
        var game = Game.Create(GameMode.TwoPlayer);

        // Assert
        Assert.NotEqual(Guid.Empty, game.Id.Value);
        Assert.Equal(GameMode.TwoPlayer, game.Mode);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Empty(game.MoveHistory);
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
    }

    [Fact]
    public void MakeMove_ValidMove_PlacesMarkAppendsHistoryAndAlternatesTurn()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        var cell0 = new CellIndex(0);

        // Act
        game.MakeMove(Player.X, cell0);

        // Assert
        Assert.Equal(Player.X, game.Board.GetCell(cell0));
        Assert.Single(game.MoveHistory);
        Assert.Equal(1, game.MoveHistory[0].MoveNumber);
        Assert.Equal(Player.X, game.MoveHistory[0].Player);
        Assert.Equal(cell0, game.MoveHistory[0].CellIndex);
        Assert.Equal(Player.O, game.CurrentPlayer);
    }

    [Fact]
    public void MakeMove_SequentialValidMoves_AlternatesPlayerCorrectly()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));

        // Assert
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(Player.X, game.Board.GetCell(new CellIndex(0)));
        Assert.Equal(Player.O, game.Board.GetCell(new CellIndex(1)));
    }

    [Fact]
    public void MakeMove_WrongPlayer_ThrowsInvalidTurnExceptionAndDoesNotMutateState()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Act & Assert: Player O tries to move first
        var ex = Assert.Throws<InvalidTurnException>(() => game.MakeMove(Player.O, new CellIndex(0)));
        Assert.Equal(Player.O, ex.AttemptedPlayer);
        Assert.Equal(Player.X, ex.ExpectedPlayer);

        // Verify all aggregate state facets remain unchanged
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
        Assert.Empty(game.MoveHistory);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
    }

    [Fact]
    public void MakeMove_OccupiedCell_ThrowsCellOccupiedExceptionAndDoesNotMutateState()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        var cellIndex = new CellIndex(4);
        game.MakeMove(Player.X, cellIndex);

        // Act & Assert: Player O tries to play on cell 4
        var ex = Assert.Throws<CellOccupiedException>(() => game.MakeMove(Player.O, cellIndex));
        Assert.Equal(4, ex.CellIndex);

        // Verify all aggregate state facets remain unchanged from post-move-1 state
        Assert.Equal(Player.X, game.Board.GetCell(cellIndex));
        Assert.All(game.Board.Cells.Where((c, idx) => idx != 4), cell => Assert.Null(cell));
        Assert.Single(game.MoveHistory);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
    }

    [Theory]
    [InlineData(GameStatus.Won)]
    [InlineData(GameStatus.Draw)]
    public void MakeMove_WhenGameAlreadyCompleted_ThrowsGameAlreadyCompletedExceptionAndPreservesState(GameStatus completedStatus)
    {
        // Arrange: Rehydrate game into terminal state via reflection to verify domain invariant guard
        var game = Game.Create(GameMode.TwoPlayer);
        typeof(Game).GetProperty(nameof(Game.Status))!.SetValue(game, completedStatus);

        // Act & Assert: Player X tries to move on completed game
        var ex = Assert.Throws<GameAlreadyCompletedException>(() => game.MakeMove(Player.X, new CellIndex(0)));
        Assert.Equal(completedStatus, ex.CurrentStatus);

        // Verify all aggregate state facets remain unchanged
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
        Assert.Empty(game.MoveHistory);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(completedStatus, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
    }

    [Fact]
    public void MakeMove_PlayerXWinsRow_TransitionsToWonSetsWinnerAndHaltsTurnProgression()
    {
        // Arrange: Row 0 win (0, 1, 2)
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));

        // Act: Winning move for X
        game.MakeMove(Player.X, new CellIndex(2));

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
        // Turn does not alternate to O upon completion
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(5, game.MoveHistory.Count);
    }

    [Fact]
    public void MakeMove_PlayerOWinsColumn_TransitionsToWonSetsWinnerAndHaltsTurnProgression()
    {
        // Arrange: Column 1 win (1, 4, 7) for Player O
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0)); // X at 0
        game.MakeMove(Player.O, new CellIndex(1)); // O at 1
        game.MakeMove(Player.X, new CellIndex(2)); // X at 2
        game.MakeMove(Player.O, new CellIndex(4)); // O at 4
        game.MakeMove(Player.X, new CellIndex(3)); // X at 3

        // Act: Winning move for O
        game.MakeMove(Player.O, new CellIndex(7)); // O at 7

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(new[] { 1, 4, 7 }, game.WinningCells);
        // Turn does not alternate to X upon completion
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(6, game.MoveHistory.Count);
    }

    [Theory]
    [InlineData(0, 4, 8)] // Main diagonal
    [InlineData(2, 4, 6)] // Anti diagonal
    public void MakeMove_PlayerXWinsDiagonal_TransitionsToWon(int d0, int d1, int d2)
    {
        // Arrange: X plays diagonal, O plays off-diagonal
        var game = Game.Create(GameMode.TwoPlayer);
        int o1 = (d0 == 0) ? 1 : 0;
        int o2 = (d1 == 4) ? 3 : 1;

        game.MakeMove(Player.X, new CellIndex(d0));
        game.MakeMove(Player.O, new CellIndex(o1));
        game.MakeMove(Player.X, new CellIndex(d1));
        game.MakeMove(Player.O, new CellIndex(o2));

        // Act
        game.MakeMove(Player.X, new CellIndex(d2));

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { d0, d1, d2 }, game.WinningCells);
    }

    [Fact]
    public void MakeMove_StandardDraw_TransitionsToDrawWithNullWinnerAndEmptyWinningCells()
    {
        // Arrange: Classic draw game (9 moves, no line completed)
        // X O X
        // O X X
        // O X O
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(4));
        game.MakeMove(Player.O, new CellIndex(6));
        game.MakeMove(Player.X, new CellIndex(5));
        game.MakeMove(Player.O, new CellIndex(8));

        // Act: 9th move filling the board with no winner
        game.MakeMove(Player.X, new CellIndex(7));

        // Assert
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        // Turn does not alternate
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(9, game.MoveHistory.Count);
        Assert.True(game.Board.IsFull);
    }

    [Fact]
    public void MakeMove_FinalMoveCompletesWinningLine_TakesPrecedenceOverDraw()
    {
        // Arrange: Full board where the 9th move completes a win for X on Column 2 (2, 5, 8)
        // Board layout at move 8:
        // 0:X, 1:O, 2:X
        // 3:O, 4:O, 5:X
        // 6:O, 7:X, 8:(empty)
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0)); // 1. X at 0
        game.MakeMove(Player.O, new CellIndex(1)); // 2. O at 1
        game.MakeMove(Player.X, new CellIndex(2)); // 3. X at 2
        game.MakeMove(Player.O, new CellIndex(3)); // 4. O at 3
        game.MakeMove(Player.X, new CellIndex(5)); // 5. X at 5
        game.MakeMove(Player.O, new CellIndex(4)); // 6. O at 4
        game.MakeMove(Player.X, new CellIndex(7)); // 7. X at 7
        game.MakeMove(Player.O, new CellIndex(6)); // 8. O at 6

        // Act: 9th move completes the board AND completes Column 2 (2, 5, 8)
        game.MakeMove(Player.X, new CellIndex(8));

        // Assert: Win MUST take precedence over Draw
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.NotEqual(GameStatus.Draw, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { 2, 5, 8 }, game.WinningCells);
        Assert.True(game.Board.IsFull);
        Assert.Equal(9, game.MoveHistory.Count);
    }

    [Fact]
    public void MakeMove_AfterNaturalWin_RejectsSubsequentMovesAndPreservesState()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // X wins row 0

        var historyCountBefore = game.MoveHistory.Count;

        // Act & Assert: Player O tries to move after game is Won
        var ex = Assert.Throws<GameAlreadyCompletedException>(() => game.MakeMove(Player.O, new CellIndex(5)));
        Assert.Equal(GameStatus.Won, ex.CurrentStatus);

        // Verify state is preserved
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(historyCountBefore, game.MoveHistory.Count);
        Assert.Null(game.Board.GetCell(new CellIndex(5)));
    }

    [Fact]
    public void MakeMove_AfterNaturalDraw_RejectsSubsequentMovesAndPreservesState()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(2));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(4));
        game.MakeMove(Player.O, new CellIndex(6));
        game.MakeMove(Player.X, new CellIndex(5));
        game.MakeMove(Player.O, new CellIndex(8));
        game.MakeMove(Player.X, new CellIndex(7)); // Draw

        // Act & Assert
        var ex = Assert.Throws<GameAlreadyCompletedException>(() => game.MakeMove(Player.O, new CellIndex(0)));
        Assert.Equal(GameStatus.Draw, ex.CurrentStatus);

        // Verify state is preserved
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.MoveHistory.Count);
    }

    [Fact]
    public void MakeMove_AtomicityAndOccupiedCellConsistency_PreservesInvariants()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));

        // Invariant: MoveHistory count equals Board occupied count
        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);

        // Attempt invalid move (occupied cell)
        Assert.Throws<CellOccupiedException>(() => game.MakeMove(Player.X, new CellIndex(1)));

        // Invariant holds: both counts unchanged
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Equal(2, game.Board.OccupiedCount);
        Assert.Equal(game.MoveHistory.Count, game.Board.OccupiedCount);
    }
}
