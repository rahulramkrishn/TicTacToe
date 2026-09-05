namespace TicTacToe.Tests.Api;

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Controllers;
using TicTacToe.Application.Services;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Infrastructure.Repositories;
using Xunit;

public class ArchitectureBoundaryTests
{
    private readonly Assembly _domainAssembly = typeof(Game).Assembly;
    private readonly Assembly _applicationAssembly = typeof(IGameService).Assembly;
    private readonly Assembly _infrastructureAssembly = typeof(InMemoryGameRepository).Assembly;
    private readonly Assembly _apiAssembly = typeof(GamesController).Assembly;

    [Fact]
    public void DomainAssembly_HasNoDependencyOnOuterLayers()
    {
        var referencedAssemblies = _domainAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain("TicTacToe.Application", referencedAssemblies);
        Assert.DoesNotContain("TicTacToe.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("TicTacToe.Api", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplicationAssembly_DependsOnlyOnDomain_NotOnInfrastructureOrApi()
    {
        var referencedAssemblies = _applicationAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.Contains("TicTacToe.Domain", referencedAssemblies);
        Assert.DoesNotContain("TicTacToe.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("TicTacToe.Api", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InfrastructureAssembly_DoesNotDependOnApi()
    {
        var referencedAssemblies = _infrastructureAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.Contains("TicTacToe.Domain", referencedAssemblies);
        Assert.DoesNotContain("TicTacToe.Api", referencedAssemblies);
    }

    [Fact]
    public void Controllers_AreThin_AndOnlyDependOnApplicationServiceInterfaces()
    {
        var controllerTypes = _apiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            var constructors = controller.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                foreach (var param in parameters)
                {
                    // Controllers must not depend on repositories directly or infrastructure implementations
                    Assert.False(
                        param.ParameterType.Namespace?.StartsWith("TicTacToe.Infrastructure") ?? false,
                        $"{controller.Name} directly injects infrastructure type {param.ParameterType.Name}");

                    Assert.False(
                        param.ParameterType.Name.EndsWith("Repository"),
                        $"{controller.Name} directly injects repository {param.ParameterType.Name}");
                }
            }
        }
    }
}
