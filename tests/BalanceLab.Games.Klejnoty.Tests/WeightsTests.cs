namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class WeightsTests
{
    private static readonly KlejnotyWeights Zero = new(0, 0, 0, 0, 0, 0, 0, 0);

    // Gracz 0: 3 punkty, 2 rabaty (W, U), 12 zetonow (2 zlote), 1 karta zarezerwowana.
    private static readonly KlejnotyState Position = Positions.Start(seed: 4)
        .Give(0, Gems.Of(white: 3, blue: 3, green: 2, red: 1, black: 1, gold: 2))
        .WithPlayer(0, p => p with { Points = 3, Bonuses = Gems.Of(white: 1, blue: 1), Reserved = [new Reservation(89, false)] });

    [Theory]
    [InlineData("Points", 3.0)]
    [InlineData("Bonuses", 2.0)]
    [InlineData("Gold", 2.0)]
    [InlineData("Tokens", 10.0)]
    [InlineData("Reserved", 1.0)]
    [InlineData("Overflow", -2.0)]
    public void Each_weight_scales_exactly_its_own_term(string weight, double term)
    {
        var only = Only(weight, 2);

        Assert.Equal(2 * term, new KlejnotyEvaluator(only).Evaluate(Position, 0), 9);
    }

    [Fact]
    public void Target_and_patron_weights_scale_their_terms()
    {
        var target = new KlejnotyEvaluator(Zero with { Target = 1 }).Evaluate(Position, 0);
        var patrons = new KlejnotyEvaluator(Zero with { Patrons = 1 }).Evaluate(Position, 0);

        Assert.True(target > 0);
        Assert.Equal(2 * target, new KlejnotyEvaluator(Zero with { Target = 2 }).Evaluate(Position, 0), 9);
        Assert.True(patrons > 0); // rabaty W i U licza sie do patronow, ktorzy ich wymagaja
        Assert.Equal(3 * patrons, new KlejnotyEvaluator(Zero with { Patrons = 3 }).Evaluate(Position, 0), 9);
        Assert.Equal(0, new KlejnotyEvaluator(Zero).Evaluate(Position, 0));
    }

    [Fact]
    public void Default_evaluator_plays_the_balanced_profile()
    {
        Assert.Same(KlejnotyWeights.Balanced, new KlejnotyEvaluator().Weights);
        Assert.Equal(new KlejnotyWeights(20, 6, 3, 2, 1, 3, 1, 4), KlejnotyWeights.Balanced);
        Assert.Equal(
            new KlejnotyEvaluator(KlejnotyWeights.Profiles["wyrownany"]).Evaluate(Position, 0),
            new KlejnotyEvaluator().Evaluate(Position, 0));
    }

    [Fact]
    public void Profiles_differ_from_the_balanced_one_in_their_namesake()
    {
        var p = KlejnotyWeights.Profiles;

        Assert.Equal(["patroni", "punkty", "silnik", "wyrownany", "zetony"], p.Keys);
        Assert.True(p["punkty"].Points > KlejnotyWeights.Balanced.Points);
        Assert.True(p["silnik"].Bonuses > KlejnotyWeights.Balanced.Bonuses);
        Assert.True(p["patroni"].Patrons > KlejnotyWeights.Balanced.Patrons);
        Assert.True(p["zetony"].Tokens > KlejnotyWeights.Balanced.Tokens);
        Assert.Equal(5, p.Values.Distinct().Count());
    }

    private static KlejnotyWeights Only(string weight, double value) => weight switch
    {
        "Points" => Zero with { Points = value },
        "Bonuses" => Zero with { Bonuses = value },
        "Gold" => Zero with { Gold = value },
        "Tokens" => Zero with { Tokens = value },
        "Reserved" => Zero with { Reserved = value },
        _ => Zero with { Overflow = value },
    };
}
