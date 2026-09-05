namespace TicTacToe.Application.Events;

using System.Threading.Tasks;
using TicTacToe.Domain.Events;

/// <summary>
/// Defines a strongly-typed in-process handler for a specific domain event.
/// </summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    Task HandleAsync(TEvent domainEvent);
}
