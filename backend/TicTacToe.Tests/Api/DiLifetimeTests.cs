namespace TicTacToe.Tests.Api;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application.Events;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Repositories;
using TicTacToe.Domain.Services;
using Xunit;

public class DiLifetimeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DiLifetimeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GameService_IsRegisteredAsSingleton_PreservingPerGameLockDictionary()
    {
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var service1 = scope1.ServiceProvider.GetRequiredService<IGameService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<IGameService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2);
    }

    [Fact]
    public void ScoreboardService_IsRegisteredAsSingleton()
    {
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var service1 = scope1.ServiceProvider.GetRequiredService<IScoreboardService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<IScoreboardService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2);
    }

    [Fact]
    public void Repositories_AreRegisteredAsSingletons()
    {
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var gameRepo1 = scope1.ServiceProvider.GetRequiredService<IGameRepository>();
        var gameRepo2 = scope2.ServiceProvider.GetRequiredService<IGameRepository>();
        Assert.Same(gameRepo1, gameRepo2);

        var scoreboardRepo1 = scope1.ServiceProvider.GetRequiredService<IScoreboardRepository>();
        var scoreboardRepo2 = scope2.ServiceProvider.GetRequiredService<IScoreboardRepository>();
        Assert.Same(scoreboardRepo1, scoreboardRepo2);
    }

    [Fact]
    public void InfrastructureAndDomainServices_AreRegisteredAsSingletons()
    {
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        Assert.Same(dispatcher1, dispatcher2);

        var strategy1 = scope1.ServiceProvider.GetRequiredService<IComputerMoveStrategy>();
        var strategy2 = scope2.ServiceProvider.GetRequiredService<IComputerMoveStrategy>();
        Assert.Same(strategy1, strategy2);
    }
}
