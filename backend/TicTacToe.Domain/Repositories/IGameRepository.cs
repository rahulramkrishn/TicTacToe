namespace TicTacToe.Domain.Repositories;

using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Domain repository abstraction for loading and persisting the Game aggregate root.
/// </summary>
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(GameId id, CancellationToken cancellationToken = default);
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
