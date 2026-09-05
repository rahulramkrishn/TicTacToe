namespace TicTacToe.Domain.Repositories;

using System.Threading.Tasks;
using TicTacToe.Domain.Entities;

/// <summary>
/// Domain repository contract for managing the session-level Scoreboard aggregate root.
/// </summary>
public interface IScoreboardRepository
{
    /// <summary>
    /// Asynchronously retrieves the authoritative Scoreboard aggregate for the session.
    /// </summary>
    Task<Scoreboard> GetScoreboardAsync();

    /// <summary>
    /// Asynchronously persists updates to the Scoreboard aggregate.
    /// </summary>
    Task SaveScoreboardAsync(Scoreboard scoreboard);
}
