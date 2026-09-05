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
}
