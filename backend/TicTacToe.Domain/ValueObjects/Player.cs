namespace TicTacToe.Domain.ValueObjects;

/// <summary>
/// Represents the two players in Tic Tac Toe: X and O.
/// </summary>
public enum Player
{
    X = 1,
    O = 2
}

public static class PlayerExtensions
{
    /// <summary>
    /// Returns the alternating player (X -> O, O -> X).
    /// </summary>
    public static Player Other(this Player player) =>
        player == Player.X ? Player.O : Player.X;
}
