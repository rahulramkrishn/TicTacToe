namespace TicTacToe.Tests.Api;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ApiValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task HealthEndpoint_Returns200OK()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task MakeMove_NegativeCellIndex_Returns400BadRequest_WithProblemDetails()
    {
        var gameId = await CreateGameAsync();

        var response = await _client.PostAsJsonAsync($"/api/games/{gameId}/moves", new
        {
            player = "X",
            cellIndex = -1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("https://api.tictactoe.com/errors/invalid-cell-index", root.GetProperty("type").GetString());
        Assert.Equal("INVALID_CELL_INDEX", root.GetProperty("code").GetString());
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.True(root.TryGetProperty("traceId", out var traceId) && !string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task MakeMove_CellIndexGreaterThan8_Returns400BadRequest_WithProblemDetails()
    {
        var gameId = await CreateGameAsync();

        var response = await _client.PostAsJsonAsync($"/api/games/{gameId}/moves", new
        {
            player = "X",
            cellIndex = 9
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("https://api.tictactoe.com/errors/invalid-cell-index", root.GetProperty("type").GetString());
        Assert.Equal("INVALID_CELL_INDEX", root.GetProperty("code").GetString());
        Assert.Equal(400, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task MakeMove_UnknownPropertyRowColumn_Returns400BadRequest_WithMalformedRequest()
    {
        var gameId = await CreateGameAsync();

        var content = new StringContent(
            "{\"player\":\"X\",\"cellIndex\":4,\"row\":1,\"column\":1}",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync($"/api/games/{gameId}/moves", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("https://api.tictactoe.com/errors/malformed-request", root.GetProperty("type").GetString());
        Assert.Equal("MALFORMED_REQUEST", root.GetProperty("code").GetString());
        Assert.Equal(400, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task CreateGame_UnknownProperty_Returns400BadRequest_WithMalformedRequest()
    {
        var content = new StringContent(
            "{\"mode\":\"TwoPlayer\",\"unexpectedProperty\":123}",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/games", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("https://api.tictactoe.com/errors/malformed-request", root.GetProperty("type").GetString());
        Assert.Equal("MALFORMED_REQUEST", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task MakeMove_MalformedJson_Returns400BadRequest()
    {
        var gameId = await CreateGameAsync();

        var content = new StringContent("{ not valid json }", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"/api/games/{gameId}/moves", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("MALFORMED_REQUEST", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetGame_NonExistentGameId_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/games/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        var root = doc.RootElement;

        Assert.Equal("https://api.tictactoe.com/errors/game-not-found", root.GetProperty("type").GetString());
        Assert.Equal("GAME_NOT_FOUND", root.GetProperty("code").GetString());
        Assert.Equal(404, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task MakeMove_NonExistentGameId_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/api/games/{nonExistentId}/moves", new
        {
            player = "X",
            cellIndex = 0
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        Assert.Equal("GAME_NOT_FOUND", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task NonExistentRoute_Returns404NotFound()
    {
        var response = await _client.GetAsync("/api/non-existent-endpoint");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnsupportedVerb_Returns405MethodNotAllowed()
    {
        var response = await _client.DeleteAsync("/api/games");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task CustomCorrelationId_IsPreservedInResponseHeaderAndProblemDetails()
    {
        var customCorrelationId = "custom-trace-12345";
        var nonExistentId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/games/{nonExistentId}");
        request.Headers.Add("X-Correlation-Id", customCorrelationId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Contains(customCorrelationId, values);

        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        Assert.NotNull(doc);
        Assert.Equal(customCorrelationId, doc.RootElement.GetProperty("traceId").GetString());
    }

    private async Task<Guid> CreateGameAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        return Guid.Parse(doc!.RootElement.GetProperty("gameId").GetString()!);
    }
}
