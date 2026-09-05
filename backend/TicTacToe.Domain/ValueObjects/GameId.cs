namespace TicTacToe.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identity value object for a Game session.
/// Invariant: Guid cannot be empty.
/// </summary>
public readonly record struct GameId
{
    public Guid Value { get; }

    public GameId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("GameId cannot be Guid.Empty.", nameof(value));
        }

        Value = value;
    }

    public static GameId New() => new(Guid.NewGuid());

    public static GameId From(Guid value) => new(value);

    public static implicit operator Guid(GameId gameId) => gameId.Value;
    public static explicit operator GameId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
