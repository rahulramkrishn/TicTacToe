namespace TicTacToe.Infrastructure.Repositories;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Repositories;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// In-memory repository providing thread-safe storage and retrieval of authoritative Game aggregate roots.
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<GameId, Game> _games = new();

    /// <inheritdoc />
    public Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        _games.TryGetValue(gameId, out var game);
        return Task.FromResult(game);
    }

    /// <inheritdoc />
    public Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        _games[game.Id] = game;
        return Task.CompletedTask;
    }
}
