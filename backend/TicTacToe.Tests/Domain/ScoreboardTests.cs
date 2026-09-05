namespace TicTacToe.Tests.Domain;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ScoreboardTests
{
    [Fact]
    public void RecordGameCompleted_WhenPlayerXWins_IncrementsXWinsOnce()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);

        var processed = scoreboard.RecordGameCompleted(evt);

        Assert.True(processed);
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordGameCompleted_WhenPlayerOWins_IncrementsOWinsOnce()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.O, new[] { 1, 4, 7 }, 6, DateTimeOffset.UtcNow);

        var processed = scoreboard.RecordGameCompleted(evt);

        Assert.True(processed);
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(1, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordGameCompleted_WhenDraw_IncrementsDrawsOnce()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Draw, null, Array.Empty<int>(), 9, DateTimeOffset.UtcNow);

        var processed = scoreboard.RecordGameCompleted(evt);

        Assert.True(processed);
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(1, scoreboard.Draws);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordGameCompleted_WhenDuplicateEventIdDelivered_IgnoresDuplicateAndReturnsFalse()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);

        var first = scoreboard.RecordGameCompleted(evt);
        var second = scoreboard.RecordGameCompleted(evt);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void Reset_ResetsCountersToZero()
    {
        var scoreboard = new Scoreboard(3, 2, 1);
        Assert.Equal(3, scoreboard.XWins);
        Assert.Equal(2, scoreboard.OWins);
        Assert.Equal(1, scoreboard.Draws);
        Assert.Equal(6, scoreboard.TotalGames);

        scoreboard.Reset();

        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(0, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordGameCompleted_AfterScoreboardReset_DuplicateEventStillIgnored()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);

        scoreboard.RecordGameCompleted(evt);
        Assert.Equal(1, scoreboard.XWins);

        scoreboard.Reset();
        Assert.Equal(0, scoreboard.XWins);

        // Same event delivered again after reset
        var result = scoreboard.RecordGameCompleted(evt);

        Assert.False(result);
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.TotalGames);
    }

    [Fact]
    public async Task RecordGameCompleted_ConcurrentDuplicateDelivery_IncrementsExactlyOnce()
    {
        var scoreboard = new Scoreboard();
        var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);

        const int concurrencyLevel = 20;
        var tasks = new List<Task<bool>>();

        for (var i = 0; i < concurrencyLevel; i++)
        {
            tasks.Add(Task.Run(() => scoreboard.RecordGameCompleted(evt)));
        }

        var results = await Task.WhenAll(tasks);

        var successCount = 0;
        var duplicateCount = 0;
        foreach (var r in results)
        {
            if (r) successCount++;
            else duplicateCount++;
        }

        Assert.Equal(1, successCount);
        Assert.Equal(concurrencyLevel - 1, duplicateCount);
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public async Task RecordGameCompleted_ConcurrentDistinctEvents_AllIncrementCounters()
    {
        var scoreboard = new Scoreboard();
        const int concurrencyLevel = 20;
        var tasks = new List<Task<bool>>();

        for (var i = 0; i < concurrencyLevel; i++)
        {
            var evt = new GameCompletedEvent(Guid.NewGuid(), GameId.New(), GameStatus.Won, Player.X, new[] { 0, 1, 2 }, 5, DateTimeOffset.UtcNow);
            tasks.Add(Task.Run(() => scoreboard.RecordGameCompleted(evt)));
        }

        var results = await Task.WhenAll(tasks);

        var successCount = 0;
        foreach (var r in results)
        {
            if (r) successCount++;
        }

        Assert.Equal(concurrencyLevel, successCount);
        Assert.Equal(concurrencyLevel, scoreboard.XWins);
        Assert.Equal(concurrencyLevel, scoreboard.TotalGames);
    }
}
