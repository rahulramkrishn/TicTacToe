namespace TicTacToe.Domain.Exceptions;

/// <summary>
/// Base exception for all business domain invariant violations.
/// Pure domain concept with zero HTTP or framework dependencies.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
