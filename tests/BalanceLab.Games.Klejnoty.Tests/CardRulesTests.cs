using BalanceLab.Engine;
using static BalanceLab.Games.Klejnoty.Tests.Positions;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class CardRulesTests
{
    [Fact]
    public void Reserving_from_the_market_refills_the_slot_and_gives_gold()
    {
        var start = Start();
        var card = start.FirstOnMarket(2);

        var state = start.Play(KlejnotyMove.Reserve(card.Id));

        Assert.Equal([new Reservation(card.Id, false)], state.Players[0].Reserved.ToArray());
        Assert.Equal(Gems.Of(gold: 1), state.Players[0].Tokens);
        Assert.Equal(4, state.Bank[Gem.Gold]);
        Assert.Equal(start.Decks[1][^1], state.MarketCard(2, 0)!.Id);
        Assert.Equal(start.Decks[1][..^1].ToArray(), state.Decks[1].ToArray());
        Assert.Equal(1, state.ToMove);
    }

    [Fact]
    public void Reserving_from_a_deck_takes_its_top_card_face_down()
    {
        var start = Start();

        var state = start.Play(KlejnotyMove.ReserveFromDeck(3));

        Assert.Equal([new Reservation(start.Decks[2][^1], true)], state.Players[0].Reserved.ToArray());
        Assert.Equal(15, state.Decks[2].Length);
        Assert.Equal(start.Market, state.Market);
    }

    [Fact]
    public void No_gold_left_means_reserving_without_gold()
    {
        var state = (Start() with { Bank = Start().Bank.With(Gem.Gold, 0) }).Play(KlejnotyMove.ReserveFromDeck(1));

        Assert.Equal(Gems.None, state.Players[0].Tokens);
        Assert.Single(state.Players[0].Reserved);
    }

    [Fact]
    public void At_most_three_reserved_cards()
    {
        var state = Start().WithPlayer(0, p => p with { Reserved = [new(0, false), new(1, false), new(2, true)] });

        Assert.DoesNotContain(Game.LegalMoves(state), m => m.Kind is MoveKind.Reserve or MoveKind.ReserveFromDeck);
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.ReserveFromDeck(1)));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Reserve(state.FirstOnMarket(1).Id)));
        Assert.Contains(Game.LegalMoves(state.WithPlayer(0, p => p with { Reserved = p.Reserved.RemoveAt(0) })), m => m.Kind == MoveKind.Reserve);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Reserving_from_a_missing_deck_is_rejected(int tier)
    {
        Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.ReserveFromDeck(tier)));
    }

    [Fact]
    public void Empty_deck_cannot_be_reserved_and_leaves_an_empty_market_slot()
    {
        var start = Start();
        var state = start with { Decks = start.Decks.SetItem(2, []) };
        var card = state.FirstOnMarket(3);

        Assert.DoesNotContain(KlejnotyMove.ReserveFromDeck(3), Game.LegalMoves(state));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.ReserveFromDeck(3)));

        var after = state.Play(KlejnotyMove.Reserve(card.Id));

        Assert.Null(after.MarketCard(3, 0));
        Assert.Equal(11, Game.LegalMoves(after).Count(m => m.Kind == MoveKind.Reserve));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(90)]
    public void Cards_not_on_the_market_cannot_be_taken(int card)
    {
        Assert.Equal($"Karty #{card} nie ma na rynku.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Reserve(card))).Message);
        Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Buy(card)));
    }

    [Fact]
    public void Payment_uses_bonuses_first_then_colors_then_gold_for_the_rest()
    {
        var card = new Card(0, 1, Gem.White, 0, Gems.Of(white: 3, blue: 2, red: 1));
        var player = PlayerBoard.Empty with { Bonuses = Gems.Of(white: 1), Tokens = Gems.Of(white: 5, blue: 1, gold: 2) };

        Assert.Equal(Gems.Of(white: 2, blue: 1, gold: 2), KlejnotyRules.Payment(player, card));
        Assert.Null(KlejnotyRules.Payment(player with { Tokens = Gems.Of(white: 5, blue: 1, gold: 1) }, card));
        Assert.Equal(Gems.None, KlejnotyRules.Payment(PlayerBoard.Empty with { Bonuses = Gems.Of(4, 4, 4, 4, 4) }, card));
        Assert.Throws<ArgumentNullException>(() => KlejnotyRules.Payment(null!, card));
        Assert.Throws<ArgumentNullException>(() => KlejnotyRules.Payment(player, null!));
    }

    [Fact]
    public void Buying_pays_the_bank_and_gives_bonus_points_and_the_card()
    {
        var start = Start();
        var card = start.FirstOnMarket(1);
        var state = start.Give(0, card.Cost);

        var after = state.Play(KlejnotyMove.Buy(card.Id));
        var me = after.Players[0];

        Assert.Contains(KlejnotyMove.Buy(card.Id), Game.LegalMoves(state));
        Assert.Equal((Gems.None, Gems.One(card.Bonus), card.Points), (me.Tokens, me.Bonuses, me.Points));
        Assert.Equal([card.Id], me.Bought.ToArray());
        Assert.Equal(start.Bank, after.Bank);
        Assert.Equal(start.Decks[0][^1], after.MarketCard(1, 0)!.Id);
        Assert.Equal(1, after.ToMove);
    }

    [Fact]
    public void Cards_out_of_reach_cannot_be_bought()
    {
        var card = Start().FirstOnMarket(3);

        var ex = Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Buy(card.Id)));

        Assert.Equal($"Nie stac na karte #{card.Id} ({card.Cost}).", ex.Message);
        Assert.DoesNotContain(Game.LegalMoves(Start()), m => m.Kind is MoveKind.Buy or MoveKind.BuyReserved);
    }

    [Fact]
    public void Reserved_card_is_bought_from_the_hand()
    {
        var card = Cards.All[89];
        var state = (Start() with { Patrons = [] }).WithPlayer(0, p => p with { Reserved = [new(5, false), new(card.Id, true)], Bonuses = card.Cost });

        var after = state.Play(KlejnotyMove.BuyReserved(card.Id));

        Assert.Contains(KlejnotyMove.BuyReserved(card.Id), Game.LegalMoves(state));
        Assert.DoesNotContain(KlejnotyMove.BuyReserved(5), Game.LegalMoves(state));
        Assert.Equal([new Reservation(5, false)], after.Players[0].Reserved.ToArray());
        Assert.Equal(5, after.Players[0].Points);
        Assert.Equal([89], after.Players[0].Bought.ToArray());
        Assert.Equal(state.Market, after.Market);
        Assert.Equal($"Karta #{state.FirstOnMarket(1).Id} nie jest zarezerwowana.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.BuyReserved(state.FirstOnMarket(1).Id))).Message);
    }

    [Fact]
    public void Patron_visits_once_the_bonuses_are_there()
    {
        var start = Start();
        var patron = Cards.Patrons[start.Patrons[0]];
        // Brakuje jednego rabatu w kolorze, ktory da zarezerwowana karta.
        var missing = Enum.GetValues<Gem>().First(g => patron.Requirement[g] > 0);
        var card = Cards.All.First(c => c.Bonus == missing && c.Tier == 1);
        var state = start.WithPlayer(0, p => p with { Bonuses = patron.Requirement - Gems.One(missing), Reserved = [new(card.Id, false)] })
            .Give(0, card.Cost);

        Assert.Empty(state.Play(KlejnotyMove.ReserveFromDeck(1)).Players[0].Patrons);

        var after = state.Play(KlejnotyMove.BuyReserved(card.Id));

        Assert.Equal([patron.Id], after.Players[0].Patrons.ToArray());
        Assert.Equal(card.Points + patron.Points, after.Players[0].Points);
        Assert.DoesNotContain(patron.Id, after.Patrons);
        Assert.Equal(start.Patrons.Length - 1, after.Patrons.Length);
    }

    [Fact]
    public void Only_one_patron_per_turn()
    {
        var start = Start(3, 2);
        var everything = Gems.Of(9, 9, 9, 9, 9);

        var after = start.WithPlayer(0, p => p with { Bonuses = everything }).Play(KlejnotyMove.ReserveFromDeck(1));
        var next = after.Play(KlejnotyMove.ReserveFromDeck(1), KlejnotyMove.ReserveFromDeck(1), KlejnotyMove.ReserveFromDeck(1));

        Assert.Equal([start.Patrons[0]], after.Players[0].Patrons.ToArray());
        Assert.Equal([start.Patrons[0], start.Patrons[1]], next.Players[0].Patrons.ToArray());
        Assert.Equal(6, next.Players[0].Points);
    }
}
