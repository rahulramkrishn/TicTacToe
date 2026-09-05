namespace TicTacToe.Application.Services;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Exceptions;
using TicTacToe.Application.Mappings;
using TicTacToe.Application.Models;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Repositories;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Orchestrates Game aggregate use cases, synchronous scoreboard consequences, and persistence.
/// Enforces per-GameId synchronization to serialize concurrent commands targeting the same game.
/// Delegates all domain business rules to the Domain layer.
/// <para>
/// Architectural Note (Lock Lifecycle): GameService maintains per-GameId locks for the lifetime of
/// the application process. Lock cleanup is intentionally deferred because games are retained in the
/// in-memory repository for the process lifetime. This is an explicit limitation of the in-memory
/// assessment architecture and will be revisited if persistent storage is introduced.
/// </para>
/// <para>
/// Architectural Note (Persistence Boundary &amp; Failure Semantics): P012 does not introduce distributed
/// transaction guarantees. Game and scoreboard persistence are performed synchronously within the Application
/// service and are assumed to share the same process/storage consistency boundary.
/// </para>
/// </summary>
public sealed class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IScoreboardRepository _scoreboardRepository;
    private readonly IComputerMoveStrategy _computerStrategy;
    private readonly ConcurrentDictionary<GameId, SemaphoreSlim> _gameLocks = new();

    public GameService(
        IGameRepository gameRepository,
        IScoreboardRepository scoreboardRepository,
        IComputerMoveStrategy computerStrategy)
    {
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _scoreboardRepository = scoreboardRepository ?? throw new ArgumentNullException(nameof(scoreboardRepository));
        _computerStrategy = computerStrategy ?? throw new ArgumentNullException(nameof(computerStrategy));
    }

    /// <inheritdoc />
    public async Task<GameStateDto> CreateGameAsync(CreateGameCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var game = Game.Create(command.Mode);
        await _gameRepository.SaveAsync(game, cancellationToken).ConfigureAwait(false);

        var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
        return game.ToDto(scoreboard);
    }

    /// <inheritdoc />
    public async Task<GameStateDto> GetGameAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken).ConfigureAwait(false);
        if (game == null)
        {
            throw new GameNotFoundException(gameId.Value);
        }

        var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
        return game.ToDto(scoreboard);
    }

    /// <inheritdoc />
    public async Task<GameStateDto> MakeMoveAsync(MakeMoveCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var gameId = new GameId(command.GameId);
        var cellIndex = new CellIndex(command.CellIndex);

        var lockObj = _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken).ConfigureAwait(false);
            if (game == null)
            {
                throw new GameNotFoundException(gameId.Value);
            }

            var previousStatus = game.Status;

            if (game.Mode == GameMode.TwoPlayer)
            {
                game.MakeMove(command.Player, cellIndex);
            }
            else if (game.Mode == GameMode.Computer)
            {
                // Orchestrate human move followed by computer response as a logical turn
                game.ExecuteTurn(cellIndex, _computerStrategy);
            }

            // Explicit Terminal Transition Detection (One transition = One scoreboard update)
            if (previousStatus == GameStatus.InProgress && game.Status == GameStatus.Won && game.Winner.HasValue)
            {
                var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
                scoreboard.RecordWin(game.Winner.Value);
                await _scoreboardRepository.SaveScoreboardAsync(scoreboard).ConfigureAwait(false);
            }
            else if (previousStatus == GameStatus.InProgress && game.Status == GameStatus.Draw)
            {
                var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
                scoreboard.RecordDraw();
                await _scoreboardRepository.SaveScoreboardAsync(scoreboard).ConfigureAwait(false);
            }

            await _gameRepository.SaveAsync(game, cancellationToken).ConfigureAwait(false);

            var currentScoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
            return game.ToDto(currentScoreboard);
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <inheritdoc />
    public async Task<GameStateDto> UndoAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var lockObj = _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken).ConfigureAwait(false);
            if (game == null)
            {
                throw new GameNotFoundException(gameId.Value);
            }

            game.Undo();
            await _gameRepository.SaveAsync(game, cancellationToken).ConfigureAwait(false);

            var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
            return game.ToDto(scoreboard);
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <inheritdoc />
    public async Task<GameStateDto> ResetGameAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var lockObj = _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken).ConfigureAwait(false);
            if (game == null)
            {
                throw new GameNotFoundException(gameId.Value);
            }

            game.Reset();
            await _gameRepository.SaveAsync(game, cancellationToken).ConfigureAwait(false);

            var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
            return game.ToDto(scoreboard);
        }
        finally
        {
            lockObj.Release();
        }
    }
}
