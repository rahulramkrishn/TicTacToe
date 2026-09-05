namespace TicTacToe.Tests.Api;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Application.Models;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class GamesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public GamesApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    [Fact]
    public async Task CreateGame_Returns201Created_WithLocationHeaderAndGameState()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var state = await response.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.NotEqual(Guid.Empty, state.GameId);
        Assert.Equal($"/api/games/{state.GameId}", response.Headers.Location.AbsolutePath);
        Assert.Equal("X", state.CurrentPlayer.ToString());
        Assert.Equal("InProgress", state.Status.ToString());
        Assert.Equal(9, state.Board.Count);
        Assert.All(state.Board, cell => Assert.Null(cell));
    }

    [Fact]
    public async Task GetGame_WhenGameExists_Returns200OK_WithAuthoritativeState()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/games/{created.GameId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrieved = await getResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(retrieved);
        Assert.Equal(created.GameId, retrieved.GameId);
        Assert.Equal(created.Status, retrieved.Status);
    }

    [Fact]
    public async Task GetGame_WhenGameNotFound_Returns404NotFound_WithProblemDetails()
    {
        var missingId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/games/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("GAME_NOT_FOUND", root.GetProperty("code").GetString());
        Assert.Equal("https://api.tictactoe.com/errors/game-not-found", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId) && !string.IsNullOrEmpty(traceId.GetString()));
    }

    [Fact]
    public async Task MakeMove_ValidMove_Returns200OK_WithUpdatedBoardAndAlternatingTurn()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        var moveResponse = await _client.PostAsJsonAsync(
            $"/api/games/{created.GameId}/moves",
            new { player = "X", cellIndex = 0 });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        var state = await moveResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.Equal(Player.X, state.Board[0]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.MoveHistory);
        Assert.True(state.CanUndo);
    }

    [Fact]
    public async Task MakeMove_InvalidCellIndex_Returns400BadRequest_WithProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        var moveResponse = await _client.PostAsJsonAsync(
            $"/api/games/{created.GameId}/moves",
            new { player = "X", cellIndex = 15 });

        Assert.Equal(HttpStatusCode.BadRequest, moveResponse.StatusCode);
        Assert.Equal("application/problem+json", moveResponse.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await moveResponse.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("INVALID_CELL_INDEX", root.GetProperty("code").GetString());
        Assert.Equal("https://api.tictactoe.com/errors/invalid-cell-index", root.GetProperty("type").GetString());
    }

    [Fact]
    public async Task MakeMove_OccupiedCell_Returns409Conflict_WithProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        await _client.PostAsJsonAsync($"/api/games/{created.GameId}/moves", new { player = "X", cellIndex = 4 });

        var duplicateMoveResponse = await _client.PostAsJsonAsync(
            $"/api/games/{created.GameId}/moves",
            new { player = "O", cellIndex = 4 });

        Assert.Equal(HttpStatusCode.Conflict, duplicateMoveResponse.StatusCode);
        Assert.Equal("application/problem+json", duplicateMoveResponse.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await duplicateMoveResponse.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("CELL_OCCUPIED", root.GetProperty("code").GetString());
        Assert.Equal("https://api.tictactoe.com/errors/cell-occupied", root.GetProperty("type").GetString());
    }

    [Fact]
    public async Task MakeMove_InvalidTurn_Returns409Conflict_WithProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        // Player O tries to move first
        var response = await _client.PostAsJsonAsync(
            $"/api/games/{created.GameId}/moves",
            new { player = "O", cellIndex = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("INVALID_TURN", root.GetProperty("code").GetString());
        Assert.Equal("https://api.tictactoe.com/errors/invalid-turn", root.GetProperty("type").GetString());
    }

    [Fact]
    public async Task MakeMove_CompletedGame_Returns409Conflict_WithProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var id = created.GameId;

        // X wins row 0: 0, 1, 2
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 0 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 3 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 1 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 4 });
        var winResponse = await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 2 });

        var winState = await winResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(winState);
        Assert.Equal(GameStatus.Won, winState.Status);

        // Subsequent move must be rejected
        var postMove = await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 5 });
        Assert.Equal(HttpStatusCode.Conflict, postMove.StatusCode);
        using var doc = JsonDocument.Parse(await postMove.Content.ReadAsStringAsync());
        Assert.Equal("GAME_ALREADY_COMPLETED", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task MakeMove_InComputerMode_Returns200OK_WithBothMovesExecuted()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "Computer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        // Human plays corner 0, computer chooses center 4
        var moveResponse = await _client.PostAsJsonAsync(
            $"/api/games/{created.GameId}/moves",
            new { player = "X", cellIndex = 0 });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        var state = await moveResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.Equal(Player.X, state.Board[0]);
        Assert.Equal(Player.O, state.Board[4]);
        Assert.Equal(2, state.MoveHistory.Count);
        Assert.Equal(Player.X, state.CurrentPlayer); // Turn returned to human
    }

    [Fact]
    public async Task Undo_WhenAllowed_Returns200OK_WithRevertedState()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        await _client.PostAsJsonAsync($"/api/games/{created.GameId}/moves", new { player = "X", cellIndex = 4 });

        var undoResponse = await _client.PostAsync($"/api/games/{created.GameId}/undo", null);
        Assert.Equal(HttpStatusCode.OK, undoResponse.StatusCode);

        var state = await undoResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.Null(state.Board[4]);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Empty(state.MoveHistory);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public async Task Undo_WhenCompletedGame_Returns409Conflict_WithProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var id = created.GameId;

        // X wins row 0: 0, 1, 2
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 0 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 3 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 1 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 4 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 2 });

        var undoResponse = await _client.PostAsync($"/api/games/{id}/undo", null);
        Assert.Equal(HttpStatusCode.Conflict, undoResponse.StatusCode);
        using var doc = JsonDocument.Parse(await undoResponse.Content.ReadAsStringAsync());
        Assert.Equal("CANNOT_UNDO", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ResetGame_PreservesGameId_Returns200OK_WithFreshPlayState()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var id = created.GameId;

        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 0 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 1 });

        var resetResponse = await _client.PostAsync($"/api/games/{id}/reset", null);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var state = await resetResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.Equal(id, state.GameId);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Null(state.Winner);
        Assert.Empty(state.WinningCells);
        Assert.Empty(state.MoveHistory);
        Assert.All(state.Board, cell => Assert.Null(cell));
    }

    [Fact]
    public async Task ResetGame_WhenTerminalMoveCompleted_PreservesScoreboardWinAfterReset()
    {
        var initScoreResp = await _client.GetAsync("/api/scoreboard");
        var initScore = await initScoreResp.Content.ReadFromJsonAsync<ScoreboardDto>(_jsonOptions);
        var expectedXWins = (initScore?.XWins ?? 0) + 1;

        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var id = created.GameId;

        // X wins row 0: 0, 1, 2
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 0 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 3 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 1 });
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "O", cellIndex = 4 });
        var winResp = await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 2 });

        var winState = await winResp.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(winState);
        Assert.Equal(expectedXWins, winState.Scoreboard.XWins);

        // Reset game
        var resetResp = await _client.PostAsync($"/api/games/{id}/reset", null);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        var resetState = await resetResp.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(resetState);
        Assert.Equal(id, resetState.GameId);
        Assert.Equal(expectedXWins, resetState.Scoreboard.XWins);
        Assert.All(resetState.Board, cell => Assert.Null(cell));

        // Verify session scoreboard persists the win
        var scoreResp = await _client.GetAsync("/api/scoreboard");
        var scoreboard = await scoreResp.Content.ReadFromJsonAsync<ScoreboardDto>(_jsonOptions);
        Assert.NotNull(scoreboard);
        Assert.Equal(expectedXWins, scoreboard.XWins);
    }

    [Fact]
    public async Task Lifecycle_CreateMoveGetReset_OperatesOnSameAuthoritativeAggregate()
    {
        // 1. Create Game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var id = created.GameId;

        // 2. Make Move
        await _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player = "X", cellIndex = 4 });

        // 3. GET game -> verifies cell 4 is occupied
        var getResp = await _client.GetAsync($"/api/games/{id}");
        var getState = await getResp.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(getState);
        Assert.Equal(Player.X, getState.Board[4]);

        // 4. Reset game
        await _client.PostAsync($"/api/games/{id}/reset", null);

        // 5. GET game again -> verifies board empty, same gameId
        var getResetResp = await _client.GetAsync($"/api/games/{id}");
        var getResetState = await getResetResp.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(getResetState);
        Assert.Equal(id, getResetState.GameId);
        Assert.Null(getResetState.Board[4]);
    }
}
