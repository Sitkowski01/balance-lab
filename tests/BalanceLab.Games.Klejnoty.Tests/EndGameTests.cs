using BalanceLab.Engine;
using static BalanceLab.Games.Klejnoty.Tests.Positions;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class EndGameTests
{
    private static readonly KlejnotyMove Wait = KlejnotyMove.ReserveFromDeck(1);

    [Fact]
    public void Fifteen_points_finish_the_round_but_not_the_game_at_once()
    {
        var state = Start(3).WithPlayer(0, p => p with { Points = 15 });

        var afterFirst = state.Play(Wait);
        var afterSecond = afterFirst.Play(Wait);
        var afterLast = afterSecond.Play(Wait);

        Assert.Equal((true, false), (afterFirst.FinalRound, afterFirst.Over));
        Assert.False(Game.IsTerminal(afterSecond));
        Assert.True(Game.IsTerminal(afterLast));
        Assert.Empty(Game.LegalMoves(afterLast));
        Assert.Equal("Partia sie skonczyla.", Assert.Throws<IllegalMoveException>(() => Game.Apply(afterLast, Wait)).Message);
    }

    [Fact]
    public void Last_player_reaching_fifteen_ends_the_game_immediately()
    {
        var state = Start(2).Play(Wait).WithPlayer(1, p => p with { Points = 15 });

        Assert.True(state.Play(Wait).Over);
        Assert.False(Start(2).Play(Wait, Wait).FinalRound);
    }

    public static TheoryData<int[], int[], double[]> Finals => new()
    {
        { [16, 15, 3], [5, 5, 5], [1, 0, 0] },
        { [16, 16, 3], [7, 5, 5], [0, 1, 0] },
        { [16, 16, 16], [5, 6, 5], [0.5, 0, 0.5] },
        { [0, 0, 0], [0, 0, 0], [1.0 / 3, 1.0 / 3, 1.0 / 3] },
    };

    [Theory]
    [MemberData(nameof(Finals))]
    public void Most_points_win_and_fewer_cards_break_ties(int[] points, int[] cards, double[] expected)
    {
        var state = Start(3) with { Over = true };
        for (var p = 0; p < 3; p++)
        {
            var (score, bought) = (points[p], cards[p]);
            state = state.WithPlayer(p, b => b with { Points = score, Bought = [.. Enumerable.Range(0, bought)] });
        }

        Assert.Equal(expected, Game.Scores(state));
    }

    [Fact]
    public void No_scores_before_the_end()
    {
        Assert.Equal("Partia jeszcze trwa.", Assert.Throws<InvalidOperationException>(() => Game.Scores(Start())).Message);
    }

    [Fact]
    public void Pass_is_the_only_move_when_nothing_else_is_possible_and_all_passing_ends_the_game()
    {
        var stuck = Start() with { Bank = Gems.None, Market = [.. Enumerable.Repeat(-1, 12)], Decks = [[], [], []] };

        var once = stuck.Play(KlejnotyMove.Pass);
        var twice = once.Play(KlejnotyMove.Pass);

        Assert.Equal([KlejnotyMove.Pass], Game.LegalMoves(stuck));
        Assert.Equal((1, 1, false), (once.Passes, once.ToMove, once.Over));
        Assert.Equal((2, true), (twice.Passes, twice.Over));
        Assert.Equal([0.5, 0.5], Game.Scores(twice).ToArray());
        Assert.Equal("Pasowac mozna tylko, gdy nie ma innego ruchu.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Pass)).Message);
    }

    [Fact]
    public void Any_other_move_resets_the_pass_counter()
    {
        var state = Start() with { Passes = 1 };

        Assert.Equal(0, state.Play(Wait).Passes);
        Assert.Equal(0, state.Play(KlejnotyMove.TakeTwo(Gem.Red)).Passes);
        Assert.Equal(0, state.Give(0, Cards.All[state.Market[0]].Cost).Play(KlejnotyMove.Buy(state.Market[0])).Passes);
    }

    [Fact]
    public void Unknown_move_kind_is_rejected()
    {
        Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), new KlejnotyMove((MoveKind)99, Gems.None, -1, 0)));
        Assert.Throws<ArgumentNullException>(() => Game.Apply(null!, Wait));
    }

    [Fact]
    public void Hash_sees_every_part_of_the_state()
    {
        var start = Start(2);
        KlejnotyState[] variants =
        [
            start,
            start with { Bank = start.Bank.With(Gem.Gold, 4) },
            start with { Market = start.Market.SetItem(0, -1) },
            start with { Decks = start.Decks.SetItem(0, start.Decks[0].RemoveAt(0)) },
            start with { Patrons = start.Patrons.RemoveAt(0) },
            start with { ToMove = 1 },
            start with { Passes = 1 },
            start with { FinalRound = true },
            start with { Over = true },
            start.WithPlayer(1, p => p with { Tokens = Gems.One(Gem.Red) }),
            start.WithPlayer(1, p => p with { Bonuses = Gems.One(Gem.Red) }),
            start.WithPlayer(1, p => p with { Points = 1 }),
            start.WithPlayer(1, p => p with { Bought = [3] }),
            start.WithPlayer(1, p => p with { Patrons = [3] }),
            start.WithPlayer(1, p => p with { Reserved = [new(3, false)] }),
            start.WithPlayer(1, p => p with { Reserved = [new(3, true)] }),
        ];

        Assert.Equal(variants.Length, variants.Select(Game.Hash).Distinct().Count());
        Assert.Throws<ArgumentNullException>(() => Game.Hash(null!));
    }

    [Fact]
    public void Moves_read_well_in_logs()
    {
        Assert.Equal("TakeDifferent W1 U1 G1", KlejnotyMove.Take(Gems.Of(1, 1, 1)).ToString());
        Assert.Equal("TakeTwo R2", KlejnotyMove.TakeTwo(Gem.Red).ToString());
        Assert.Equal("Return Z1", KlejnotyMove.Return(Gem.Gold).ToString());
        Assert.Equal("ReserveFromDeck 2", KlejnotyMove.ReserveFromDeck(2).ToString());
        Assert.Equal("Buy #7", KlejnotyMove.Buy(7).ToString());
        Assert.Equal("BuyReserved #8", KlejnotyMove.BuyReserved(8).ToString());
        Assert.Equal("Reserve #9", KlejnotyMove.Reserve(9).ToString());
        Assert.Equal("Pass", KlejnotyMove.Pass.ToString());
    }
}
