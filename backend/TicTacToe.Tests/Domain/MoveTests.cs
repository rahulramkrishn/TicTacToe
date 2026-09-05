using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class MoveTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesProperties()
    {
        // Arrange
        var cellIndex = new CellIndex(4);

        // Act
        var move = new Move(1, Player.X, cellIndex);

        // Assert
        Assert.Equal(1, move.MoveNumber);
        Assert.Equal(Player.X, move.Player);
        Assert.Equal(cellIndex, move.CellIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMoveNumber_ThrowsArgumentOutOfRangeException(int invalidMoveNumber)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Move(invalidMoveNumber, Player.X, new CellIndex(0)));
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var move1 = new Move(1, Player.X, new CellIndex(0));
        var move2 = new Move(1, Player.X, new CellIndex(0));

        // Assert
        Assert.Equal(move1, move2);
    }
}
