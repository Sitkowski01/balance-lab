using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

public static partial class KlejnotyRules
{
    /// <summary>Za duzo zetonow: ten sam gracz oddaje nadmiar, zanim tura przejdzie dalej.</summary>
    private static KlejnotyState Continue(KlejnotyState state) =>
        state.Current.Tokens.Total > KlejnotyState.MaxTokens ? state : EndTurn(state);

    /// <summary>Wizyta patrona (pierwszego spelnionego), sprawdzenie konca gry i nastepny gracz.</summary>
    private static KlejnotyState EndTurn(KlejnotyState state)
    {
        var me = state.Current;
        var patrons = state.Patrons;
        var visitor = patrons.FirstOrDefault(id => me.Bonuses.Covers(Cards.Patrons[id].Requirement), -1);
        if (visitor >= 0)
        {
            me = me with { Points = me.Points + Cards.Patrons[visitor].Points, Patrons = me.Patrons.Add(visitor) };
            patrons = patrons.Remove(visitor);
        }

        var finalRound = state.FinalRound || me.Points >= KlejnotyState.WinningPoints;
        var next = (state.ToMove + 1) % state.Players.Length;
        return With(state, me) with { Patrons = patrons, ToMove = next, FinalRound = finalRound, Over = finalRound && next == 0 };
    }

    private static KlejnotyState TakeFromMarket(KlejnotyState state, int card)
    {
        var index = card < 0 ? -1 : state.Market.IndexOf(card);
        if (index < 0)
        {
            throw new IllegalMoveException($"Karty #{card} nie ma na rynku.");
        }

        var tier = Cards.All[card].Tier;
        var deck = state.Decks[tier - 1];
        if (deck.IsEmpty)
        {
            return state with { Market = state.Market.SetItem(index, -1) };
        }

        return state with
        {
            Market = state.Market.SetItem(index, deck[^1]),
            Decks = state.Decks.SetItem(tier - 1, deck.RemoveAt(deck.Length - 1)),
        };
    }

    private static KlejnotyState With(KlejnotyState state, PlayerBoard current) =>
        state with { Players = state.Players.SetItem(state.ToMove, current) };
}
