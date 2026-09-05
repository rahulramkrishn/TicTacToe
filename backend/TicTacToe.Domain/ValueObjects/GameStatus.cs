namespace TicTacToe.Domain.ValueObjects;

/// <summary>
/// Status of a Tic Tac Toe game session.
/// </summary>
public enum GameStatus
{
    InProgress = 1,
    Won = 2,
    Draw = 3
}

public static class GameStatusExtensions
{
    /// <summary>
    /// Indicates if the status represents a terminal state (Won or Draw).
    /// </summary>
    public static bool IsTerminal(this GameStatus status) =>
        status == GameStatus.Won || status == GameStatus.Draw;
}
