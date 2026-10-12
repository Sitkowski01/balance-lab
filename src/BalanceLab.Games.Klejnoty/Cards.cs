using System.Collections.Immutable;

namespace BalanceLab.Games.Klejnoty;

/// <summary>Karta kopalni: kosztuje klejnoty, daje staly rabat w swoim kolorze i czasem punkty.</summary>
/// <param name="Tier">Poziom 1-3: drozsze karty daja wiecej punktow.</param>
public sealed record Card(int Id, int Tier, Gem Bonus, int Points, Gems Cost);

/// <summary>Patron odwiedza gracza, ktory zebral wymagane rabaty, i daje punkty.</summary>
public sealed record Patron(int Id, Gems Requirement, int Points);

/// <summary>
/// Wlasny zestaw kart (mechanika jak w Splendorze, liczby nasze). Kazdy wzor kosztu wystepuje
/// w kazdym kolorze z obroconymi kolorami, wiec zaden kolor nie jest z gory lepszy — punkt
/// wyjscia, od ktorego raport balansu bedzie porownywal warianty.
/// </summary>
public static class Cards
{
    public const int Tiers = 3;

    /// <summary>Wzory kosztu: (punkty, koszt w kolorach liczonych od koloru karty: wlasny, +1, +2, +3, +4).</summary>
    private static readonly (int Points, int[] Cost)[][] Patterns =
    [
        [
            (0, [0, 1, 1, 1, 1]), (0, [0, 1, 1, 2, 1]), (0, [0, 2, 0, 2, 1]), (0, [1, 0, 3, 0, 1]),
            (0, [0, 0, 0, 3, 0]), (0, [0, 2, 0, 0, 2]), (0, [0, 0, 1, 2, 0]), (1, [0, 0, 0, 0, 4]),
        ],
        [
            (1, [0, 2, 2, 3, 0]), (1, [2, 3, 0, 0, 3]), (2, [0, 0, 0, 5, 0]),
            (2, [0, 0, 5, 3, 0]), (2, [0, 1, 0, 4, 2]), (3, [6, 0, 0, 0, 0]),
        ],
        [
            (3, [0, 3, 3, 5, 3]), (4, [0, 0, 0, 0, 7]), (4, [3, 0, 0, 3, 6]), (5, [3, 0, 0, 0, 7]),
        ],
    ];

    /// <summary>Wszystkie karty; indeks na liscie to <see cref="Card.Id"/>. Poziom 1: 40, 2: 30, 3: 20.</summary>
    public static ImmutableArray<Card> All { get; } = Build();

    /// <summary>
    /// Patroni: po 4 rabaty w dwoch sasiednich kolorach albo po 3 w trzech; kazdy za 3 punkty.
    /// </summary>
    public static ImmutableArray<Patron> Patrons { get; } =
    [
        .. Enumerable.Range(0, Gems.Colors).Select(c => new Patron(c, Rotated(c, [4, 4, 0, 0, 0]), 3)),
        .. Enumerable.Range(0, Gems.Colors).Select(c => new Patron(Gems.Colors + c, Rotated(c, [3, 3, 3, 0, 0]), 3)),
    ];

    /// <summary>Id kart danego poziomu, rosnaco.</summary>
    public static ImmutableArray<int> OfTier(int tier) => [.. All.Where(c => c.Tier == tier).Select(c => c.Id)];

    private static ImmutableArray<Card> Build()
    {
        var cards = ImmutableArray.CreateBuilder<Card>();
        for (var tier = 1; tier <= Tiers; tier++)
        {
            for (var color = 0; color < Gems.Colors; color++)
            {
                foreach (var (points, cost) in Patterns[tier - 1])
                {
                    cards.Add(new Card(cards.Count, tier, (Gem)color, points, Rotated(color, cost)));
                }
            }
        }

        return cards.ToImmutable();
    }

    /// <summary>Koszt liczony od koloru <paramref name="color"/> przerobiony na zwykle kolory.</summary>
    private static Gems Rotated(int color, int[] relative)
    {
        var counts = new int[Gems.Colors];
        for (var i = 0; i < Gems.Colors; i++)
        {
            counts[(color + i) % Gems.Colors] = relative[i];
        }

        return Gems.FromColors(counts);
    }
}
