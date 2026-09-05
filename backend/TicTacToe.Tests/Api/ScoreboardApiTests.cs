namespace TicTacToe.Tests.Api;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Application.Models;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ScoreboardApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ScoreboardApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    [Fact]
    public async Task GetScoreboard_Returns200OK_WithCurrentScores()
    {
        var response = await _client.GetAsync("/api/scoreboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardDto>(_jsonOptions);
        Assert.NotNull(scoreboard);
        Assert.True(scoreboard.TotalGames >= 0);
    }

    [Fact]
    public async Task ResetScoreboard_Returns200OK_WithZeroedScores()
    {
        var response = await _client.PostAsync("/api/scoreboard/reset", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardDto>(_jsonOptions);
        Assert.NotNull(scoreboard);
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
        Assert.Equal(0, scoreboard.TotalGames);
    }

    [Fact]
    public async Task ResetScoreboard_DoesNotModifyActiveGames()
    {
        // 1. Create a game and place a mark
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);

        await _client.PostAsJsonAsync($"/api/games/{created.GameId}/moves", new { player = "X", cellIndex = 4 });

        // 2. Reset scoreboard
        var resetScoreResp = await _client.PostAsync("/api/scoreboard/reset", null);
        Assert.Equal(HttpStatusCode.OK, resetScoreResp.StatusCode);

        // 3. Verify active game remains intact
        var getResp = await _client.GetAsync($"/api/games/{created.GameId}");
        var gameState = await getResp.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(gameState);
        Assert.Equal(Player.X, gameState.Board[4]);
        Assert.Equal(Player.O, gameState.CurrentPlayer);
        Assert.Single(gameState.MoveHistory);
    }
}
