namespace TicTacToe.Tests.Application;

using System;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Application.EventHandlers;
using TicTacToe.Application.Events;
using TicTacToe.Application.Exceptions;
using TicTacToe.Application.Models;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class GameServiceTests
{
    private readonly InMemoryGameRepository _gameRepository;
    private readonly InMemoryScoreboardRepository _scoreboardRepository;
    private readonly DomainEventDispatcher _eventDispatcher;
    private readonly IComputerMoveStrategy _computerStrategy;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepository = new InMemoryGameRepository();
        _scoreboardRepository = new InMemoryScoreboardRepository();
        _eventDispatcher = new DomainEventDispatcher();
        _computerStrategy = new BasicComputerMoveStrategy();

        // Wire event handler
        var handler = new GameCompletedEventHandler(_scoreboardRepository);
        _eventDispatcher.RegisterHandler(handler);

        _gameService = new GameService(
            _gameRepository,
            _scoreboardRepository,
            _eventDispatcher,
            _computerStrategy);
    }

    [Fact]
    public async Task CreateGame_ReturnsInitialGameState_WithRequestedMode()
    {
        var command = new CreateGameCommand(GameMode.TwoPlayer);
        var result = await _gameService.CreateGameAsync(command);

        Assert.NotEqual(Guid.Empty, result.GameId);
        Assert.Equal(9, result.Board.Count);
        Assert.All(result.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, result.CurrentPlayer);
        Assert.Equal(GameMode.TwoPlayer, result.Mode);
        Assert.Equal(GameStatus.InProgress, result.Status);
        Assert.Null(result.Winner);
        Assert.Empty(result.WinningCells);
        Assert.Empty(result.MoveHistory);
        Assert.False(result.CanUndo);
        Assert.Equal(0, result.Scoreboard.TotalGames);
    }

    [Fact]
    public async Task GetGame_WhenGameExists_ReturnsAuthoritativeState()
    {
        var created = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var retrieved = await _gameService.GetGameAsync(new GameId(created.GameId));

        Assert.Equal(created.GameId, retrieved.GameId);
        Assert.Equal(created.Status, retrieved.Status);
    }

    [Fact]
    public async Task GetGame_WhenGameNotFound_ThrowsGameNotFoundException()
    {
        var missingId = new GameId(Guid.NewGuid());
        var ex = await Assert.ThrowsAsync<GameNotFoundException>(() => _gameService.GetGameAsync(missingId));
        Assert.Equal(missingId.Value, ex.GameId);
    }

    [Fact]
    public async Task MakeMove_ValidMove_UpdatesBoardAndAlternatesTurn()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));

        var moveCommand = new MakeMoveCommand(game.GameId, Player.X, 0);
        var result = await _gameService.MakeMoveAsync(moveCommand);

        Assert.Equal(Player.X, result.Board[0]);
        Assert.Equal(Player.O, result.CurrentPlayer);
        Assert.Single(result.MoveHistory);
        Assert.Equal(1, result.MoveHistory[0].MoveNumber);
        Assert.Equal(0, result.MoveHistory[0].CellIndex);
        Assert.True(result.CanUndo);
    }

    [Fact]
    public async Task MakeMove_InvalidCellIndex_ThrowsInvalidCellIndexException()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var moveCommand = new MakeMoveCommand(game.GameId, Player.X, 9); // Outside 0..8

        await Assert.ThrowsAsync<InvalidCellIndexException>(() => _gameService.MakeMoveAsync(moveCommand));
    }

    [Fact]
    public async Task MakeMove_OccupiedCell_ThrowsCellOccupiedException()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(game.GameId, Player.X, 4));

        await Assert.ThrowsAsync<CellOccupiedException>(() =>
            _gameService.MakeMoveAsync(new MakeMoveCommand(game.GameId, Player.O, 4)));
    }

    [Fact]
    public async Task MakeMove_InvalidTurn_ThrowsInvalidTurnException()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));

        // Player O tries to move first
        await Assert.ThrowsAsync<InvalidTurnException>(() =>
            _gameService.MakeMoveAsync(new MakeMoveCommand(game.GameId, Player.O, 0)));
    }

    [Fact]
    public async Task MakeMove_WhenGameAlreadyCompleted_ThrowsGameAlreadyCompletedException()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        // Play to win for X on row 0: 0, 1, 2
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 3));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 1));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 4));
        var terminalState = await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2));

        Assert.Equal(GameStatus.Won, terminalState.Status);
        Assert.Equal(Player.X, terminalState.Winner);

        // Attempt move after completion
        await Assert.ThrowsAsync<GameAlreadyCompletedException>(() =>
            _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 5)));
    }

    [Fact]
    public async Task MakeMove_WhenTerminalWin_DispatchesEventAndClearsPendingEvents()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 3));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 1));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 4));
        var result = await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2));

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal(Player.X, result.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, result.WinningCells);
        Assert.Equal(1, result.Scoreboard.XWins);
        Assert.Equal(0, result.Scoreboard.OWins);
        Assert.Equal(0, result.Scoreboard.Draws);

        // Verify pending events cleared on aggregate in repo
        var rawGame = await _gameRepository.GetByIdAsync(new GameId(id));
        Assert.NotNull(rawGame);
        Assert.Empty(rawGame.DomainEvents);
    }

    [Fact]
    public async Task MakeMove_WhenTerminalDraw_DispatchesEventAndClearsPendingEvents()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        // Draw sequence:
        // X: 0, 2, 3, 4, 7
        // O: 1, 5, 6, 8
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 1));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 5));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 3));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 6));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 4));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 8));
        var result = await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 7)); // Draw!

        Assert.Equal(GameStatus.Draw, result.Status);
        Assert.Null(result.Winner);
        Assert.Empty(result.WinningCells);
        Assert.Equal(0, result.Scoreboard.XWins);
        Assert.Equal(0, result.Scoreboard.OWins);
        Assert.Equal(1, result.Scoreboard.Draws);

        var rawGame = await _gameRepository.GetByIdAsync(new GameId(id));
        Assert.NotNull(rawGame);
        Assert.Empty(rawGame.DomainEvents);
    }

    [Fact]
    public async Task MakeMove_WhenDispatchThrows_RetainsPendingEvent()
    {
        var failingDispatcher = new DomainEventDispatcher();
        failingDispatcher.RegisterHandler<GameCompletedEvent>(_ =>
            throw new InvalidOperationException("Simulated dispatcher outage"));

        var gameService = new GameService(
            _gameRepository,
            _scoreboardRepository,
            failingDispatcher,
            _computerStrategy);

        var game = await gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        await gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 3));
        await gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 1));
        await gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 4));

        // Move that causes terminal win will trigger failing dispatcher
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2)));

        // Verify pending event is retained on the game aggregate for retry
        var rawGame = await _gameRepository.GetByIdAsync(new GameId(id));
        Assert.NotNull(rawGame);
        Assert.Single(rawGame.DomainEvents);
    }

    [Fact]
    public async Task MakeMove_InComputerMode_ExecutesBothHumanAndComputerMoves()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.Computer));
        var result = await _gameService.MakeMoveAsync(new MakeMoveCommand(game.GameId, Player.X, 0));

        // Human played 0, Computer should select center 4
        Assert.Equal(Player.X, result.Board[0]);
        Assert.Equal(Player.O, result.Board[4]);
        Assert.Equal(2, result.MoveHistory.Count);
        Assert.Equal(Player.X, result.CurrentPlayer); // Turn returned to human
        Assert.True(result.CanUndo);
    }

    [Fact]
    public async Task MakeMove_InComputerMode_WhenHumanWins_DoesNotInvokeComputer()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.Computer));
        var id = game.GameId;

        // Turn 1: Human 0, Computer 4
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        // Turn 2: Human 8, Computer 2
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 8));
        // Turn 3: Human 6, Computer 3
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 6));
        // Turn 4: Human completes row 2 at cell 7. Human wins!
        var result = await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 7));

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal(Player.X, result.Winner);
        Assert.Equal(7, result.MoveHistory.Count); // 4 human moves + 3 computer moves = 7
        Assert.Equal(1, result.Scoreboard.XWins);
    }

    [Fact]
    public async Task Undo_WhenAllowed_RevertsGameState()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(game.GameId, Player.X, 4));

        var undoneState = await _gameService.UndoAsync(new GameId(game.GameId));

        Assert.Null(undoneState.Board[4]);
        Assert.Equal(Player.X, undoneState.CurrentPlayer);
        Assert.Empty(undoneState.MoveHistory);
        Assert.False(undoneState.CanUndo);
    }

    [Fact]
    public async Task Undo_WhenCompletedGame_ThrowsCannotUndoException()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 3));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 1));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 4));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2)); // Win

        // Option A terminal lock enforces CannotUndoException
        await Assert.ThrowsAsync<CannotUndoException>(() =>
            _gameService.UndoAsync(new GameId(id)));
    }

    [Fact]
    public async Task ResetGame_PreservesGameId()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        var resetState = await _gameService.ResetGameAsync(new GameId(id));

        Assert.Equal(id, resetState.GameId);
    }

    [Fact]
    public async Task ResetGame_ClearsGameState()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 1));

        var resetState = await _gameService.ResetGameAsync(new GameId(id));

        Assert.Equal(GameStatus.InProgress, resetState.Status);
        Assert.Equal(Player.X, resetState.CurrentPlayer);
        Assert.Null(resetState.Winner);
        Assert.Empty(resetState.WinningCells);
        Assert.Empty(resetState.MoveHistory);
        Assert.All(resetState.Board, cell => Assert.Null(cell));
    }

    [Fact]
    public async Task ResetGame_PreservesScoreboard()
    {
        var game = await _gameService.CreateGameAsync(new CreateGameCommand(GameMode.TwoPlayer));
        var id = game.GameId;

        // Complete game 1
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 0));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 3));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 1));
        await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.O, 4));
        var winState = await _gameService.MakeMoveAsync(new MakeMoveCommand(id, Player.X, 2));

        Assert.Equal(1, winState.Scoreboard.XWins);

        // Reset game
        var resetState = await _gameService.ResetGameAsync(new GameId(id));

        // Scoreboard remains 1
        Assert.Equal(1, resetState.Scoreboard.XWins);
        Assert.Equal(1, resetState.Scoreboard.TotalGames);
    }

    [Fact]
    public async Task ResetGame_WhenEventPending_DispatchesBeforeReset()
    {
        // Manually place a game in repository that has a pending event
        var gameId = GameId.New();
        var game = new Game(gameId, GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won - has pending event

        Assert.Single(game.DomainEvents);
        await _gameRepository.SaveAsync(game);

        // Reset via GameService
        var resetState = await _gameService.ResetGameAsync(gameId);

        // Scoreboard was updated because pending event was drained before reset
        Assert.Equal(1, resetState.Scoreboard.XWins);
        Assert.Equal(GameStatus.InProgress, resetState.Status);

        // Aggregate retained in repo has cleared events
        var rawGame = await _gameRepository.GetByIdAsync(gameId);
        Assert.NotNull(rawGame);
        Assert.Empty(rawGame.DomainEvents);
    }

    [Fact]
    public async Task ResetGame_WhenDispatchFails_DoesNotReset()
    {
        var failingDispatcher = new DomainEventDispatcher();
        failingDispatcher.RegisterHandler<GameCompletedEvent>(_ =>
            throw new InvalidOperationException("Simulated reset dispatch failure"));

        var gameService = new GameService(
            _gameRepository,
            _scoreboardRepository,
            failingDispatcher,
            _computerStrategy);

        var gameId = GameId.New();
        var game = new Game(gameId, GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won
        await _gameRepository.SaveAsync(game);

        // Reset should fail and halt
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gameService.ResetGameAsync(gameId));

        // Game remains in Won state
        var rawGame = await _gameRepository.GetByIdAsync(gameId);
        Assert.NotNull(rawGame);
        Assert.Equal(GameStatus.Won, rawGame.Status);
    }

    [Fact]
    public async Task ResetGame_WhenDispatchFails_RetainsEvent()
    {
        var failingDispatcher = new DomainEventDispatcher();
        failingDispatcher.RegisterHandler<GameCompletedEvent>(_ =>
            throw new InvalidOperationException("Simulated reset dispatch failure"));

        var gameService = new GameService(
            _gameRepository,
            _scoreboardRepository,
            failingDispatcher,
            _computerStrategy);

        var gameId = GameId.New();
        var game = new Game(gameId, GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Won
        await _gameRepository.SaveAsync(game);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gameService.ResetGameAsync(gameId));

        // Game retains pending event
        var rawGame = await _gameRepository.GetByIdAsync(gameId);
        Assert.NotNull(rawGame);
        Assert.Single(rawGame.DomainEvents);
    }
}
