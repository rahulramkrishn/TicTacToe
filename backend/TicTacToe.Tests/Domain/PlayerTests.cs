using TicTacToe.Domain.ValueObjects;

namespace TicTacToe.Tests.Domain;

public class PlayerTests
{
    [Fact]
    public void Other_ForPlayerX_ReturnsPlayerO()
    {
        // Act & Assert
        Assert.Equal(Player.O, Player.X.Other());
    }

    [Fact]
    public void Other_ForPlayerO_ReturnsPlayerX()
    {
        // Act & Assert
        Assert.Equal(Player.X, Player.O.Other());
    }
}
