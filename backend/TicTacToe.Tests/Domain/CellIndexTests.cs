using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class CellIndexTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Constructor_WithValidIndex_CreatesInstanceWithValue(int validIndex)
    {
        // Act
        var cellIndex = new CellIndex(validIndex);

        // Assert
        Assert.Equal(validIndex, cellIndex.Value);
        Assert.Equal(validIndex, (int)cellIndex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(9)]
    [InlineData(100)]
    public void Constructor_WithInvalidIndex_ThrowsInvalidCellIndexException(int invalidIndex)
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidCellIndexException>(() => new CellIndex(invalidIndex));
        Assert.Equal(invalidIndex, ex.AttemptedIndex);
    }

    [Fact]
    public void TryCreate_WithValidIndex_ReturnsTrueAndCellIndex()
    {
        // Act
        var success = CellIndex.TryCreate(4, out var cellIndex);

        // Assert
        Assert.True(success);
        Assert.Equal(4, cellIndex.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void TryCreate_WithInvalidIndex_ReturnsFalse(int invalidIndex)
    {
        // Act
        var success = CellIndex.TryCreate(invalidIndex, out var cellIndex);

        // Assert
        Assert.False(success);
        Assert.Equal(default, cellIndex);
    }

    [Fact]
    public void Equals_WithSameIndex_ReturnsTrue()
    {
        // Arrange
        var a = new CellIndex(3);
        var b = new CellIndex(3);

        // Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_WithDifferentIndex_ReturnsFalse()
    {
        // Arrange
        var a = new CellIndex(3);
        var b = new CellIndex(5);

        // Assert
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
