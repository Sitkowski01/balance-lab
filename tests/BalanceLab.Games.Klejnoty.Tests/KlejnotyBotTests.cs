using BalanceLab.Bots;
using BalanceLab.Engine;
using static BalanceLab.Games.Klejnoty.Tests.Positions;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class KlejnotyBotTests
{
    private static readonly KlejnotyEvaluator Evaluator = new();

    [Fact]
    public void Finished_game_is_worth_its_score()
    {
        var won = (Start() with { Over = true }).WithPlayer(1, p => p with { Points = 15 });

        Assert.Equal(0, Evaluator.Evaluate(won, 0));
        Assert.Equal(10_000, Evaluator.Evaluate(won, 1));
    }

    [Fact]
    public void Points_count_more_than_bonuses_and_bonuses_more_than_tokens()
    {
        var start = Start() with { Patrons = [] };
        double Value(Func<PlayerBoard, PlayerBoard> change) => Evaluator.Evaluate(start.WithPlayer(0, change), 0);

        Assert.True(Value(p => p with { Points = 1 }) > Value(p => p with { Bonuses = Gems.Of(red: 3) }));
        Assert.True(Value(p => p with { Bonuses = Gems.Of(red: 1) }) > Value(p => p with { Tokens = Gems.Of(red: 2) }));
        Assert.True(Value(p => p with { Tokens = Gems.Of(gold: 1) }) > Value(p => p with { Tokens = Gems.Of(red: 1) }));
        Assert.Equal(Value(p => p), Evaluator.Evaluate(start.WithPlayer(1, p => p with { Points = 9 }), 0));
    }

    [Fact]
    public void Closer_patron_is_worth_more()
    {
        var patron = Cards.Patrons[0];
        var state = Start() with { Patrons = [0], Market = [.. Enumerable.Repeat(-1, 12)] };

        var half = Evaluator.Evaluate(state.WithPlayer(0, p => p with { Bonuses = Gems.Of(white: 4) }), 0);
        var other = Evaluator.Evaluate(state.WithPlayer(0, p => p with { Bonuses = Gems.Of(green: 4) }), 0);

        Assert.Equal(6.0 * 4, other);
        Assert.Equal((6.0 * 4) + (4.0 * 4 / patron.Requirement.Total), half);
    }

    [Fact]
    public void Shortfall_counts_missing_tokens_after_bonuses_and_gold()
    {
        var card = new Card(0, 1, Gem.White, 0, Gems.Of(white: 3, blue: 2));
        var player = PlayerBoard.Empty with { Bonuses = Gems.Of(white: 1), Tokens = Gems.Of(blue: 1, gold: 1) };

        Assert.Equal(2, KlejnotyEvaluator.Shortfall(player, card));
        Assert.Equal(0, KlejnotyEvaluator.Shortfall(player with { Tokens = Gems.Of(gold: 9) }, card));
        Assert.Throws<ArgumentNullException>(() => KlejnotyEvaluator.Shortfall(null!, card));
        Assert.Throws<ArgumentNullException>(() => KlejnotyEvaluator.Shortfall(player, null!));
        Assert.Throws<ArgumentNullException>(() => Evaluator.Evaluate(null!, 0));
    }

    [Fact]
    public void Greedy_bot_buys_a_point_card_it_can_afford()
    {
        var card = Cards.All.First(c => c.Tier == 1 && c.Points == 1);
        var state = (Start() with { Market = Start().Market.SetItem(0, card.Id) }).Give(0, card.Cost);

        var move = new GreedyBot<KlejnotyState, KlejnotyMove>(Evaluator, 1).ChooseMove(Game, state);

        Assert.Equal(KlejnotyMove.Buy(card.Id), move);
    }

    [Fact]
    [Trait("Category", "Strength")]
    public void Greedy_beats_random_and_ismcts_beats_greedy()
    {
        var greedyVsRandom = Duel(s => new GreedyBot<KlejnotyState, KlejnotyMove>(Evaluator, s), s => new RandomBot<KlejnotyState, KlejnotyMove>(s), 100);
        var ismctsVsGreedy = Duel(
            s => new IsmctsBot<KlejnotyState, KlejnotyMove>(s, new MctsOptions { Iterations = 300 }),
            s => new GreedyBot<KlejnotyState, KlejnotyMove>(Evaluator, s),
            10);

        Assert.True(greedyVsRandom >= 0.75, $"greedy z losowym: {greedyVsRandom}");
        Assert.True(ismctsVsGreedy >= 0.6, $"ISMCTS z greedy: {ismctsVsGreedy}");
    }

    /// <summary>Sredni wynik pierwszego bota; miejsca na zmiane, zeby przewaga zaczynajacego sie znosila.</summary>
    private static double Duel(Func<ulong, IPlayer<KlejnotyState, KlejnotyMove>> a, Func<ulong, IPlayer<KlejnotyState, KlejnotyMove>> b, int games)
    {
        var total = 0.0;
        for (var seed = 0UL; seed < (ulong)games; seed++)
        {
            var seat = (int)(seed % 2);
            IPlayer<KlejnotyState, KlejnotyMove>[] players = seat == 0 ? [a(seed), b(seed + 1_000)] : [b(seed + 1_000), a(seed)];
            total += Match.Play(Game, new GameSetup(2, seed), players).Scores[seat];
        }

        return total / games;
    }
}
