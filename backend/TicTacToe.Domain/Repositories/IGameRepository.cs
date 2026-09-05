namespace TicTacToe.Domain.Repositories;

using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Domain repository contract for managing Game aggregate root retrieval and persistence.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Asynchronously retrieves the authoritative Game aggregate by its unique identity.
    /// Returns null if no game with the specified identity exists.
    /// </summary>
    Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously persists the Game aggregate.
    /// </summary>
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
