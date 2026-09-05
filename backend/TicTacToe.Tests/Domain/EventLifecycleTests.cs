namespace TicTacToe.Tests.Domain;

using System;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Application.EventHandlers;
using TicTacToe.Application.Events;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class EventLifecycleTests
{
    [Fact]
    public void GameCompleted_EventExistsAfterTerminalMove()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Terminal move

        // Assert
        Assert.Single(game.DomainEvents);
        var evt = game.DomainEvents.First();
        Assert.IsType<GameCompletedEvent>(evt);
    }

    [Fact]
    public void GameReset_DoesNotSilentlyDiscardPendingEvent()
    {
        // Arrange: Terminal transition creates pending domain event
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won

        Assert.Single(game.DomainEvents);
        var pendingEvent = game.DomainEvents.First();

        // Act: Game.Reset() called while event is still pending
        game.Reset();

        // Assert (P008.2 specification): Game state is reset, but DomainEvents MUST NOT be cleared
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Empty(game.MoveHistory);
        Assert.Single(game.DomainEvents);
        Assert.Same(pendingEvent, game.DomainEvents.First());
    }

    [Fact]
    public async Task SuccessfulDispatch_ClearsPendingEvent()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2));

        var scoreboard = new Scoreboard();
        var repo = new InMemoryScoreboardRepository(scoreboard);
        var handler = new GameCompletedEventHandler(repo);
        var dispatcher = new DomainEventDispatcher();
        dispatcher.RegisterHandler(handler);

        // Act
        var eventsToDispatch = game.DomainEvents.ToList();
        await dispatcher.DispatchAsync(eventsToDispatch);
        game.ClearDomainEvents();

        // Assert
        Assert.Empty(game.DomainEvents);
        Assert.Equal(1, scoreboard.XWins);
    }

    [Fact]
    public async Task FailedDispatch_RetainsPendingEvent()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2));

        var dispatcher = new DomainEventDispatcher();
        dispatcher.RegisterHandler<GameCompletedEvent>(_ => throw new InvalidOperationException("Simulated failure in dispatch"));

        // Act & Assert
        var eventsToDispatch = game.DomainEvents.ToList();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await dispatcher.DispatchAsync(eventsToDispatch);
            game.ClearDomainEvents(); // Should not be reached
        });

        // Event remains pending for subsequent retry
        Assert.Single(game.DomainEvents);
    }
}
