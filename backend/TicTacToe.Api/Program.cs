using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicTacToe.Api.Middleware;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Repositories;
using TicTacToe.Domain.Services;
using TicTacToe.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Controllers with strict JSON deserialization and string enum formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var traceId = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            if (context.HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) &&
                !string.IsNullOrWhiteSpace(correlationHeader))
            {
                traceId = correlationHeader.ToString();
            }

            context.HttpContext.Response.Headers["X-Correlation-Id"] = traceId;

            var problemDetails = new
            {
                type = "https://api.tictactoe.com/errors/malformed-request",
                title = "Malformed Request",
                status = StatusCodes.Status400BadRequest,
                detail = "The request body is malformed or contains invalid/unknown properties.",
                instance = context.HttpContext.Request.Path.Value,
                code = "MALFORMED_REQUEST",
                traceId
            };

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// Configuration-driven CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Authoritative In-Memory Repositories
builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
builder.Services.AddSingleton<IScoreboardRepository, InMemoryScoreboardRepository>();

// Pure Domain Services & Strategies
builder.Services.AddSingleton<IComputerMoveStrategy, BasicComputerMoveStrategy>();

// Application Services
// CRITICAL: Registered as Singleton so the internal ConcurrentDictionary<GameId, SemaphoreSlim>
// persists across HTTP requests, serializing concurrent requests per game across threads.
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IScoreboardService, ScoreboardService>();

// Health Checks & Developer Tooling
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Centralized ProblemDetails Exception Middleware (MUST be first)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Make Program public for WebApplicationFactory discovery in test suites
public partial class Program { }
