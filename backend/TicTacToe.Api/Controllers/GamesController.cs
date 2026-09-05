namespace TicTacToe.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Application.Models;
using TicTacToe.Application.Services;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// REST API controller exposing Game aggregate orchestration use cases.
/// Strictly delegates all business and domain logic to IGameService.
/// </summary>
[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
    }

    /// <summary>
    /// Creates a new game session with the requested mode.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGame(
        [FromBody] CreateGameRequest? request,
        CancellationToken cancellationToken)
    {
        var mode = request?.Mode ?? GameMode.TwoPlayer;
        var command = new CreateGameCommand(mode);
        var result = await _gameService.CreateGameAsync(command, cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetGame),
            new { gameId = result.GameId },
            result);
    }

    /// <summary>
    /// Retrieves the authoritative current state of a game.
    /// Strictly side-effect free.
    /// </summary>
    [HttpGet("{gameId:guid}")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(
        [FromRoute] Guid gameId,
        CancellationToken cancellationToken)
    {
        var result = await _gameService.GetGameAsync(new GameId(gameId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Executes a player move. In Computer mode, automatically triggers the computer's response.
    /// </summary>
    [HttpPost("{gameId:guid}/moves")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MakeMove(
        [FromRoute] Guid gameId,
        [FromBody] MakeMoveRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { code = "MALFORMED_REQUEST", message = "Request body is required." });
        }

        var command = new MakeMoveCommand(gameId, request.Player, request.CellIndex);
        var result = await _gameService.MakeMoveAsync(command, cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Reverts the previous move (1 move in TwoPlayer, 2 moves in Computer mode).
    /// </summary>
    [HttpPost("{gameId:guid}/undo")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Undo(
        [FromRoute] Guid gameId,
        CancellationToken cancellationToken)
    {
        var result = await _gameService.UndoAsync(new GameId(gameId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Resets the game to initial play state while preserving the same gameId.
    /// Drains pending completion events before reset and preserves the session scoreboard.
    /// </summary>
    [HttpPost("{gameId:guid}/reset")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetGame(
        [FromRoute] Guid gameId,
        CancellationToken cancellationToken)
    {
        var result = await _gameService.ResetGameAsync(new GameId(gameId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
