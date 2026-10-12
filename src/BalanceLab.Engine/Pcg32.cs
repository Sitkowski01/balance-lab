using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace BalanceLab.Engine;

/// <summary>
/// Generator PCG32 (M. O'Neill, 2014): maly, szybki i w pelni deterministyczny.
/// Jest niemutowalny, bo bedzie czescia stanu gry: to samo ziarno daje te sama partie,
/// a powtorka z dziennika ruchow odtwarza identyczne losowania. <see cref="Random"/>
/// tego nie obiecuje — jego algorytm moze sie zmienic miedzy wersjami .NET.
/// </summary>
/// <remarks>Tworz go przez <see cref="Create"/>; <c>default(Pcg32)</c> nie jest poprawnym generatorem.</remarks>
public readonly record struct Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;

    private Pcg32(ulong state, ulong increment)
    {
        State = state;
        Increment = increment;
    }

    /// <summary>Wewnetrzny stan — publiczny, zeby dalo sie go zapisac i porownac w testach powtorek.</summary>
    public ulong State { get; }

    /// <summary>Zawsze nieparzysty; rozne wartosci to niezalezne strumienie dla tego samego ziarna.</summary>
    public ulong Increment { get; }

    /// <summary>Inicjalizacja jak <c>pcg32_srandom_r</c> z implementacji referencyjnej.</summary>
    public static Pcg32 Create(ulong seed, ulong stream = 0)
    {
        var rng = new Pcg32(0, (stream << 1) | 1).Step();
        return new Pcg32(unchecked(rng.State + seed), rng.Increment).Step();
    }

    /// <summary>Losowa liczba 32-bitowa i generator po losowaniu.</summary>
    public (uint Value, Pcg32 Next) NextUInt()
    {
        if (Increment == 0)
        {
            throw new InvalidOperationException("Generator nie jest zainicjowany - uzyj Pcg32.Create.");
        }

        var old = State;
        var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (uint.RotateRight(xorShifted, rotation), Step());
    }

    /// <summary>
    /// Liczba z przedzialu [0, bound). Samo <c>r % bound</c> faworyzowaloby male liczby,
    /// wiec odrzucamy losowania z niepelnej koncowki zakresu 2^32.
    /// </summary>
    public (int Value, Pcg32 Next) NextInt(int bound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bound);

        var range = (uint)bound;
        var threshold = unchecked(0u - range) % range; // 2^32 mod range
        var rng = this;
        while (true)
        {
            (var r, rng) = rng.NextUInt();
            if (r >= threshold)
            {
                return ((int)(r % range), rng);
            }
        }
    }

    /// <summary>Tasowanie Fishera-Yatesa; wejscie zostaje nietkniete.</summary>
    public (ImmutableArray<T> Items, Pcg32 Next) Shuffle<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var result = items.ToArray();
        var rng = this;
        for (var i = result.Length - 1; i > 0; i--)
        {
            (var j, rng) = rng.NextInt(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return (ImmutableCollectionsMarshal.AsImmutableArray(result), rng);
    }

    private Pcg32 Step() => new(unchecked((State * Multiplier) + Increment), Increment);
}
