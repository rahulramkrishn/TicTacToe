namespace TicTacToe.Api.Middleware;

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TicTacToe.Application.Exceptions;
using TicTacToe.Domain.Exceptions;

/// <summary>
/// Centralized middleware intercepting unhandled exceptions and formatting them as RFC 7807 ProblemDetails.
/// Enforces error sanitization, correlation/trace ID handling, and strict error code conventions.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this._next = _next ?? throw new ArgumentNullException(nameof(_next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) &&
            !string.IsNullOrWhiteSpace(correlationHeader))
        {
            traceId = correlationHeader.ToString();
        }

        context.Response.Headers["X-Correlation-Id"] = traceId;

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, traceId).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot write ProblemDetails for exception.");
            return;
        }

        var (statusCode, title, code, typeUri, detail) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled server exception occurred for request {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);
        }
        else
        {
            _logger.LogInformation(
                "Handled client/business exception for request {Method} {Path}: {Code} - {Detail}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                code,
                detail,
                traceId);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = typeUri,
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            code,
            traceId
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }

    private static (int StatusCode, string Title, string Code, string TypeUri, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            GameNotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Game Not Found",
                "GAME_NOT_FOUND",
                "https://api.tictactoe.com/errors/game-not-found",
                ex.Message
            ),
            InvalidCellIndexException ex => (
                StatusCodes.Status400BadRequest,
                "Invalid Cell Index",
                "INVALID_CELL_INDEX",
                "https://api.tictactoe.com/errors/invalid-cell-index",
                ex.Message
            ),
            JsonException ex => (
                StatusCodes.Status400BadRequest,
                "Malformed Request",
                "MALFORMED_REQUEST",
                "https://api.tictactoe.com/errors/malformed-request",
                "The request body is malformed or contains invalid/unknown properties."
            ),
            BadHttpRequestException ex => (
                StatusCodes.Status400BadRequest,
                "Malformed Request",
                "MALFORMED_REQUEST",
                "https://api.tictactoe.com/errors/malformed-request",
                ex.Message
            ),
            CellOccupiedException ex => (
                StatusCodes.Status409Conflict,
                "Cell Occupied",
                "CELL_OCCUPIED",
                "https://api.tictactoe.com/errors/cell-occupied",
                ex.Message
            ),
            InvalidTurnException ex => (
                StatusCodes.Status409Conflict,
                "Invalid Turn",
                "INVALID_TURN",
                "https://api.tictactoe.com/errors/invalid-turn",
                ex.Message
            ),
            GameAlreadyCompletedException ex => (
                StatusCodes.Status409Conflict,
                "Game Already Completed",
                "GAME_ALREADY_COMPLETED",
                "https://api.tictactoe.com/errors/game-already-completed",
                ex.Message
            ),
            CannotUndoException ex => (
                StatusCodes.Status409Conflict,
                "Cannot Undo Move",
                "CANNOT_UNDO",
                "https://api.tictactoe.com/errors/cannot-undo",
                ex.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "INTERNAL_SERVER_ERROR",
                "https://api.tictactoe.com/errors/internal-server-error",
                "An unexpected error occurred. Please try again later."
            )
        };
    }
}
