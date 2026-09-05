namespace TicTacToe.Application.Services;

using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Models;

/// <summary>
/// Application service contract for querying and resetting the session-level Scoreboard aggregate.
/// </summary>
public interface IScoreboardService
{
    /// <summary>
    /// Retrieves the current session scoreboard counters without side effects.
    /// </summary>
    Task<ScoreboardDto> GetScoreboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets scoreboard counters to 0 while preserving processed event history to guarantee idempotency.
    /// Does not alter active game state.
    /// </summary>
    Task<ScoreboardDto> ResetScoreboardAsync(CancellationToken cancellationToken = default);
}
