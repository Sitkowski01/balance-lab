using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace BalanceLab.Simulation.Tests;

public sealed class ProportionTests
{
    [Theory]
    // Wartosci policzone niezaleznie (osobny skrypt, ten sam wzor); 50/100 zgadza sie z tablicami: 0,404-0,596.
    [InlineData(50, 100, 0.40383, 0.59617)]
    [InlineData(0, 10, 0.0, 0.27753)]
    [InlineData(10, 10, 0.72247, 1.0)]
    [InlineData(1, 3, 0.06149, 0.79234)]
    [InlineData(7, 20, 0.18119, 0.56715)]
    public void Matches_reference_wilson_intervals(int successes, int trials, double low, double high)
    {
        var p = Proportion.Of(successes, trials);

        Assert.Equal(low, p.Low, 5);
        Assert.Equal(high, p.High, 5);
        Assert.Equal((double)successes / trials, p.Rate);
        Assert.Equal((successes, trials), (p.Successes, p.Trials));
    }

    [Fact]
    public void No_trials_means_we_know_nothing()
    {
        Assert.Equal(new Proportion(0, 0, 0, 0, 1), Proportion.Of(0, 0));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(6, 5)]
    public void Rejects_impossible_counts(int successes, int trials)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Proportion.Of(successes, trials));
    }

    [Property]
    public Property Interval_contains_the_rate_and_stays_within_zero_and_one(PositiveInt trials, NonNegativeInt successes)
    {
        var p = Proportion.Of(Math.Min(successes.Get, trials.Get), trials.Get);

        return (0 <= p.Low && p.Low <= p.Rate && p.Rate <= p.High && p.High <= 1).ToProperty();
    }

    [Fact]
    public void More_games_give_a_narrower_interval()
    {
        var small = Proportion.Of(55, 100);
        var large = Proportion.Of(550, 1_000);

        Assert.True(large.High - large.Low < small.High - small.Low);
        Assert.False(small.Excludes(0.5));
        Assert.True(Proportion.Of(5_500, 10_000).Excludes(0.5));
        Assert.True(Proportion.Of(4_500, 10_000).Excludes(0.5));
    }

    [Fact]
    public void Interval_ends_belong_to_the_interval()
    {
        var p = Proportion.Of(30, 100);

        Assert.False(p.Excludes(p.Low));
        Assert.False(p.Excludes(p.High));
        Assert.True(p.Excludes(Math.BitDecrement(p.Low)));
        Assert.True(p.Excludes(Math.BitIncrement(p.High)));
    }
}
