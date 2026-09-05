namespace TicTacToe.Application.Mappings;

using System.Collections.Generic;
using System.Linq;
using TicTacToe.Application.Models;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Pure mapping extension methods converting Domain aggregate state into Application DTOs.
/// </summary>
public static class DtoMappingExtensions
{
    /// <summary>
    /// Maps a Scoreboard aggregate to a ScoreboardDto.
    /// </summary>
    public static ScoreboardDto ToDto(this Scoreboard scoreboard)
    {
        return new ScoreboardDto(
            scoreboard.XWins,
            scoreboard.OWins,
            scoreboard.Draws,
            scoreboard.TotalGames);
    }

    /// <summary>
    /// Maps a Move value object to a MoveDto.
    /// </summary>
    public static MoveDto ToDto(this Move move)
    {
        return new MoveDto(
            move.MoveNumber,
            move.Player,
            move.CellIndex.Value);
    }

    /// <summary>
    /// Maps a Game aggregate and authoritative Scoreboard to a GameStateDto.
    /// </summary>
    public static GameStateDto ToDto(this Game game, Scoreboard scoreboard)
    {
        var boardCells = new List<Player?>(9);
        for (var i = 0; i < 9; i++)
        {
            boardCells.Add(game.Board.GetCell(new CellIndex(i)));
        }

        var moveHistoryDtos = game.MoveHistory
            .Select(m => m.ToDto())
            .ToList();

        var winningCells = game.WinningCells != null
            ? game.WinningCells.ToList()
            : new List<int>();

        return new GameStateDto(
            game.Id.Value,
            boardCells.AsReadOnly(),
            game.CurrentPlayer,
            game.Mode,
            game.Status,
            game.Winner,
            winningCells.AsReadOnly(),
            moveHistoryDtos.AsReadOnly(),
            game.CanUndo,
            scoreboard.ToDto());
    }
}
