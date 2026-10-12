using FsCheck;
using FsCheck.Xunit;

namespace BalanceLab.Bots.Tests;

public sealed class DeterministicMathTests
{
    [Property(MaxTest = 1_000)]
    public bool Matches_the_system_logarithm_to_a_few_last_bits(PositiveInt n)
    {
        var expected = Math.Log(n.Get);
        var ulp = Math.BitIncrement(Math.Max(expected, 1)) - Math.Max(expected, 1);
        return Math.Abs(DeterministicMath.Ln(n.Get) - expected) <= 4 * ulp;
    }

    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(2, 0.6931471805599453)]
    [InlineData(3, 1.0986122886681098)]
    [InlineData(1_000, 6.907755278982137)]
    [InlineData(int.MaxValue, 21.487562596892644)]
    public void Known_values(int n, double expected)
    {
        Assert.Equal(expected, DeterministicMath.Ln(n), 14);
    }

    [Fact]
    public void Bits_are_frozen_so_every_machine_gets_the_same_search()
    {
        // Zmiana algorytmu zmienilaby ruchy MCTS dla zapisanych ziaren. Dla 7 wynik rozni sie od Math.Log
        // na Windowsie o jeden ostatni bit — wlasnie po to jest wlasna funkcja: bity sa nasze, nie systemu.
        Assert.Equal(0x401B_A18A_998F_FFA0UL, BitConverter.DoubleToUInt64Bits(DeterministicMath.Ln(1_000)));
        Assert.Equal(0x3FFF_2272_AE32_5A56UL, BitConverter.DoubleToUInt64Bits(DeterministicMath.Ln(7)));
        Assert.Equal(0x4027_7280_F467_1181UL, BitConverter.DoubleToUInt64Bits(DeterministicMath.Ln(123_456)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_non_positive_numbers(int n)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeterministicMath.Ln(n));
    }
}
