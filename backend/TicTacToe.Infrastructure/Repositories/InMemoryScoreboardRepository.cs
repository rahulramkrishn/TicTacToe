namespace TicTacToe.Infrastructure.Repositories;

using System;
using System.Threading.Tasks;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Repositories;

/// <summary>
/// In-memory repository providing access to the authoritative session Scoreboard aggregate root.
/// Thread-safe because the underlying Scoreboard aggregate encapsulates internal thread synchronization.
/// </summary>
public sealed class InMemoryScoreboardRepository : IScoreboardRepository
{
    private readonly Scoreboard _scoreboard;

    public InMemoryScoreboardRepository()
    {
        _scoreboard = new Scoreboard();
    }

    public InMemoryScoreboardRepository(Scoreboard initialScoreboard)
    {
        _scoreboard = initialScoreboard ?? throw new ArgumentNullException(nameof(initialScoreboard));
    }

    /// <inheritdoc />
    public Task<Scoreboard> GetScoreboardAsync()
    {
        return Task.FromResult(_scoreboard);
    }

    /// <inheritdoc />
    public Task SaveScoreboardAsync(Scoreboard scoreboard)
    {
        // In-memory reference is authoritative and mutated in-place under lock.
        return Task.CompletedTask;
    }
}
