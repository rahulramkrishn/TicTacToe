namespace TicTacToe.Tests.Domain;

using System;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class GameResetTests
{
    [Fact]
    public void Reset_RestoresFreshBoardAndStartingPlayer()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));

        game.Reset();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Empty(game.MoveHistory);
        Assert.False(game.CanUndo);

        for (var i = 0; i < 9; i++)
        {
            Assert.False(game.Board.IsOccupied(new CellIndex(i)));
        }
    }

    [Fact]
    public void Reset_PreservesGameId()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        var initialId = game.Id;

        game.MakeMove(Player.X, new CellIndex(0));
        game.Reset();

        Assert.Equal(initialId, game.Id);
    }

    [Fact]
    public void Reset_DoesNotEmitDomainEvents()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        Assert.Empty(game.DomainEvents);

        game.Reset();

        Assert.Empty(game.DomainEvents);
    }

    [Fact]
    public void Reset_AllowsNewGameToPlayToCompletion()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        // Play first game to win
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Single(game.DomainEvents);

        // Clear events as application layer would after dispatch
        game.ClearDomainEvents();

        // Reset
        game.Reset();
        Assert.Equal(GameStatus.InProgress, game.Status);

        // Play second game to win
        game.MakeMove(Player.X, new CellIndex(3));
        game.MakeMove(Player.O, new CellIndex(0));
        game.MakeMove(Player.X, new CellIndex(4));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(5)); // Won again!
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Single(game.DomainEvents);
    }

    [Fact]
    public void Reset_WhenCompletedAgain_ProducesNewEventIdWithSameGameId()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        // Completion 1
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won

        var event1 = Assert.IsType<GameCompletedEvent>(game.DomainEvents.First());
        game.ClearDomainEvents();

        // Reset
        game.Reset();

        // Completion 2
        game.MakeMove(Player.X, new CellIndex(3));
        game.MakeMove(Player.O, new CellIndex(0));
        game.MakeMove(Player.X, new CellIndex(4));
        game.MakeMove(Player.O, new CellIndex(1));
        game.MakeMove(Player.X, new CellIndex(5)); // Won

        var event2 = Assert.IsType<GameCompletedEvent>(game.DomainEvents.First());

        // Invariant: E1 != E2, but GameId is preserved
        Assert.NotEqual(event1.EventId, event2.EventId);
        Assert.Equal(event1.GameId, event2.GameId);
        Assert.Equal(game.Id, event2.GameId);
    }
}
