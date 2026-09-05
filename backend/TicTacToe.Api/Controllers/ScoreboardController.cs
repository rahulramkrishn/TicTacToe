namespace TicTacToe.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Models;
using TicTacToe.Application.Services;

/// <summary>
/// REST API controller exposing session scoreboard inspection and reset use cases.
/// Strictly delegates all state access to IScoreboardService.
/// </summary>
[ApiController]
[Route("api/scoreboard")]
[Produces("application/json")]
public sealed class ScoreboardController : ControllerBase
{
    private readonly IScoreboardService _scoreboardService;

    public ScoreboardController(IScoreboardService scoreboardService)
    {
        _scoreboardService = scoreboardService ?? throw new ArgumentNullException(nameof(scoreboardService));
    }

    /// <summary>
    /// Retrieves current session scoreboard metrics.
    /// Strictly side-effect free.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ScoreboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScoreboard(CancellationToken cancellationToken)
    {
        var result = await _scoreboardService.GetScoreboardAsync(cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Resets session scoreboard counters to zero while preserving event deduplication history.
    /// Does not alter active games.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ScoreboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetScoreboard(CancellationToken cancellationToken)
    {
        var result = await _scoreboardService.ResetScoreboardAsync(cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
