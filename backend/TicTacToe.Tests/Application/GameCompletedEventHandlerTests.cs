namespace TicTacToe.Tests.Application;

using System;
using System.Threading.Tasks;
using TicTacToe.Application.EventHandlers;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class GameCompletedEventHandlerTests
{
    [Fact]
    public async Task Handler_UpdatesAndSavesScoreboardOnEvent()
    {
        var scoreboard = new Scoreboard();
        var repo = new InMemoryScoreboardRepository(scoreboard);
        var handler = new GameCompletedEventHandler(repo);

        var domainEvent = new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow);

        await handler.HandleAsync(domainEvent);

        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
    }

    [Fact]
    public async Task Handler_DuplicateEvent_DoesNotDoubleCount()
    {
        var scoreboard = new Scoreboard();
        var repo = new InMemoryScoreboardRepository(scoreboard);
        var handler = new GameCompletedEventHandler(repo);

        var domainEvent = new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow);

        await handler.HandleAsync(domainEvent);
        await handler.HandleAsync(domainEvent); // Duplicate delivery

        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public async Task Handler_ThrowsWhenEventIsNull()
    {
        var repo = new InMemoryScoreboardRepository();
        var handler = new GameCompletedEventHandler(repo);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));
    }
}
