namespace TicTacToe.Application.EventHandlers;

using System;
using System.Threading.Tasks;
using TicTacToe.Application.Events;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.Repositories;

/// <summary>
/// Domain event handler that processes GameCompletedEvent and records the outcome on the Scoreboard aggregate.
/// </summary>
public sealed class GameCompletedEventHandler : IDomainEventHandler<GameCompletedEvent>
{
    private readonly IScoreboardRepository _scoreboardRepository;

    public GameCompletedEventHandler(IScoreboardRepository scoreboardRepository)
    {
        _scoreboardRepository = scoreboardRepository ?? throw new ArgumentNullException(nameof(scoreboardRepository));
    }

    public async Task HandleAsync(GameCompletedEvent domainEvent)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var scoreboard = await _scoreboardRepository.GetScoreboardAsync().ConfigureAwait(false);
        scoreboard.RecordGameCompleted(domainEvent);
        await _scoreboardRepository.SaveScoreboardAsync(scoreboard).ConfigureAwait(false);
    }
}
