namespace TicTacToe.Tests.Application;

using System;
using System.Linq;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ComputerModeEventIntegrationTests
{
    private readonly IComputerMoveStrategy _strategy = new BasicComputerMoveStrategy();

    [Fact]
    public void ExecuteTurn_WhenHumanWins_EmitsSingleEventAndStops()
    {
        var game = Game.Create(GameMode.Computer);

        // Turn 1: Human 0, Computer takes 4
        game.ExecuteTurn(new CellIndex(0), _strategy);
        Assert.Empty(game.DomainEvents);

        // Turn 2: Human 8, Computer takes 2
        game.ExecuteTurn(new CellIndex(8), _strategy);
        Assert.Empty(game.DomainEvents);

        // Turn 3: Human 6 creates fork (threatens 3 and 7). Computer blocks lowest index 3.
        game.ExecuteTurn(new CellIndex(6), _strategy);
        Assert.Empty(game.DomainEvents);

        // Turn 4: Human completes row 2 at cell 7. Human wins!
        var turnResult = game.ExecuteTurn(new CellIndex(7), _strategy);

        Assert.Equal(new CellIndex(7), turnResult.HumanMove);
        Assert.Null(turnResult.ComputerMove);
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Single(game.DomainEvents);

        var evt = (GameCompletedEvent)game.DomainEvents.First();
        Assert.Equal(Player.X, evt.Winner);
    }

    [Fact]
    public void ExecuteTurn_WhenComputerWins_EmitsSingleEvent()
    {
        // Set up a game where Computer (O) can win on its turn
        var game = Game.Create(GameMode.Computer);

        // Turn 1: Human plays 0, Computer plays center 4
        game.ExecuteTurn(new CellIndex(0), _strategy);

        // Turn 2: Human plays 1, Computer plays block/move
        game.ExecuteTurn(new CellIndex(1), _strategy);

        // Let's set up explicit moves to give Computer a direct win
        // Reset and play deliberately:
        var deliberateGame = Game.Create(GameMode.Computer);
        deliberateGame.MakeMove(Player.X, new CellIndex(0));
        deliberateGame.MakeMove(Player.O, new CellIndex(3));
        deliberateGame.MakeMove(Player.X, new CellIndex(1));
        deliberateGame.MakeMove(Player.O, new CellIndex(4));
        deliberateGame.MakeMove(Player.X, new CellIndex(8)); // Non-blocking human move
        // Now it's Computer O's turn with cells 3 and 4 occupied. Cell 5 completes winning row!
        Assert.Empty(deliberateGame.DomainEvents);

        deliberateGame.PlayComputerMove(_strategy);

        Assert.Equal(GameStatus.Won, deliberateGame.Status);
        Assert.Equal(Player.O, deliberateGame.Winner);
        Assert.Single(deliberateGame.DomainEvents);

        var evt = (GameCompletedEvent)deliberateGame.DomainEvents.First();
        Assert.Equal(Player.O, evt.Winner);
        Assert.Equal(new[] { 3, 4, 5 }, evt.WinningCells);
    }

    [Fact]
    public void ExecuteTurn_WhenNonTerminal_EmitsZeroEvents()
    {
        var game = Game.Create(GameMode.Computer);
        var turnResult = game.ExecuteTurn(new CellIndex(0), _strategy);

        Assert.NotNull(turnResult.ComputerMove);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Empty(game.DomainEvents);
    }
}
