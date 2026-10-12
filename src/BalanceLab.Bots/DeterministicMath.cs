using System.Numerics;

namespace BalanceLab.Bots;

/// <summary>
/// Logarytm liczony wylacznie dodawaniem, mnozeniem i dzieleniem. IEEE 754 gwarantuje dla nich
/// wynik co do bitu na kazdej maszynie, a dla <see cref="Math.Log(double)"/> juz nie — ten
/// korzysta z biblioteki systemu (inna na Windowsie, inna na Linuksie). Gdyby ostatni bit UCB
/// sie roznil, MCTS z tym samym ziarnem moglby wybrac inny ruch na innym komputerze.
/// </summary>
internal static class DeterministicMath
{
    private const double Ln2 = 0.6931471805599453;

    /// <summary>Logarytm naturalny liczby dodatniej.</summary>
    public static double Ln(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);

        // n = 2^k · m, m w [1, 2): ln n = k·ln 2 + ln m.
        var k = BitOperations.Log2((uint)n);
        var m = (double)n / (1 << k);

        // ln m = 2·atanh(s), s = (m − 1)/(m + 1) <= 1/3, wiec szereg s + s³/3 + s⁵/5 + ... zbiega szybko:
        // 20 wyrazow to blad ponizej 1e-20, duzo mniej niz dokladnosc double.
        var s = (m - 1) / (m + 1);
        var s2 = s * s;
        var term = s;
        var sum = 0.0;
        for (var i = 1; i < 40; i += 2)
        {
            sum += term / i;
            term *= s2;
        }

        return (k * Ln2) + (2 * sum);
    }
}
