namespace TicTacToe.Application.Services;

using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Models;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Application service contract for orchestrating Game aggregate lifecycle and use cases.
/// Transport-neutral and independent of ASP.NET Core or HTTP abstractions.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Creates a new game session with the requested mode.
    /// </summary>
    Task<GameStateDto> CreateGameAsync(CreateGameCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the authoritative game state without side effects.
    /// </summary>
    Task<GameStateDto> GetGameAsync(GameId gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a player move, dispatches domain events on terminal transition, updates the scoreboard, and saves aggregate state.
    /// In Computer Mode, coordinates the subsequent computer response as part of the logical turn.
    /// </summary>
    Task<GameStateDto> MakeMoveAsync(MakeMoveCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts the most recent move (TwoPlayer) or move pair (Computer), respecting Option A terminal restrictions.
    /// </summary>
    Task<GameStateDto> UndoAsync(GameId gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the game play state while preserving the GameId (FR-12).
    /// Drains any pending domain events prior to resetting so that undelivered completion facts are never lost.
    /// </summary>
    Task<GameStateDto> ResetGameAsync(GameId gameId, CancellationToken cancellationToken = default);
}
