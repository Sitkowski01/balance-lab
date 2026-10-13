using System.Collections.Immutable;
using System.Numerics;
using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

/// <summary>Zasady Klejnotow jako funkcje statyczne; <see cref="KlejnotyGame"/> podpina je pod kontrakt silnika.</summary>
public static partial class KlejnotyRules
{
    public const int GoldTokens = 5;

    public const int MinPlayers = 2;

    public const int MaxPlayers = 4;

    /// <summary>Zetonow kazdego koloru w banku na start: mniej graczy, ciasniejszy bank.</summary>
    public static int TokensPerColor(int players) => players switch
    {
        2 => 4,
        3 => 5,
        _ => 7,
    };

    /// <summary>Rozdanie; wariant zmienia liczby na kartach, ale nie tasowanie — to samo ziarno, ten sam uklad.</summary>
    public static KlejnotyState Setup(GameSetup setup, KlejnotyVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        setup.EnsurePlayers(MinPlayers, MaxPlayers);

        var rng = Pcg32.Create(setup.Seed);
        var market = new int[Cards.Tiers * KlejnotyState.SlotsPerTier];
        var decks = ImmutableArray.CreateBuilder<ImmutableArray<int>>(Cards.Tiers);
        for (var tier = 1; tier <= Cards.Tiers; tier++)
        {
            (var deck, rng) = rng.Shuffle(Cards.OfTier(tier));
            for (var slot = 0; slot < KlejnotyState.SlotsPerTier; slot++)
            {
                market[KlejnotyState.MarketIndex(tier, slot)] = deck[^(slot + 1)];
            }

            decks.Add(deck.RemoveRange(deck.Length - KlejnotyState.SlotsPerTier, KlejnotyState.SlotsPerTier));
        }

        var (patrons, _) = rng.Shuffle(variant.AllPatrons.Select(p => p.Id).ToArray());
        var perColor = TokensPerColor(setup.PlayerCount);
        return new KlejnotyState(
            [.. Enumerable.Repeat(PlayerBoard.Empty, setup.PlayerCount)],
            Gems.Of(perColor, perColor, perColor, perColor, perColor, GoldTokens),
            [.. market],
            decks.MoveToImmutable(),
            [.. patrons.Take(setup.PlayerCount + 1).Order()],
            ToMove: 0,
            Passes: 0,
            FinalRound: false,
            Over: false,
            variant);
    }

    public static IReadOnlyList<KlejnotyMove> LegalMoves(KlejnotyState state)
    {
        if (state.Over)
        {
            return [];
        }

        var me = state.Current;
        var moves = new List<KlejnotyMove>();
        if (me.Tokens.Total > KlejnotyState.MaxTokens)
        {
            for (var gem = Gem.White; gem <= Gem.Gold; gem++)
            {
                if (me.Tokens[gem] > 0)
                {
                    moves.Add(KlejnotyMove.Return(gem));
                }
            }

            return moves;
        }

        var available = AvailableColors(state.Bank);
        var size = Math.Min(3, BitOperations.PopCount((uint)available));
        for (var mask = 1; mask < 1 << Gems.Colors; mask++)
        {
            if ((mask & available) == mask && BitOperations.PopCount((uint)mask) == size)
            {
                moves.Add(KlejnotyMove.Take(ColorsOf(mask)));
            }
        }

        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            if (state.Bank[color] >= 4)
            {
                moves.Add(KlejnotyMove.TakeTwo(color));
            }
        }

        if (me.Reserved.Length < KlejnotyState.MaxReserved)
        {
            moves.AddRange(state.Market.Where(id => id >= 0).Select(KlejnotyMove.Reserve));
            for (var tier = 1; tier <= Cards.Tiers; tier++)
            {
                if (!state.Decks[tier - 1].IsEmpty)
                {
                    moves.Add(KlejnotyMove.ReserveFromDeck(tier));
                }
            }
        }

        moves.AddRange(state.Market.Where(id => id >= 0 && Payment(me, state.Variant.AllCards[id]) is not null).Select(KlejnotyMove.Buy));
        moves.AddRange(me.Reserved.Where(r => Payment(me, state.Variant.AllCards[r.Card]) is not null).Select(r => KlejnotyMove.BuyReserved(r.Card)));

        if (moves.Count == 0)
        {
            moves.Add(KlejnotyMove.Pass);
        }

        return moves;
    }

    public static ImmutableArray<double> Scores(KlejnotyState state)
    {
        if (!state.Over)
        {
            throw new InvalidOperationException("Partia jeszcze trwa.");
        }

        // Najwiecej punktow; przy remisie wygrywa ten, kto kupil mniej kart. Dalszy remis dzieli wygrana.
        var best = state.Players.Max(p => p.Points);
        var fewest = state.Players.Where(p => p.Points == best).Min(p => p.Bought.Length);
        var winners = state.Players.Count(p => p.Points == best && p.Bought.Length == fewest);
        return [.. state.Players.Select(p => p.Points == best && p.Bought.Length == fewest ? 1.0 / winners : 0.0)];
    }

    /// <summary>
    /// Ile trzeba zaplacic za karte: kolory po rabatach, a brakujace zlotem.
    /// Null, gdy gracza nie stac.
    /// </summary>
    public static Gems? Payment(PlayerBoard player, Card card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        var pay = Gems.None;
        var gold = 0;
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            var need = Math.Max(0, card.Cost[color] - player.Bonuses[color]);
            var fromColor = Math.Min(need, player.Tokens[color]);
            pay = pay.With(color, fromColor);
            gold += need - fromColor;
        }

        return gold <= player.Tokens[Gem.Gold] ? pay.With(Gem.Gold, gold) : null;
    }

    private static int AvailableColors(Gems bank)
    {
        var mask = 0;
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            if (bank[color] > 0)
            {
                mask |= 1 << (int)color;
            }
        }

        return mask;
    }

    private static Gems ColorsOf(int mask)
    {
        var gems = Gems.None;
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            if ((mask & (1 << (int)color)) != 0)
            {
                gems = gems.With(color, 1);
            }
        }

        return gems;
    }
}
