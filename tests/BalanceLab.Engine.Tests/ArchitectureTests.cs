using NetArchTest.Rules;

namespace BalanceLab.Engine.Tests;

public sealed class ArchitectureTests
{
    private static readonly Types Engine = Types.InAssembly(typeof(Pcg32).Assembly);

    [Fact]
    public void Engine_knows_no_game_bot_or_infrastructure()
    {
        var result = Engine.ShouldNot()
            .HaveDependencyOnAny("BalanceLab.Games", "BalanceLab.Bots", "BalanceLab.Simulation",
                "BalanceLab.Persistence", "BalanceLab.Api", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Engine_draws_randomness_only_from_its_own_generator()
    {
        // System.Random i zegar psuja determinizm: ta sama partia musi dac sie odtworzyc z ziarna.
        var result = Engine.ShouldNot()
            .HaveDependencyOnAny("System.Random", "System.Security.Cryptography.RandomNumberGenerator",
                "System.DateTime", "System.DateTimeOffset", "System.Guid")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
