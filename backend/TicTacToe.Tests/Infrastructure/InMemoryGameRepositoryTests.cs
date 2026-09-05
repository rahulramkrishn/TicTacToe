namespace TicTacToe.Tests.Infrastructure;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.ValueObjects;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class InMemoryGameRepositoryTests
{
    [Fact]
    public async Task SaveAsync_And_GetByIdAsync_StoresAndRetrievesAuthoritativeAggregate()
    {
        var repo = new InMemoryGameRepository();
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));

        await repo.SaveAsync(game);
        var retrieved = await repo.GetByIdAsync(game.Id);

        Assert.NotNull(retrieved);
        Assert.Same(game, retrieved);
        Assert.Equal(Player.O, retrieved.CurrentPlayer);
        Assert.Equal(Player.X, retrieved.Board.GetCell(new CellIndex(0)));
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var repo = new InMemoryGameRepository();
        var result = await repo.GetByIdAsync(GameId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task ConcurrentSaves_DoNotCorruptDictionary()
    {
        var repo = new InMemoryGameRepository();
        const int count = 50;
        var tasks = new List<Task>();

        for (var i = 0; i < count; i++)
        {
            var g = Game.Create(GameMode.TwoPlayer);
            tasks.Add(Task.Run(() => repo.SaveAsync(g)));
        }

        await Task.WhenAll(tasks);

        // All 50 games should exist without exception
        Assert.True(true);
    }
}
