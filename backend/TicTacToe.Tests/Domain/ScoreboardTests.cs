namespace TicTacToe.Tests.Domain;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ScoreboardTests
{
    [Fact]
    public void RecordWin_WhenPlayerXWins_IncrementsXWins()
    {
        var scoreboard = new Scoreboard();

        scoreboard.RecordWin(Player.X);

        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordWin_WhenPlayerOWins_IncrementsOWins()
    {
        var scoreboard = new Scoreboard();

        scoreboard.RecordWin(Player.O);

        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(1, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void RecordDraw_IncrementsDraws()
    {
        var scoreboard = new Scoreboard();

        scoreboard.RecordDraw();

        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(1, scoreboard.Draws);
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
    public async Task RecordWin_ConcurrentIncrements_AreThreadSafe()
    {
        var scoreboard = new Scoreboard();
        const int concurrencyLevel = 20;
        var tasks = new List<Task>();

        for (var i = 0; i < concurrencyLevel; i++)
        {
            tasks.Add(Task.Run(() => scoreboard.RecordWin(Player.X)));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrencyLevel, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(concurrencyLevel, scoreboard.TotalGames);
    }

    [Fact]
    public async Task RecordDraw_ConcurrentIncrements_AreThreadSafe()
    {
        var scoreboard = new Scoreboard();
        const int concurrencyLevel = 20;
        var tasks = new List<Task>();

        for (var i = 0; i < concurrencyLevel; i++)
        {
            tasks.Add(Task.Run(() => scoreboard.RecordDraw()));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(concurrencyLevel, scoreboard.Draws);
        Assert.Equal(concurrencyLevel, scoreboard.TotalGames);
    }
}
