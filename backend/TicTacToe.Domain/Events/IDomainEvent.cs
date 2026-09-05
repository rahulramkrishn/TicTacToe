namespace TicTacToe.Domain.Events;

/// <summary>
/// Defines the contract for an immutable domain event within the TicTacToe domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identity of this event instance (used for deduplication and idempotency).
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp indicating when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
