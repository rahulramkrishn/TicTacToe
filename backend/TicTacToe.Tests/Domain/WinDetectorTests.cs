using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class WinDetectorTests
{
    private static readonly int[][] CanonicalWinningLines =
    {
        new[] { 0, 1, 2 }, // Row 0
        new[] { 3, 4, 5 }, // Row 1
        new[] { 6, 7, 8 }, // Row 2
        new[] { 0, 3, 6 }, // Column 0
        new[] { 1, 4, 7 }, // Column 1
        new[] { 2, 5, 8 }, // Column 2
        new[] { 0, 4, 8 }, // Main diagonal
        new[] { 2, 4, 6 }  // Anti diagonal
    };

    public static IEnumerable<object[]> WinningLinesData()
    {
        for (int i = 0; i < CanonicalWinningLines.Length; i++)
        {
            yield return new object[] { CanonicalWinningLines[i] };
        }
    }

    [Theory]
    [MemberData(nameof(WinningLinesData))]
    public void CheckWin_AllEightLines_DetectsWinForPlayerX(int[] line)
    {
        // Arrange
        var board = new Board();
        board.PlaceMark(new CellIndex(line[0]), Player.X);
        board.PlaceMark(new CellIndex(line[1]), Player.X);
        board.PlaceMark(new CellIndex(line[2]), Player.X);

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert
        Assert.True(result.IsWin);
        Assert.Equal(Player.X, result.Winner);
        Assert.Equal(line, result.WinningCells);
    }

    [Theory]
    [MemberData(nameof(WinningLinesData))]
    public void CheckWin_AllEightLines_DetectsWinForPlayerO(int[] line)
    {
        // Arrange
        var board = new Board();
        board.PlaceMark(new CellIndex(line[0]), Player.O);
        board.PlaceMark(new CellIndex(line[1]), Player.O);
        board.PlaceMark(new CellIndex(line[2]), Player.O);

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert
        Assert.True(result.IsWin);
        Assert.Equal(Player.O, result.Winner);
        Assert.Equal(line, result.WinningCells);
    }

    [Fact]
    public void CheckWin_EmptyBoard_ReturnsNoWin()
    {
        // Arrange
        var board = new Board();

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert
        Assert.False(result.IsWin);
        Assert.Null(result.Winner);
        Assert.Empty(result.WinningCells);
        Assert.Same(WinResult.None, result);
    }

    [Fact]
    public void CheckWin_PartialBoardWithoutWinningLine_ReturnsNoWin()
    {
        // Arrange
        var board = new Board();
        board.PlaceMark(new CellIndex(0), Player.X);
        board.PlaceMark(new CellIndex(1), Player.O);
        board.PlaceMark(new CellIndex(2), Player.X);

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert
        Assert.False(result.IsWin);
        Assert.Null(result.Winner);
        Assert.Empty(result.WinningCells);
    }

    [Fact]
    public void CheckWin_FullBoardWithoutWinningLine_ReturnsNoWin()
    {
        // Arrange: Classic draw board:
        // X O X
        // X X O
        // O X O
        // Indices:
        // 0:X, 1:O, 2:X
        // 3:X, 4:X, 5:O
        // 6:O, 7:X, 8:O
        var board = new Board(new Player?[]
        {
            Player.X, Player.O, Player.X,
            Player.X, Player.X, Player.O,
            Player.O, Player.X, Player.O
        });

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert
        Assert.False(result.IsWin);
        Assert.Null(result.Winner);
        Assert.Empty(result.WinningCells);
    }

    [Fact]
    public void CheckWin_NullBoard_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WinDetector.CheckWin(null!));
    }

    [Fact]
    public void CheckWin_WinningCells_IsDefensivelyIsolatedFromMutation()
    {
        // Arrange
        var board = new Board();
        board.PlaceMark(new CellIndex(0), Player.X);
        board.PlaceMark(new CellIndex(1), Player.X);
        board.PlaceMark(new CellIndex(2), Player.X);

        // Act
        var result = WinDetector.CheckWin(board);

        // Assert: WinningCells is a read-only collection that cannot be cast to a mutable array or mutated
        Assert.True(result.WinningCells is System.Collections.ObjectModel.ReadOnlyCollection<int>);
        Assert.Equal(new[] { 0, 1, 2 }, result.WinningCells);
    }
}
