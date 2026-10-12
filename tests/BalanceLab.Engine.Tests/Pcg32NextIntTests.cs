using FsCheck;
using FsCheck.Xunit;

namespace BalanceLab.Engine.Tests;

public sealed class Pcg32NextIntTests
{
    [Property]
    public bool NextInt_stays_in_range(ulong seed, PositiveInt bound)
    {
        var rng = Pcg32.Create(seed);
        for (var i = 0; i < 50; i++)
        {
            (var value, rng) = rng.NextInt(bound.Get);
            if (value < 0 || value >= bound.Get)
            {
                return false;
            }
        }

        return true;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NextInt_rejects_bound_below_one(int bound)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Pcg32.Create(1).NextInt(bound));
    }

    [Fact]
    public void NextInt_of_one_is_always_zero()
    {
        var rng = Pcg32.Create(3);
        for (var i = 0; i < 100; i++)
        {
            (var value, rng) = rng.NextInt(1);
            Assert.Equal(0, value);
        }
    }

    [Fact]
    public void NextInt_values_are_stable_between_versions()
    {
        // Zapisane powtorki zaleza od kazdego losowania. Przy 2^30 + 1 co czwarte losowanie
        // wpada w odrzucana koncowke zakresu, wiec ta lista pilnuje tez odrzucania.
        Assert.Equal(StableNextInts, Ints(Pcg32.Create(2026), 1_073_741_825, 8));
    }

    [Fact]
    public void NextInt_accepts_a_draw_exactly_at_the_rejection_threshold()
    {
        // Pierwsze losowanie dla ziarna 12 to r = 1 370 425 950. Dla bound = (2^32 - r) / 2
        // jest 2^32 = 2 * bound + r, czyli prog odrzucania wynosi dokladnie r.
        const uint FirstDraw = 1_370_425_950;
        const int Bound = 1_462_270_673;
        var rng = Pcg32.Create(12);
        Assert.Equal(FirstDraw, rng.NextUInt().Value);

        var (value, next) = rng.NextInt(Bound);

        Assert.Equal((int)FirstDraw, value);
        Assert.Equal(rng.NextUInt().Next, next);
    }

    [Fact]
    public void NextInt_is_uniform()
    {
        const int Draws = 60_000;
        var counts = new int[6];
        var rng = Pcg32.Create(11);
        for (var i = 0; i < Draws; i++)
        {
            (var value, rng) = rng.NextInt(6);
            counts[value]++;
        }

        // Chi-kwadrat dla 5 stopni swobody; 20.5 to wartosc krytyczna dla p = 0.001.
        const double Expected = Draws / 6.0;
        var chiSquare = counts.Sum(c => (c - Expected) * (c - Expected) / Expected);
        Assert.True(chiSquare < 20.5, $"chi^2 = {chiSquare}");
    }

    private static readonly int[] StableNextInts =
        [1_002_914_738, 703_860_388, 546_070_747, 642_721_242, 307_266_128, 791_274_889, 258_164_438, 1_003_055_170];

    private static int[] Ints(Pcg32 rng, int bound, int count)
    {
        var values = new int[count];
        for (var i = 0; i < count; i++)
        {
            (values[i], rng) = rng.NextInt(bound);
        }

        return values;
    }
}
