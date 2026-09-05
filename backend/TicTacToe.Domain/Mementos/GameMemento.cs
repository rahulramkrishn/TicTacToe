namespace TicTacToe.Domain.Mementos;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.ValueObjects;

/// <summary>
/// Immutable snapshot of the Game aggregate root's internal state.
/// Acts as the Memento in the GoF Memento Pattern.
/// </summary>
internal sealed record GameMemento(
    Board BoardSnapshot,
    Player CurrentPlayer,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<Move> MoveHistory);
