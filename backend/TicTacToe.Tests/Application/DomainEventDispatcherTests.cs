namespace TicTacToe.Tests.Application;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacToe.Application.Events;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task Dispatcher_SynchronouslyInvokesRegisteredHandler()
    {
        var dispatcher = new DomainEventDispatcher();
        var invokedEvents = new List<GameCompletedEvent>();

        dispatcher.RegisterHandler<GameCompletedEvent>(evt =>
        {
            invokedEvents.Add(evt);
            return Task.CompletedTask;
        });

        var domainEvent = new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(domainEvent);

        Assert.Single(invokedEvents);
        Assert.Same(domainEvent, invokedEvents[0]);
    }

    [Fact]
    public async Task Dispatcher_InvokesMultipleHandlersForSameEvent()
    {
        var dispatcher = new DomainEventDispatcher();
        var count1 = 0;
        var count2 = 0;

        dispatcher.RegisterHandler<GameCompletedEvent>(_ =>
        {
            count1++;
            return Task.CompletedTask;
        });

        dispatcher.RegisterHandler<GameCompletedEvent>(_ =>
        {
            count2++;
            return Task.CompletedTask;
        });

        var domainEvent = new GameCompletedEvent(
            Guid.NewGuid(),
            GameId.New(),
            GameStatus.Won,
            Player.X,
            new[] { 0, 1, 2 },
            5,
            DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(domainEvent);

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task Dispatcher_ThrowsWhenEventIsNull()
    {
        var dispatcher = new DomainEventDispatcher();
        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.DispatchAsync((IDomainEvent)null!));
    }
}
