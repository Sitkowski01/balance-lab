using BalanceLab.Engine;

namespace BalanceLab.Bots.Tests;

public sealed class RolloutTests
{
    [Fact]
    public void Seed_is_built_from_two_draws_high_first()
    {
        var rng = Pcg32.Create(3);
        var (high, afterHigh) = rng.NextUInt();
        var (low, afterLow) = afterHigh.NextUInt();

        var seed = Rollout.NextSeed(ref rng);

        Assert.Equal(((ulong)high << 32) | low, seed);
        Assert.Equal(afterLow, rng);
    }

    [Theory]
    [InlineData(2, 0.5)]
    [InlineData(4, 0.25)]
    public void Cut_off_playout_counts_as_a_draw(int players, double expected)
    {
        Assert.Equal(expected, Rollout.ScoreOf([], 1, players));
        Assert.Equal(0.7, Rollout.ScoreOf([0.3, 0.7], 1, players));
    }
}
