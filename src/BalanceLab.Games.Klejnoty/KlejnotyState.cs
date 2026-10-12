using System.Collections.Immutable;

namespace BalanceLab.Games.Klejnoty;

/// <summary>Zarezerwowana karta; wzieta w ciemno z talii jest zakryta dla pozostalych graczy.</summary>
public readonly record struct Reservation(int Card, bool Hidden);

/// <summary>Wszystko, co ma jeden gracz.</summary>
/// <param name="Bonuses">Rabaty z kupionych kart (po jednym w kolorze kazdej karty).</param>
public sealed record PlayerBoard(
    Gems Tokens,
    Gems Bonuses,
    int Points,
    ImmutableArray<int> Bought,
    ImmutableArray<Reservation> Reserved,
    ImmutableArray<int> Patrons)
{
    public static PlayerBoard Empty { get; } = new(Gems.None, Gems.None, 0, [], [], []);
}

/// <summary>
/// Stan Klejnotow. Talie leza wierzchem na koncu tablicy (zdjecie karty nie przesuwa reszty).
/// Rynek to 4 sloty na kazdy poziom; pusty slot (wyczerpana talia) ma wartosc -1.
/// </summary>
/// <param name="Passes">Ile ruchow z rzedu to pasy; gdy pasuja wszyscy po kolei, partia sie konczy.</param>
/// <param name="FinalRound">Ktos ma 15 punktow: dogrywamy runde do ostatniego gracza.</param>
public sealed record KlejnotyState(
    ImmutableArray<PlayerBoard> Players,
    Gems Bank,
    ImmutableArray<int> Market,
    ImmutableArray<ImmutableArray<int>> Decks,
    ImmutableArray<int> Patrons,
    int ToMove,
    int Passes,
    bool FinalRound,
    bool Over)
{
    public const int SlotsPerTier = 4;
    public const int MaxTokens = 10;
    public const int MaxReserved = 3;
    public const int WinningPoints = 15;

    public PlayerBoard Current => Players[ToMove];

    /// <summary>Karta w slocie rynku albo null, gdy slot pusty.</summary>
    public Card? MarketCard(int tier, int slot)
    {
        var id = Market[MarketIndex(tier, slot)];
        return id < 0 ? null : Cards.All[id];
    }

    internal static int MarketIndex(int tier, int slot) => ((tier - 1) * SlotsPerTier) + slot;
}

public enum MoveKind
{
    /// <summary>Do trzech zetonow w roznych kolorach.</summary>
    TakeDifferent,

    /// <summary>Dwa zetony jednego koloru, gdy w banku sa co najmniej 4.</summary>
    TakeTwo,

    /// <summary>Karta z rynku na reke, plus zloto, jesli jest w banku.</summary>
    Reserve,

    /// <summary>Wierzchnia karta talii w ciemno, plus zloto.</summary>
    ReserveFromDeck,

    Buy,

    BuyReserved,

    /// <summary>Oddanie zetonu ponad limit 10 — ruch tego samego gracza az zejdzie do limitu.</summary>
    Return,

    /// <summary>Tylko gdy nic innego nie jest legalne.</summary>
    Pass,
}

/// <summary>Ruch w Klejnotach. Placenie jest automatyczne: najpierw kolory, zlotem tylko brakujace.</summary>
public readonly record struct KlejnotyMove(MoveKind Kind, Gems Gems, int Card, int Tier)
{
    public static KlejnotyMove Take(Gems gems) => new(MoveKind.TakeDifferent, gems, -1, 0);

    public static KlejnotyMove TakeTwo(Gem gem) => new(MoveKind.TakeTwo, Gems.None.With(gem, 2), -1, 0);

    public static KlejnotyMove Reserve(int card) => new(MoveKind.Reserve, Gems.None, card, 0);

    public static KlejnotyMove ReserveFromDeck(int tier) => new(MoveKind.ReserveFromDeck, Gems.None, -1, tier);

    public static KlejnotyMove Buy(int card) => new(MoveKind.Buy, Gems.None, card, 0);

    public static KlejnotyMove BuyReserved(int card) => new(MoveKind.BuyReserved, Gems.None, card, 0);

    public static KlejnotyMove Return(Gem gem) => new(MoveKind.Return, Gems.One(gem), -1, 0);

    public static KlejnotyMove Pass { get; } = new(MoveKind.Pass, Gems.None, -1, 0);

    public override string ToString() => Kind switch
    {
        MoveKind.TakeDifferent or MoveKind.TakeTwo or MoveKind.Return => $"{Kind} {Gems}",
        MoveKind.ReserveFromDeck => $"{Kind} {Tier}",
        MoveKind.Pass => "Pass",
        _ => $"{Kind} #{Card}",
    };
}
