namespace TicTacToe.Tests.Api;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ApiSerializationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSerializationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GameState_SerializesEnumsAsStrings_AndUsesCamelCaseProperties()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        // Verify camelCase property names
        Assert.True(root.TryGetProperty("gameId", out var gameIdProp));
        Assert.True(root.TryGetProperty("board", out var boardProp));
        Assert.True(root.TryGetProperty("currentPlayer", out var currentPlayerProp));
        Assert.True(root.TryGetProperty("mode", out var modeProp));
        Assert.True(root.TryGetProperty("status", out var statusProp));
        Assert.True(root.TryGetProperty("winner", out var winnerProp));
        Assert.True(root.TryGetProperty("winningCells", out var winningCellsProp));
        Assert.True(root.TryGetProperty("moveHistory", out var moveHistoryProp));
        Assert.True(root.TryGetProperty("scoreboard", out var scoreboardProp));

        // Verify string enum values
        Assert.Equal("TwoPlayer", modeProp.GetString());
        Assert.Equal("X", currentPlayerProp.GetString());
        Assert.Equal("InProgress", statusProp.GetString());
        Assert.Equal(JsonValueKind.Null, winnerProp.ValueKind);

        // Verify board items are null initially
        Assert.Equal(9, boardProp.GetArrayLength());
        foreach (var cell in boardProp.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Null, cell.ValueKind);
        }

        // Verify scoreboard camelCase properties
        Assert.True(scoreboardProp.TryGetProperty("xWins", out var xWinsProp));
        Assert.True(scoreboardProp.TryGetProperty("oWins", out var oWinsProp));
        Assert.True(scoreboardProp.TryGetProperty("draws", out var drawsProp));
        Assert.Equal(JsonValueKind.Number, xWinsProp.ValueKind);
        Assert.Equal(JsonValueKind.Number, oWinsProp.ValueKind);
        Assert.Equal(JsonValueKind.Number, drawsProp.ValueKind);
    }

    [Fact]
    public async Task MoveHistory_SerializesPlayerAsString_AndCellIndexAsNumber()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var createDoc = await createResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var id = createDoc!.RootElement.GetProperty("gameId").GetString();

        var moveResponse = await _client.PostAsJsonAsync($"/api/games/{id}/moves", new
        {
            player = "X",
            cellIndex = 4
        });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var rawJson = await moveResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        // Verify Board contains string "X"
        var board = root.GetProperty("board");
        Assert.Equal("X", board[4].GetString());

        // Verify MoveHistory entry
        var moveHistory = root.GetProperty("moveHistory");
        Assert.Equal(1, moveHistory.GetArrayLength());
        var firstMove = moveHistory[0];

        Assert.True(firstMove.TryGetProperty("moveNumber", out var moveNumberProp));
        Assert.True(firstMove.TryGetProperty("player", out var playerProp));
        Assert.True(firstMove.TryGetProperty("cellIndex", out var cellIndexProp));

        Assert.Equal(1, moveNumberProp.GetInt32());
        Assert.Equal("X", playerProp.GetString());
        Assert.Equal(4, cellIndexProp.GetInt32());
    }

    [Fact]
    public async Task ScoreboardEndpoint_SerializesCamelCaseProperties()
    {
        var response = await _client.GetAsync("/api/scoreboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("xWins", out _));
        Assert.True(root.TryGetProperty("oWins", out _));
        Assert.True(root.TryGetProperty("draws", out _));
    }
}
