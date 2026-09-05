namespace TicTacToe.Application.Models;

using System;
using System.Collections.Generic;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Session scoreboard counters DTO conforming to docs/06-api-contract.md.
/// </summary>
public sealed record ScoreboardDto(
    int XWins,
    int OWins,
    int Draws,
    int TotalGames);

/// <summary>
/// Move history item DTO conforming to docs/06-api-contract.md.
/// </summary>
public sealed record MoveDto(
    int MoveNumber,
    Player Player,
    int CellIndex);

/// <summary>
/// Full authoritative game state DTO conforming to docs/06-api-contract.md.
/// Exposes application-safe data with zero leakage of domain entities or internal collections.
/// </summary>
public sealed record GameStateDto(
    Guid GameId,
    IReadOnlyList<Player?> Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveDto> MoveHistory,
    bool CanUndo,
    ScoreboardDto Scoreboard);

/// <summary>
/// Command to create a new game session.
/// </summary>
public sealed record CreateGameCommand(
    GameMode Mode = GameMode.TwoPlayer);

/// <summary>
/// Command to execute a player move.
/// </summary>
public sealed record MakeMoveCommand(
    Guid GameId,
    Player Player,
    int CellIndex);
