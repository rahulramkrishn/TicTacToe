namespace TicTacToe.Api.Models;

using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Transport request model for creating a new game session.
/// </summary>
public sealed record CreateGameRequest(
    GameMode Mode = GameMode.TwoPlayer);

/// <summary>
/// Transport request model for executing a player move.
/// Strictly conforms to docs/06-api-contract.md.
/// </summary>
public sealed record MakeMoveRequest(
    Player Player,
    int CellIndex);
