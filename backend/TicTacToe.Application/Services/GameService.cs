namespace TicTacToe.Application.Services;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Events;
using TicTacToe.Application.Exceptions;
using TicTacToe.Application.Mappings;
using TicTacToe.Application.Models;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Repositories;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Orchestrates Game aggregate use cases, domain event dispatching, and persistence.
/// Enforces per-GameId synchronization to serialize concurrent commands targeting the same game.
/// Delegates all domain business rules to the Domain layer.
/// <para>
/// Architectural Note (Lock Lifecycle): GameService maintains per-GameId locks for the lifetime of
/// the application process. Lock cleanup is intentionally deferred because games are retained in the
/// in-memory repository for the process lifetime. This is an explicit limitation of the in-memory
/// assessment architecture and will be revisited if persistent storage is introduced.
/// </para>
/// <para>
/// Architectural Note (Transaction Boundary &amp; Failure Semantics): For the current single-process
/// in-memory architecture, aggregate mutation, event dispatch, event clearing, and repository persistence
/// are treated as one application-level operation. ClearDomainEvents() occurs only after successful event
/// dispatch, and persistence failure must not be silently swallowed. The design does not claim distributed
/// transactional guarantees.
/// </para>
/// </summary>
public sealed class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IScoreboardRepository _scoreboardRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IComputerMoveStrategy _computerStrategy;
    private readonly ConcurrentDictionary<GameId, SemaphoreSlim> _gameLocks = new();

    public GameService(
        IGameRepository gameRepository,
        IScoreboardRepository scoreboardRepository,
        IDomainEventDispatcher eventDispatcher,
        IComputerMoveStrategy computerStrategy)
    {
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _scoreboardRepository = scoreboardRepository ?? throw new ArgumentNullException(nameof(scoreboardRepository));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
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

            if (game.Mode == GameMode.TwoPlayer)
            {
                game.MakeMove(command.Player, cellIndex);
            }
            else if (game.Mode == GameMode.Computer)
            {
                // Orchestrate human move followed by computer response as a logical turn
                game.ExecuteTurn(cellIndex, _computerStrategy);
            }

            // Domain Event Lifecycle: Dispatch pending events and clear strictly upon success
            if (game.DomainEvents.Count > 0)
            {
                var events = game.DomainEvents.ToList();
                await _eventDispatcher.DispatchAsync(events).ConfigureAwait(false);
                game.ClearDomainEvents();
            }

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

            // Invariant (P009.1.1): Drain pending events before resetting so completion facts are never lost.
            // If dispatch throws, reset halts and ClearDomainEvents is NOT called.
            if (game.DomainEvents.Count > 0)
            {
                var events = game.DomainEvents.ToList();
                await _eventDispatcher.DispatchAsync(events).ConfigureAwait(false);
                game.ClearDomainEvents();
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
