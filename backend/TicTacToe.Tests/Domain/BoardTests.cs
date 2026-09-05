using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class BoardTests
{
    [Fact]
    public void Constructor_InitializesExactlyNineEmptyCells()
    {
        // Act
        var board = new Board();

        // Assert
        Assert.Equal(9, board.Cells.Count);
        Assert.All(board.Cells, cell => Assert.Null(cell));
    }

    [Fact]
    public void PlaceMark_OnEmptyCell_SetsPlayerCorrectly()
    {
        // Arrange
        var board = new Board();
        var index = new CellIndex(4);

        // Act
        board.PlaceMark(index, Player.X);

        // Assert
        Assert.Equal(Player.X, board.GetCell(index));
        Assert.True(board.IsOccupied(index));
    }

    [Fact]
    public void PlaceMark_OnOccupiedCell_ThrowsCellOccupiedException()
    {
        // Arrange
        var board = new Board();
        var index = new CellIndex(4);
        board.PlaceMark(index, Player.X);

        // Act & Assert
        var ex = Assert.Throws<CellOccupiedException>(() => board.PlaceMark(index, Player.O));
        Assert.Equal(4, ex.CellIndex);
        // Ensure mark was not overwritten
        Assert.Equal(Player.X, board.GetCell(index));
    }

    [Fact]
    public void Clone_CreatesIndependentDeepCopy()
    {
        // Arrange
        var board = new Board();
        var index = new CellIndex(0);
        board.PlaceMark(index, Player.X);

        // Act
        var clone = board.Clone();

        // Assert
        Assert.Equal(Player.X, clone.GetCell(index));
        // Mutating original does not mutate clone
        board.PlaceMark(new CellIndex(1), Player.O);
        Assert.Null(clone.GetCell(new CellIndex(1)));
    }
}
