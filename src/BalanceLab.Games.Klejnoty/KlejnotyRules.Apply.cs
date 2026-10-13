using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

public static partial class KlejnotyRules
{
    public static KlejnotyState Apply(KlejnotyState state, KlejnotyMove move)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Over)
        {
            throw new IllegalMoveException("Partia sie skonczyla.");
        }

        var returning = state.Current.Tokens.Total > KlejnotyState.MaxTokens;
        if (returning != (move.Kind == MoveKind.Return))
        {
            throw new IllegalMoveException(returning
                ? "Najpierw oddaj zetony ponad limit 10."
                : "Oddaje sie tylko zetony ponad limit 10.");
        }

        // Nieuzywane pola musza miec wartosci z fabryk: inaczej dwa rozne zapisy w dzienniku
        // oznaczalyby ten sam ruch, a powtorka nie moglaby ich porownac.
        if (move != Canonical(move))
        {
            throw new IllegalMoveException($"Ruch {move} ma wypelnione pola, ktorych nie uzywa.");
        }

        return move.Kind switch
        {
            MoveKind.TakeDifferent => Take(state, move.Gems, IsValidTakeDifferent(state.Bank, move.Gems)),
            MoveKind.TakeTwo => Take(state, move.Gems, IsValidTakeTwo(state.Bank, move.Gems)),
            MoveKind.Reserve => Reserve(state, move.Card, fromDeck: false),
            MoveKind.ReserveFromDeck => Reserve(state, move.Tier, fromDeck: true),
            MoveKind.Buy => Buy(state, move.Card, fromReserved: false),
            MoveKind.BuyReserved => Buy(state, move.Card, fromReserved: true),
            MoveKind.Return => Return(state, move.Gems),
            MoveKind.Pass => Pass(state),
            _ => throw new IllegalMoveException($"Nieznany ruch {move.Kind}."),
        };
    }

    private static KlejnotyMove Canonical(KlejnotyMove move) => move.Kind switch
    {
        MoveKind.TakeDifferent or MoveKind.TakeTwo or MoveKind.Return => move with { Card = -1, Tier = 0 },
        MoveKind.Reserve or MoveKind.Buy or MoveKind.BuyReserved => move with { Gems = Gems.None, Tier = 0 },
        MoveKind.ReserveFromDeck => move with { Gems = Gems.None, Card = -1 },
        MoveKind.Pass => KlejnotyMove.Pass,
        _ => move,
    };

    /// <summary>Rozne kolory, bez zlota; tyle, ile sie da, ale nie wiecej niz 3.</summary>
    private static bool IsValidTakeDifferent(Gems bank, Gems take)
    {
        var colors = 0;
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            colors += bank[color] > 0 ? 1 : 0;
            if (take[color] > 1)
            {
                return false;
            }
        }

        return take[Gem.Gold] == 0 && take.Total == Math.Min(3, colors) && take.Total > 0 && bank.Covers(take);
    }

    private static bool IsValidTakeTwo(Gems bank, Gems take)
    {
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            if (take == Gems.None.With(color, 2))
            {
                return bank[color] >= 4;
            }
        }

        return false;
    }

    private static KlejnotyState Take(KlejnotyState state, Gems take, bool valid)
    {
        if (!valid)
        {
            throw new IllegalMoveException($"Nie mozna wziac {take} z banku {state.Bank}.");
        }

        var me = state.Current with { Tokens = state.Current.Tokens + take };
        return Continue(With(state, me) with { Bank = state.Bank - take, Passes = 0 });
    }

    private static KlejnotyState Reserve(KlejnotyState state, int cardOrTier, bool fromDeck)
    {
        var me = state.Current;
        if (me.Reserved.Length >= KlejnotyState.MaxReserved)
        {
            throw new IllegalMoveException($"Mozna miec najwyzej {KlejnotyState.MaxReserved} zarezerwowane karty.");
        }

        int card;
        if (fromDeck)
        {
            if (cardOrTier is < 1 or > Cards.Tiers || state.Decks[cardOrTier - 1].IsEmpty)
            {
                throw new IllegalMoveException($"Talia poziomu {cardOrTier} jest pusta albo nie istnieje.");
            }

            var deck = state.Decks[cardOrTier - 1];
            card = deck[^1];
            state = state with { Decks = state.Decks.SetItem(cardOrTier - 1, deck.RemoveAt(deck.Length - 1)) };
        }
        else
        {
            card = cardOrTier;
            state = TakeFromMarket(state, card);
        }

        var gold = state.Bank[Gem.Gold] > 0 ? Gems.One(Gem.Gold) : Gems.None;
        me = me with { Tokens = me.Tokens + gold, Reserved = me.Reserved.Add(new Reservation(card, fromDeck)) };
        return Continue(With(state, me) with { Bank = state.Bank - gold, Passes = 0 });
    }

    private static KlejnotyState Buy(KlejnotyState state, int cardId, bool fromReserved)
    {
        var me = state.Current;
        if (fromReserved)
        {
            var index = me.Reserved.Select(r => r.Card).ToList().IndexOf(cardId);
            if (index < 0)
            {
                throw new IllegalMoveException($"Karta #{cardId} nie jest zarezerwowana.");
            }

            me = me with { Reserved = me.Reserved.RemoveAt(index) };
        }
        else
        {
            state = TakeFromMarket(state, cardId);
        }

        var card = state.Variant.AllCards[cardId];
        var pay = Payment(me, card) ?? throw new IllegalMoveException($"Nie stac na karte #{cardId} ({card.Cost}).");
        me = me with
        {
            Tokens = me.Tokens - pay,
            Bonuses = me.Bonuses + Gems.One(card.Bonus),
            Points = me.Points + card.Points,
            Bought = me.Bought.Add(cardId),
        };
        return EndTurn(With(state, me) with { Bank = state.Bank + pay, Passes = 0 });
    }

    private static KlejnotyState Return(KlejnotyState state, Gems token)
    {
        if (token.Total != 1 || !state.Current.Tokens.Covers(token))
        {
            throw new IllegalMoveException($"Nie mozna oddac {token}.");
        }

        var me = state.Current with { Tokens = state.Current.Tokens - token };
        return Continue(With(state, me) with { Bank = state.Bank + token });
    }

    private static KlejnotyState Pass(KlejnotyState state)
    {
        if (LegalMoves(state)[0].Kind != MoveKind.Pass)
        {
            throw new IllegalMoveException("Pasowac mozna tylko, gdy nie ma innego ruchu.");
        }

        var passes = state.Passes + 1;
        var ended = EndTurn(state with { Passes = passes });
        return passes == state.Players.Length ? ended with { Over = true } : ended;
    }
}
