namespace TicTacToe.Tests.Domain;

using System;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ResetIndependenceTests
{
    [Fact]
    public void GameReset_ClearsGameState_ScoreboardUnchanged()
    {
        var scoreboard = new Scoreboard();
        var game = Game.Create(GameMode.TwoPlayer);

        // Complete game: X wins
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2));

        var evt = (GameCompletedEvent)game.DomainEvents.GetEnumerator().Current ?? (GameCompletedEvent)System.Linq.Enumerable.First(game.DomainEvents);
        scoreboard.RecordGameCompleted(evt);

        Assert.Equal(1, scoreboard.XWins);

        // Reset Game only
        game.Reset();

        // Game is reset, but scoreboard remains 1
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(1, scoreboard.TotalGames);
    }

    [Fact]
    public void ScoreboardReset_ClearsCounters_GameAggregateUnchanged()
    {
        var scoreboard = new Scoreboard();
        var game = Game.Create(GameMode.TwoPlayer);

        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won

        var evt = (GameCompletedEvent)System.Linq.Enumerable.First(game.DomainEvents);
        scoreboard.RecordGameCompleted(evt);
        Assert.Equal(1, scoreboard.XWins);

        // Reset Scoreboard only
        scoreboard.Reset();

        // Scoreboard is reset to 0
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.TotalGames);

        // Game aggregate remains in Won state
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(5, game.MoveHistory.Count);
    }

    [Fact]
    public void GameReset_PreservesSameGameId()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        var originalId = game.Id;

        game.MakeMove(Player.X, new CellIndex(0));
        game.Reset();

        Assert.Equal(originalId, game.Id);
    }

    [Fact]
    public void GameReset_DoesNotEmitDomainEvents()
    {
        var game = Game.Create(GameMode.TwoPlayer);
        game.Reset();

        Assert.Empty(game.DomainEvents);
    }
}
