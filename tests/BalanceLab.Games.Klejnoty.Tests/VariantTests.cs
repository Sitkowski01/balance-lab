using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class VariantTests
{
    [Fact]
    public void Standard_is_the_original_set_of_rules()
    {
        var standard = KlejnotyVariant.Standard;

        Assert.Equal("standard", standard.Name);
        Assert.Equal(Cards.All.ToArray(), standard.AllCards.ToArray());
        Assert.Equal(Cards.Patrons.ToArray(), standard.AllPatrons.ToArray());
        Assert.Equal(15, standard.WinningPoints);
        Assert.Same(standard, new KlejnotyGame().Variant);
    }

    [Fact]
    public void Builders_reject_values_the_rules_cannot_handle()
    {
        var standard = KlejnotyVariant.Standard;

        Assert.Throws<ArgumentException>(() => standard.WithCard(0, Gems.Of(gold: 1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => standard.WithCard(0, Gems.None, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => standard.WithCard(-1, Gems.None, 0));
        Assert.Throws<ArgumentException>(() => standard.WithPatron(0, Gems.Of(white: 1, gold: 1), 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => standard.WithPatron(0, Gems.One(Gem.White), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => standard.WithPatron(-1, Gems.One(Gem.White), 3));
        Assert.Throws<ArgumentException>(() => standard.Named(" "));
        Assert.Equal(1, standard.WithWinningPoints(1).WinningPoints);
    }

    [Fact]
    public void Same_seed_deals_the_same_layout_in_every_variant()
    {
        var setup = new GameSetup(3, 42);
        var original = new KlejnotyGame().Setup(setup);
        var changed = new KlejnotyGame(KlejnotyVariant.Parse("card5.points=3; patron1.points=5; win=10")).Setup(setup);

        Assert.Equal(original.Market.ToArray(), changed.Market.ToArray());
        Assert.Equal(original.Decks.Select(d => d.ToArray()), changed.Decks.Select(d => d.ToArray()));
        Assert.Equal(original.Patrons.ToArray(), changed.Patrons.ToArray());
        Assert.Equal("card5.points=3; patron1.points=5; win=10", changed.Variant.Name);
    }

    [Fact]
    public void Changed_cost_points_and_threshold_drive_the_rules()
    {
        var target = Positions.Start().FirstOnMarket(1);
        var variant = KlejnotyVariant.Standard.WithCard(target.Id, Gems.None, 2).WithWinningPoints(2);
        var game = new KlejnotyGame(variant);
        var start = game.Setup(new GameSetup(2, 1));

        Assert.Equal(variant.AllCards[target.Id], start.MarketCard(1, 0));
        Assert.Contains(KlejnotyMove.Buy(target.Id), game.LegalMoves(start)); // za darmo, bez zetonow
        var after = game.Apply(start, KlejnotyMove.Buy(target.Id));
        Assert.Equal(2, after.Players[0].Points);
        Assert.True(after.FinalRound);
        Assert.Same(variant, after.Variant);
    }

    [Fact]
    public void Changed_patron_visits_with_its_new_points()
    {
        var patron = Positions.Start().Patrons[0];
        var game = new KlejnotyGame(KlejnotyVariant.Standard.WithPatron(patron, Gems.One(Gem.White), 7));
        var start = game.Setup(new GameSetup(2, 1)).WithPlayer(0, p => p with { Bonuses = Gems.One(Gem.White) });

        var after = game.Apply(start, game.LegalMoves(start)[0]);

        Assert.Equal([patron], after.Players[0].Patrons.ToArray());
        Assert.Equal(7, after.Players[0].Points);
    }

    [Fact]
    public void Determinized_view_plays_by_the_same_variant()
    {
        var variant = KlejnotyVariant.Parse("win=9");
        var state = new KlejnotyGame(variant).Setup(new GameSetup(2, 3));

        Assert.Same(variant, KlejnotyRules.Determinize(state, 1, 5).Variant);
    }
}
