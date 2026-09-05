namespace TicTacToe.Tests.Application;

using System;
using System.Threading.Tasks;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Entities;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class ScoreboardServiceTests
{
    [Fact]
    public async Task GetScoreboard_ReturnsAuthoritativeCounters()
    {
        var scoreboard = new Scoreboard(3, 2, 1);
        var repo = new InMemoryScoreboardRepository(scoreboard);
        var service = new ScoreboardService(repo);

        var dto = await service.GetScoreboardAsync();

        Assert.Equal(3, dto.XWins);
        Assert.Equal(2, dto.OWins);
        Assert.Equal(1, dto.Draws);
        Assert.Equal(6, dto.TotalGames);
    }

    [Fact]
    public async Task ResetScoreboard_ResetsCountersToZero()
    {
        var scoreboard = new Scoreboard(3, 2, 1);
        var repo = new InMemoryScoreboardRepository(scoreboard);
        var service = new ScoreboardService(repo);

        var resetDto = await service.ResetScoreboardAsync();

        Assert.Equal(0, resetDto.XWins);
        Assert.Equal(0, resetDto.OWins);
        Assert.Equal(0, resetDto.Draws);
        Assert.Equal(0, resetDto.TotalGames);
    }

    [Fact]
    public async Task ResetScoreboard_DoesNotModifyActiveGames()
    {
        var scoreboard = new Scoreboard(1, 0, 0);
        var scoreboardRepo = new InMemoryScoreboardRepository(scoreboard);
        var scoreboardService = new ScoreboardService(scoreboardRepo);

        var gameRepo = new InMemoryGameRepository();
        var game = TicTacToe.Domain.Aggregates.Game.Create(TicTacToe.Domain.ValueObjects.GameMode.TwoPlayer);
        game.MakeMove(TicTacToe.Domain.ValueObjects.Player.X, new TicTacToe.Domain.ValueObjects.CellIndex(4));
        await gameRepo.SaveAsync(game);

        // Reset scoreboard
        await scoreboardService.ResetScoreboardAsync();

        // Game aggregate in repository is completely unchanged
        var retrievedGame = await gameRepo.GetByIdAsync(game.Id);
        Assert.NotNull(retrievedGame);
        Assert.Equal(TicTacToe.Domain.ValueObjects.Player.O, retrievedGame.CurrentPlayer);
        Assert.Single(retrievedGame.MoveHistory);
        Assert.Equal(TicTacToe.Domain.ValueObjects.Player.X, retrievedGame.Board.GetCell(new TicTacToe.Domain.ValueObjects.CellIndex(4)));
    }
}
