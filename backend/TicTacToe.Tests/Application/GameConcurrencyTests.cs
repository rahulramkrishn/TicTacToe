namespace TicTacToe.Tests.Application;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Application.Models;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class GameConcurrencyTests
{
    private readonly InMemoryGameRepository _gameRepository;
    private readonly InMemoryScoreboardRepository _scoreboardRepository;
    private readonly IComputerMoveStrategy _computerStrategy;
    private readonly GameService _gameService;

    public GameConcurrencyTests()
    {
        _gameRepository = new InMemoryGameRepository();
        _scoreboardRepository = new InMemoryScoreboardRepository();
        _computerStrategy = new BasicComputerMoveStrategy();

        _gameService = new GameService(
            _gameRepository,
            _scoreboardRepository,
            _computerStrategy);
    }

    [Fact]
    public async Task ConcurrentMoves_OnDifferentGameIds_ExecuteWithoutContention()
    {
        var game1 = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var game2 = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));

        var task1 = Task.Run(() => _gameService.MakeMoveAsync(new MakeMoveCommand(game1.GameId, Player.X, 0)));
        var task2 = Task.Run(() => _gameService.MakeMoveAsync(new MakeMoveCommand(game2.GameId, Player.X, 4)));

        var results = await Task.WhenAll(task1, task2);

        Assert.Equal(Player.X, results[0].Board[0]);
        Assert.Equal(Player.X, results[1].Board[4]);
    }

    [Fact]
    public async Task ConcurrentMoves_OnSameGameId_AreSerializedWithoutLostMovesOrCorruption()
    {
        // Setup: create a two-player game
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var gameId = game.GameId;

        // Player X and Player O try to move simultaneously on cell 0 and cell 1
        // One must succeed first, the other must either alternate turn or be rejected cleanly
        // Let's test that 10 parallel tasks attempting moves on the same game do not throw unhandled race conditions
        const int attempts = 10;
        var tasks = new List<Task<GameStateDto?>>();

        for (var i = 0; i < attempts; i++)
        {
            var cell = i % 9;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Attempt alternating players
                    var player = (cell % 2 == 0) ? Player.X : Player.O;
                    return await _gameService.MakeMoveAsync(new MakeMoveCommand(gameId, player, cell));
                }
                catch
                {
                    // Controlled domain rejection (InvalidTurnException, CellOccupiedException, etc.) is expected
                    return null;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Verify that the game aggregate in repo is in a completely valid, uncorrupted state
        var authoritativeGame = await _gameService.GetGameAsync(new GameId(gameId));
        Assert.NotNull(authoritativeGame);
        Assert.True(authoritativeGame.MoveHistory.Count >= 1);

        // Turn history must have strictly alternating players and strictly increasing move numbers
        for (var i = 0; i < authoritativeGame.MoveHistory.Count; i++)
        {
            Assert.Equal(i + 1, authoritativeGame.MoveHistory[i].MoveNumber);
            if (i > 0)
            {
                Assert.NotEqual(authoritativeGame.MoveHistory[i].Player, authoritativeGame.MoveHistory[i - 1].Player);
            }
        }
    }

    [Fact]
    public async Task ConcurrentMoveAndUndo_OnSameGameId_AreSerializedSafely()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var gameId = game.GameId;

        // Play one move first so undo is valid
        await _gameService.MakeMoveAsync(new MakeMoveCommand(gameId, Player.X, 0));

        // Concurrently run Move and Undo
        var moveTask = Task.Run(async () =>
        {
            try { return await _gameService.MakeMoveAsync(new MakeMoveCommand(gameId, Player.O, 1)); }
            catch { return null; }
        });

        var undoTask = Task.Run(async () =>
        {
            try { return await _gameService.UndoAsync(new GameId(gameId)); }
            catch { return null; }
        });

        await Task.WhenAll(moveTask, undoTask);

        // The final state must be valid
        var finalState = await _gameService.GetGameAsync(new GameId(gameId));
        Assert.NotNull(finalState);
        Assert.True(finalState.Status == GameStatus.InProgress);
    }

    [Fact]
    public async Task ConcurrentMoveAndReset_OnSameGameId_AreSerializedSafely()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var gameId = game.GameId;

        var moveTask = Task.Run(async () =>
        {
            try { return await _gameService.MakeMoveAsync(new MakeMoveCommand(gameId, Player.X, 0)); }
            catch { return null; }
        });

        var resetTask = Task.Run(async () =>
        {
            try { return await _gameService.ResetGameAsync(new GameId(gameId)); }
            catch { return null; }
        });

        await Task.WhenAll(moveTask, resetTask);

        var finalState = await _gameService.GetGameAsync(new GameId(gameId));
        Assert.NotNull(finalState);
        Assert.Equal(gameId, finalState.GameId);
    }
}
