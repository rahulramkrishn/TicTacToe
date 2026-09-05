namespace TicTacToe.Domain.ValueObjects;

using TicTacToe.Domain.Exceptions;

/// <summary>
/// Immutable value object representing a canonical board cell index (0..8).
/// Invariant: 0 &lt;= value &lt;= 8.
/// </summary>
public readonly record struct CellIndex : IComparable<CellIndex>
{
    public const int MinIndex = 0;
    public const int MaxIndex = 8;

    public int Value { get; }

    public CellIndex(int value)
    {
        if (value < MinIndex || value > MaxIndex)
        {
            throw new InvalidCellIndexException(value);
        }

        Value = value;
    }

    public static CellIndex From(int value) => new(value);

    public static bool TryCreate(int value, out CellIndex cellIndex)
    {
        if (value is >= MinIndex and <= MaxIndex)
        {
            cellIndex = new CellIndex(value);
            return true;
        }

        cellIndex = default;
        return false;
    }

    public static implicit operator int(CellIndex cellIndex) => cellIndex.Value;
    public static explicit operator CellIndex(int value) => new(value);

    public int CompareTo(CellIndex other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString();
}
