namespace TicTacToe.Tests.Api;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Application.Models;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class ApiConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    [Fact]
    public async Task SameGame_TenParallelRequestsToSameCell_ExactlyOneSucceeds_NineFailWith409()
    {
        // 1. Create a game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var gameId = created.GameId;

        // 2. Fire 10 parallel requests to claim cell 4
        const int concurrentRequests = 10;
        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ =>
            _client.PostAsJsonAsync($"/api/games/{gameId}/moves", new
            {
                player = "X",
                cellIndex = 4
            })
        ).ToList();

        var responses = await Task.WhenAll(tasks);

        // 3. Exactly 1 must be 200 OK
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(9, conflictCount);

        // 4. Verify authoritative state
        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        var state = await getResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);
        Assert.Equal(Player.X, state.Board[4]);
        Assert.Single(state.MoveHistory);
        Assert.Equal(Player.O, state.CurrentPlayer);
    }

    [Fact]
    public async Task ComputerMode_ParallelMoves_MaintainsAtomicTurnIntegrity()
    {
        // 1. Create a Computer mode game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "Computer" });
        var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(created);
        var gameId = created.GameId;

        // 2. Attempt 8 parallel moves on different cells for Player X
        var cellIndices = Enumerable.Range(0, 8).ToList();
        var tasks = cellIndices.Select(idx =>
            _client.PostAsJsonAsync($"/api/games/{gameId}/moves", new
            {
                player = "X",
                cellIndex = idx
            })
        ).ToList();

        var responses = await Task.WhenAll(tasks);

        // At least one move succeeded, and any conflicting moves returned 409
        var successResponses = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToList();
        var conflictResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        Assert.NotEmpty(successResponses);
        Assert.Equal(responses.Length, successResponses.Count + conflictResponses.Count);

        // Verify game state is coherent: board move count must equal move history count
        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        var state = await getResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
        Assert.NotNull(state);

        var boardOccupiedCount = state.Board.Count(c => c.HasValue);
        Assert.Equal(boardOccupiedCount, state.MoveHistory.Count);

        // In computer mode, after each human move that doesn't win immediately, the computer responds,
        // so total moves must be even (unless X won or board filled).
        if (state.Status == GameStatus.InProgress)
        {
            Assert.True(state.MoveHistory.Count % 2 == 0);
            Assert.Equal(Player.X, state.CurrentPlayer);
        }
    }

    [Fact]
    public async Task CrossGame_ParallelMoves_ExecuteIndependentlyWithoutInterference()
    {
        // 1. Create 5 distinct games
        var gameIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var createResponse = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
            var created = await createResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
            Assert.NotNull(created);
            gameIds.Add(created.GameId);
        }

        // 2. Play cell 4 in all 5 games concurrently
        var tasks = gameIds.Select(id =>
            _client.PostAsJsonAsync($"/api/games/{id}/moves", new
            {
                player = "X",
                cellIndex = 4
            })
        ).ToList();

        var responses = await Task.WhenAll(tasks);

        // 3. All 5 should succeed with 200 OK
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // 4. Verify all 5 games have cell 4 occupied
        foreach (var id in gameIds)
        {
            var getResponse = await _client.GetAsync($"/api/games/{id}");
            var state = await getResponse.Content.ReadFromJsonAsync<GameStateDto>(_jsonOptions);
            Assert.NotNull(state);
            Assert.Equal(Player.X, state.Board[4]);
            Assert.Single(state.MoveHistory);
        }
    }
}
