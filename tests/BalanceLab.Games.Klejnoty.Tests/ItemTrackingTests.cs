using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class ItemTrackingTests
{
    [Fact]
    public void Items_for_the_balance_report_are_bought_cards_described_by_the_variant()
    {
        var game = new KlejnotyGame(KlejnotyVariant.Parse("card17.points=4"));
        var state = game.Setup(new GameSetup(2, 1)).WithPlayer(1, p => p with { Bought = [17, 3], Reserved = [new Reservation(5, false)] });

        Assert.Equal(90, game.ItemCount);
        Assert.Equal("#17 poz.1 G 4 pkt, koszt W2 U1 R1 K1", game.ItemName(17)); // punkty z wariantu
        Assert.Equal([17, 3], game.ItemsOf(state, 1));
        Assert.Empty(game.ItemsOf(state, 0));
    }
}
