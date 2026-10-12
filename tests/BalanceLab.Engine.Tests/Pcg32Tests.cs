using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace BalanceLab.Engine.Tests;

public sealed class Pcg32Tests
{
    [Fact]
    public void Matches_the_reference_implementation()
    {
        // Wynik programu pcg32-demo z pcg-random.org dla ziarna 42 i strumienia 54.
        Assert.Equal([0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e],
            Draws(Pcg32.Create(42, 54), 6));
    }

    [Property]
    public bool Same_seed_gives_the_same_sequence(ulong seed, ulong stream) =>
        Draws(Pcg32.Create(seed, stream), 20).SequenceEqual(Draws(Pcg32.Create(seed, stream), 20));

    [Property]
    public Property Different_seeds_give_different_sequences(ulong a, ulong b) =>
        (!Draws(Pcg32.Create(a), 4).SequenceEqual(Draws(Pcg32.Create(b), 4))).When(a != b);

    [Property]
    public Property Different_streams_give_different_sequences(ulong seed, ulong a, ulong b) =>
        // Strumien jest przesuwany o bit w lewo, wiec liczy sie tylko 63 mlodsze bity.
        (!Draws(Pcg32.Create(seed, a), 4).SequenceEqual(Draws(Pcg32.Create(seed, b), 4)))
            .When((a << 1) != (b << 1));

    [Fact]
    public void Drawing_does_not_change_the_generator()
    {
        var rng = Pcg32.Create(7);

        Assert.Equal(rng.NextUInt(), rng.NextUInt());
    }

    [Fact]
    public void Default_value_refuses_to_draw()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Pcg32).NextUInt());

        Assert.Contains("Pcg32.Create", ex.Message, StringComparison.Ordinal);
    }

    private static uint[] Draws(Pcg32 rng, int count)
    {
        var values = new uint[count];
        for (var i = 0; i < count; i++)
        {
            (values[i], rng) = rng.NextUInt();
        }

        return values;
    }
}
