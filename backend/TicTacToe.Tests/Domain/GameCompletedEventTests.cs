namespace TicTacToe.Tests.Domain;

using System;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class GameCompletedEventTests
{
    [Fact]
    public void EventPayload_OnWin_ContainsCorrectGameIdWinnerWinningCellsAndMoveCount()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var gameId = GameId.New();
        var winningCells = new[] { 0, 1, 2 };
        var now = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new GameCompletedEvent(
            eventId,
            gameId,
            GameStatus.Won,
            Player.X,
            winningCells,
            5,
            now);

        // Assert
        Assert.Equal(eventId, domainEvent.EventId);
        Assert.Equal(gameId, domainEvent.GameId);
        Assert.Equal(GameStatus.Won, domainEvent.Result);
        Assert.Equal(Player.X, domainEvent.Winner);
        Assert.Equal(winningCells, domainEvent.WinningCells);
        Assert.Equal(5, domainEvent.MoveCount);
        Assert.Equal(now, domainEvent.OccurredOn);
    }

    [Fact]
    public void EventPayload_OnDraw_ContainsNullWinnerAndEmptyWinningCells()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var gameId = GameId.New();
        var now = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new GameCompletedEvent(
            eventId,
            gameId,
            GameStatus.Draw,
            null,
            Array.Empty<int>(),
            9,
            now);

        // Assert
        Assert.Equal(eventId, domainEvent.EventId);
        Assert.Equal(gameId, domainEvent.GameId);
        Assert.Equal(GameStatus.Draw, domainEvent.Result);
        Assert.Null(domainEvent.Winner);
        Assert.Empty(domainEvent.WinningCells);
        Assert.Equal(9, domainEvent.MoveCount);
    }

    [Fact]
    public void EventPayload_WhenInstantiatedWithInProgressStatus_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.InProgress,
            null,
            Array.Empty<int>(),
            0,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EventPayload_WhenWonWithoutWinner_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            null,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EventPayload_WhenWonWithInvalidWinningCellsCount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1 }, // Only 2 cells
            5,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EventPayload_WhenDrawWithNonNullWinner_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Draw,
            Player.X,
            Array.Empty<int>(),
            9,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EventPayload_WhenEmptyGameId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new GameCompletedEvent(
            Guid.NewGuid(),
            default(GameId),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EventPayload_HasUniqueEventIdAndUtcTimestamp()
    {
        var e1 = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);
        var e2 = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);

        Assert.NotEqual(e1.EventId, e2.EventId);
    }
}
