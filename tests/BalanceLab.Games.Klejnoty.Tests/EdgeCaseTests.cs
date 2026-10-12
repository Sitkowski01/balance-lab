using BalanceLab.Engine;
using static BalanceLab.Games.Klejnoty.Tests.Positions;

namespace BalanceLab.Games.Klejnoty.Tests;

/// <summary>Brzegi, ktore losowe partie trafiaja rzadko: karta i patron o id 0, puste sloty, komunikaty.</summary>
public sealed class EdgeCaseTests
{
    [Fact]
    public void Game_describes_itself()
    {
        Assert.Equal(("Klejnoty", 2, 4), (Game.Name, Game.MinPlayers, Game.MaxPlayers));
    }

    [Fact]
    public void Card_zero_on_the_market_is_a_normal_card()
    {
        var state = Start() with { Market = Start().Market.SetItem(0, 0) };
        var rich = state.Give(0, Cards.All[0].Cost);

        Assert.Equal(Cards.All[0], state.MarketCard(1, 0));
        Assert.Contains(KlejnotyMove.Reserve(0), Game.LegalMoves(state));
        Assert.Contains(KlejnotyMove.Buy(0), Game.LegalMoves(rich));
    }

    [Fact]
    public void Empty_market_slot_cannot_be_taken_as_card_minus_one()
    {
        var state = Start() with { Market = Start().Market.SetItem(5, -1) };

        Assert.Null(state.MarketCard(2, 1));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Reserve(-1)));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Buy(-1)));
    }

    [Fact]
    public void Patron_zero_visits_too()
    {
        var state = (Start() with { Patrons = [0] }).WithPlayer(0, p => p with { Bonuses = Cards.Patrons[0].Requirement });

        var after = state.Play(KlejnotyMove.ReserveFromDeck(1));

        Assert.Equal([0], after.Players[0].Patrons.ToArray());
        Assert.Empty(after.Patrons);
    }

    [Fact]
    public void Taking_nothing_is_not_a_move_even_with_an_empty_bank()
    {
        var state = Start() with { Bank = Gems.Of(gold: 5) };

        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Take(Gems.None)));
    }

    [Fact]
    public void Rejections_explain_the_rule()
    {
        var full = Start().WithPlayer(0, p => p with { Reserved = [new(0, false), new(1, false), new(2, false)] });
        var over = Start().Give(0, Gems.Of(white: 4, blue: 4, green: 3));

        Assert.Equal("Mozna miec najwyzej 3 zarezerwowane karty.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(full, KlejnotyMove.ReserveFromDeck(1))).Message);
        Assert.Equal("Talia poziomu 4 jest pusta albo nie istnieje.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.ReserveFromDeck(4))).Message);
        Assert.Equal("Nie mozna oddac R1.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(over, KlejnotyMove.Return(Gem.Red))).Message);
        Assert.Equal("Nieznany ruch 99.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), new KlejnotyMove((MoveKind)99, Gems.None, -1, 0))).Message);
    }

    [Fact]
    public void Move_factories_fill_unused_fields_with_neutral_values()
    {
        Assert.Equal(new KlejnotyMove(MoveKind.TakeDifferent, Gems.Of(1, 1, 1), -1, 0), KlejnotyMove.Take(Gems.Of(1, 1, 1)));
        Assert.Equal(new KlejnotyMove(MoveKind.TakeTwo, Gems.Of(red: 2), -1, 0), KlejnotyMove.TakeTwo(Gem.Red));
        Assert.Equal(new KlejnotyMove(MoveKind.ReserveFromDeck, Gems.None, -1, 2), KlejnotyMove.ReserveFromDeck(2));
        Assert.Equal(new KlejnotyMove(MoveKind.Return, Gems.One(Gem.Gold), -1, 0), KlejnotyMove.Return(Gem.Gold));
        Assert.Equal(new KlejnotyMove(MoveKind.Pass, Gems.None, -1, 0), KlejnotyMove.Pass);
    }

    [Fact]
    public void Hash_tells_a_hidden_card_from_its_neighbour()
    {
        var start = Start();

        Assert.NotEqual(
            Game.Hash(start.WithPlayer(1, p => p with { Reserved = [new(3, true)] })),
            Game.Hash(start.WithPlayer(1, p => p with { Reserved = [new(4, false)] })));
    }

    [Fact]
    public void Every_tier_is_reshuffled_for_the_observer()
    {
        var state = Start(2, 4);

        var views = Enumerable.Range(0, 5).Select(s => Game.Determinize(state, 0, (ulong)s)).ToArray();

        Assert.All(Enumerable.Range(0, 3), tier => Assert.Contains(views, v => !v.Decks[tier].SequenceEqual(state.Decks[tier])));
        Assert.All(views, v => Assert.Equal(state.Market.ToArray(), v.Market.ToArray()));
    }
}
