namespace TicTacToe.Application.Events;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Domain.Events;

/// <summary>
/// In-process domain event dispatcher that resolves and invokes handlers synchronously/in-process.
/// Pure C# implementation with zero external dependencies.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ConcurrentDictionary<Type, List<Func<IDomainEvent, Task>>> _handlers = new();

    /// <summary>
    /// Registers a strongly-typed domain event handler delegate.
    /// </summary>
    public void RegisterHandler<TEvent>(IDomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Func<IDomainEvent, Task>>());
        lock (list)
        {
            list.Add(e => handler.HandleAsync((TEvent)e));
        }
    }

    /// <summary>
    /// Registers an action or function delegate as a handler for testing and lightweight wiring.
    /// </summary>
    public void RegisterHandler<TEvent>(Func<TEvent, Task> handlerFunc) where TEvent : IDomainEvent
    {
        if (handlerFunc == null)
        {
            throw new ArgumentNullException(nameof(handlerFunc));
        }

        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Func<IDomainEvent, Task>>());
        lock (list)
        {
            list.Add(e => handlerFunc((TEvent)e));
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var eventType = domainEvent.GetType();
        if (_handlers.TryGetValue(eventType, out var handlerList))
        {
            List<Func<IDomainEvent, Task>> handlersCopy;
            lock (handlerList)
            {
                handlersCopy = handlerList.ToList();
            }

            foreach (var handler in handlersCopy)
            {
                await handler(domainEvent).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent).ConfigureAwait(false);
        }
    }
}
