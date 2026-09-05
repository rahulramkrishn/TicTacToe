namespace TicTacToe.Tests.Domain;

using System;
using TicTacToe.Domain.Aggregates;
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
        Assert.Equal(Player.X, game.Winner);

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
        Assert.Equal(Player.X, game.Winner);
    }
}
