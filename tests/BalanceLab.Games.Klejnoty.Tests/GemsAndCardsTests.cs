namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class GemsAndCardsTests
{
    [Fact]
    public void Gems_keep_each_kind_separately()
    {
        var gems = Gems.Of(white: 1, blue: 2, green: 3, red: 4, black: 5, gold: 255);

        Assert.Equal([1, 2, 3, 4, 5, 255], Enum.GetValues<Gem>().Select(g => gems[g]));
        Assert.Equal(270, gems.Total);
        Assert.Equal(Gems.Of(blue: 7), Gems.Of(blue: 3).With(Gem.Blue, 7));
        Assert.Equal(Gems.Of(red: 1, gold: 1), Gems.One(Gem.Red) + Gems.One(Gem.Gold));
        Assert.Equal(Gems.Of(green: 2), Gems.FromColors([0, 0, 2]));
    }

    [Fact]
    public void Adding_and_subtracting_work_on_every_kind_at_once()
    {
        var a = Gems.Of(1, 2, 3, 4, 5, 6);
        var b = Gems.Of(1, 1, 1, 1, 1, 1);

        Assert.Equal(Gems.Of(2, 3, 4, 5, 6, 7), a + b);
        Assert.Equal(Gems.Of(0, 1, 2, 3, 4, 5), a - b);
        Assert.Equal(a + b, Gems.Add(a, b));
        Assert.Equal(a - b, Gems.Subtract(a, b));
    }

    [Fact]
    public void Subtracting_more_than_there_is_fails_instead_of_borrowing()
    {
        // Bez sprawdzenia odejmowanie "pozyczyloby" z sasiedniego bajtu i po cichu zepsulo inny kolor.
        var ex = Assert.Throws<InvalidOperationException>(() => Gems.Of(blue: 1) - Gems.Of(white: 1));

        Assert.Equal("Nie mozna odjac W1 od U1.", ex.Message);
    }

    [Fact]
    public void Covers_needs_every_kind()
    {
        Assert.True(Gems.Of(2, 2).Covers(Gems.Of(2, 1)));
        Assert.True(Gems.Of(2, 2).Covers(Gems.Of(2, 2)));
        Assert.False(Gems.Of(2, 2).Covers(Gems.Of(gold: 1)));
        Assert.False(Gems.Of(2, 2).Covers(Gems.Of(3)));
        Assert.True(Gems.None.Covers(Gems.None));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void Count_must_fit_in_a_byte(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gems.None.With(Gem.Red, count));
    }

    [Fact]
    public void Text_lists_only_present_kinds()
    {
        Assert.Equal("W1 U2 G3 R4 K5 Z6", Gems.Of(1, 2, 3, 4, 5, 6).ToString());
        Assert.Equal("K2 Z1", Gems.Of(black: 2, gold: 1).ToString());
        Assert.Equal("-", Gems.None.ToString());
    }

    [Fact]
    public void Card_set_has_forty_thirty_and_twenty_cards_by_tier()
    {
        Assert.Equal(90, Cards.All.Length);
        Assert.Equal(Enumerable.Range(0, 90), Cards.All.Select(c => c.Id));
        Assert.Equal([40, 30, 20], Enumerable.Range(1, 3).Select(t => Cards.OfTier(t).Length));
        Assert.Equal(Cards.OfTier(2).Order(), Cards.OfTier(2));
        Assert.All(Cards.All, c => Assert.Equal(0, c.Cost[Gem.Gold]));
    }

    [Fact]
    public void Every_color_gets_the_same_cards_with_rotated_costs()
    {
        // Symetria kolorow: dla kazdego poziomu i koloru te same punkty i ta sama laczna cena.
        foreach (var tier in Enumerable.Range(1, 3))
        {
            var byColor = Cards.All.Where(c => c.Tier == tier).GroupBy(c => c.Bonus)
                .Select(g => (Count: g.Count(), Points: g.Sum(c => c.Points), Cost: g.Sum(c => c.Cost.Total))).ToArray();

            Assert.Equal(5, byColor.Length);
            Assert.Single(byColor.Distinct());
        }

        // Wzor jest liczony od koloru karty: pierwsza biala karta to 0 bialych + po 1 z reszty.
        Assert.Equal(Gems.Of(0, 1, 1, 1, 1), Cards.All[0].Cost);
        Assert.Equal(Gems.Of(1, 0, 1, 1, 1), Cards.All[8].Cost);
        Assert.Equal((Gem.Blue, 1, Gems.Of(blue: 0, green: 0, red: 0, black: 0, white: 4)), (Cards.All[15].Bonus, Cards.All[15].Points, Cards.All[15].Cost));
        Assert.Equal((3, Gem.Black, 5), (Cards.All[89].Tier, Cards.All[89].Bonus, Cards.All[89].Points));
        Assert.Equal(Gems.Of(black: 3, red: 7), Cards.All[89].Cost);
    }

    [Fact]
    public void Patrons_want_two_colors_by_four_or_three_by_three()
    {
        Assert.Equal(10, Cards.Patrons.Length);
        Assert.Equal(Enumerable.Range(0, 10), Cards.Patrons.Select(p => p.Id));
        Assert.All(Cards.Patrons, p => Assert.Equal(3, p.Points));
        Assert.Equal(Gems.Of(white: 4, blue: 4), Cards.Patrons[0].Requirement);
        Assert.Equal(Gems.Of(black: 4, white: 4), Cards.Patrons[4].Requirement);
        Assert.Equal(Gems.Of(red: 3, black: 3, white: 3), Cards.Patrons[8].Requirement);
    }
}
