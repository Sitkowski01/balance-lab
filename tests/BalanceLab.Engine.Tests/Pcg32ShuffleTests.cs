using FsCheck.Xunit;

namespace BalanceLab.Engine.Tests;

public sealed class Pcg32ShuffleTests
{
    [Property]
    public bool Shuffle_is_a_permutation(ulong seed, int[] items)
    {
        var (shuffled, _) = Pcg32.Create(seed).Shuffle(items);

        return shuffled.Order().SequenceEqual(items.Order());
    }

    [Fact]
    public void Shuffle_leaves_the_input_untouched()
    {
        int[] items = [1, 2, 3, 4, 5, 6, 7, 8];

        Pcg32.Create(5).Shuffle(items);

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], items);
    }

    [Fact]
    public void Shuffle_is_stable_between_versions()
    {
        var (shuffled, next) = Pcg32.Create(2026).Shuffle(Enumerable.Range(0, 10).ToArray());

        Assert.Equal(StableShuffle, shuffled);
        Assert.Equal(StableAfterShuffle, next.NextUInt().Value);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 42 })]
    public void Shuffling_fewer_than_two_items_draws_nothing(int[] items)
    {
        var rng = Pcg32.Create(9);

        var (shuffled, next) = rng.Shuffle(items);

        Assert.Equal(items, shuffled);
        Assert.Equal(rng, next);
    }

    [Fact]
    public void Shuffle_rejects_null()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Pcg32.Create(1).Shuffle<int>(null!));

        Assert.Equal("items", ex.ParamName);
    }

    [Fact]
    public void Shuffle_gives_every_order_equal_chance()
    {
        const int Rounds = 60_000;
        var counts = new Dictionary<string, int>();
        var rng = Pcg32.Create(13);
        for (var i = 0; i < Rounds; i++)
        {
            (var shuffled, rng) = rng.Shuffle("abc".ToCharArray());
            var key = new string([.. shuffled]);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        // 6 permutacji, 5 stopni swobody; 20.5 to wartosc krytyczna dla p = 0.001.
        const double Expected = Rounds / 6.0;
        Assert.Equal(6, counts.Count);
        var chiSquare = counts.Values.Sum(c => (c - Expected) * (c - Expected) / Expected);
        Assert.True(chiSquare < 20.5, $"chi^2 = {chiSquare}");
    }

    private static readonly int[] StableShuffle = [6, 9, 1, 2, 3, 5, 0, 7, 4, 8];
    private const uint StableAfterShuffle = 517_795_068;
}
