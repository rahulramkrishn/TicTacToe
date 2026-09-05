namespace TicTacToe.Application.Events;

using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Domain.Events;

/// <summary>
/// Dispatches domain events to their corresponding in-process domain event handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a single domain event to registered handlers.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent);

    /// <summary>
    /// Dispatches a collection of domain events sequentially.
    /// </summary>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents);
}
