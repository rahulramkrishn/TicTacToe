namespace TicTacToe.Application.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Mappings;
using TicTacToe.Application.Models;
using TicTacToe.Domain.Repositories;

/// <summary>
/// Orchestrates Scoreboard aggregate use cases.
/// Operates on the authoritative session scoreboard repository.
/// </summary>
public sealed class ScoreboardService : IScoreboardService
{
    private readonly IScoreboardRepository _scoreboardRepository;

    public ScoreboardService(IScoreboardRepository scoreboardRepository)
    {
        _scoreboardRepository = scoreboardRepository ?? throw new ArgumentNullException(nameof(scoreboardRepository));
    }

    /// <inheritdoc />
    public async Task<ScoreboardDto> GetScoreboardAsync(CancellationToken cancellationToken = default)
    {
        var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
        return scoreboard.ToDto();
    }

    /// <inheritdoc />
    public async Task<ScoreboardDto> ResetScoreboardAsync(CancellationToken cancellationToken = default)
    {
        var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
        scoreboard.Reset();
        await _scoreboardRepository.SaveScoreboardAsync(scoreboard).ConfigureAwait(false);
        return scoreboard.ToDto();
    }
}
