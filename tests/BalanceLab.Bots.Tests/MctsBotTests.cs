using BalanceLab.Engine;
using BalanceLab.Testing;

namespace BalanceLab.Bots.Tests;

public sealed class MctsBotTests
{
    private static readonly NimGame Nim = new();

    [Theory]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(9, 1)]
    [InlineData(11, 3)]
    public void Finds_the_perfect_move_in_two_player_nim(int pile, int winningMove)
    {
        // Teoria Nim: wygrywa, kto zostawia przeciwnikowi wielokrotnosc 4.
        var bot = new MctsBot<NimState, int>(1, new MctsOptions { Iterations = 3_000 });

        Assert.Equal(winningMove, bot.ChooseMove(Nim, new NimState(pile, 2, 0, null)));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Takes_an_immediate_win_whatever_the_number_of_players(int players)
    {
        var bot = new MctsBot<NimState, int>(2, new MctsOptions { Iterations = 200 });

        Assert.Equal(3, bot.ChooseMove(Nim, new NimState(3, players, players - 1, null)));
    }

    [Fact]
    public void Statistics_account_for_every_iteration()
    {
        var bot = new MctsBot<NimState, int>(3, new MctsOptions { Iterations = 500 });

        var search = bot.Search(Nim, new NimState(10, 2, 0, null));

        Assert.Equal(500, search.Iterations);
        Assert.Equal(500, search.Moves.Sum(m => m.Visits));
        Assert.Equal([1, 2, 3], search.Moves.Select(m => m.Move).Order());
        Assert.Equal(search.Moves[0].Move, search.BestMove);
        Assert.Equal(search.Moves.Max(m => m.Visits), search.Moves[0].Visits);
        Assert.All(search.Moves, m => Assert.InRange(m.MeanScore, 0.0, 1.0));
    }

    [Fact]
    public void Winning_move_has_the_best_mean_score()
    {
        var bot = new MctsBot<NimState, int>(4, new MctsOptions { Iterations = 2_000 });

        var search = bot.Search(Nim, new NimState(5, 2, 0, null));

        Assert.Equal(1, search.Moves.MaxBy(m => m.MeanScore)!.Move);
        Assert.True(search.Moves.Single(m => m.Move == 1).MeanScore > 0.9);
    }

    [Fact]
    public void Equal_visits_are_ordered_by_mean_score()
    {
        // Trzy iteracje = kazdy z trzech ruchow sprawdzony raz; o kolejnosci decyduje wynik.
        var search = new MctsBot<NimState, int>(11, new MctsOptions { Iterations = 3 }).Search(Nim, new NimState(10, 2, 0, null));

        Assert.All(search.Moves, m => Assert.Equal(1, m.Visits));
        Assert.Equal(search.Moves.Select(m => m.MeanScore).OrderDescending(), search.Moves.Select(m => m.MeanScore));
        Assert.True(search.Moves[0].MeanScore > search.Moves[^1].MeanScore);
    }

    [Fact]
    public void Expansion_does_not_always_start_from_the_first_legal_move()
    {
        // Przy budzecie 1 iteracji sprawdzony jest tylko jeden ruch — losowy, a nie zawsze pierwszy z listy.
        var firstExpanded = Enumerable.Range(0, 30)
            .Select(seed => new MctsBot<NimState, int>((ulong)seed, new MctsOptions { Iterations = 1 }).ChooseMove(Nim, new NimState(10, 2, 0, null)))
            .ToHashSet();

        Assert.Equal([1, 2, 3], firstExpanded.Order());
    }

    [Fact]
    public void Same_seed_gives_the_same_search()
    {
        var options = new MctsOptions { Iterations = 300 };
        var state = new NimState(12, 2, 0, null);

        var a = new MctsBot<NimState, int>(5, options).Search(Nim, state);
        var b = new MctsBot<NimState, int>(5, options).Search(Nim, state);

        Assert.Equal(a.Moves.ToArray(), b.Moves.ToArray());
    }

    [Fact]
    public void Without_exploration_the_search_can_lock_onto_a_losing_move()
    {
        // Przy c = 0 liczy sie tylko srednia: ruch, ktory raz wygral, dostaje wszystkie kolejne
        // iteracje, a pozostale moga nigdy nie dostac drugiej szansy. Ze stosu 10 wygrywa tylko 2.
        ulong[] seeds = [6, 7, 8];
        var state = new NimState(10, 2, 0, null);

        var greedy = seeds.Select(seed =>
            new MctsBot<NimState, int>(seed, new MctsOptions { Iterations = 1_000, Exploration = 0 }).Search(Nim, state)).ToArray();
        var balanced = seeds.Select(seed =>
            new MctsBot<NimState, int>(seed, new MctsOptions { Iterations = 1_000 }).Search(Nim, state)).ToArray();

        Assert.Contains(greedy, s => s.BestMove != 2);
        Assert.All(greedy, s => Assert.True(s.Moves[0].Visits > 990));
        Assert.All(balanced, s => Assert.Equal(2, s.BestMove));
        Assert.All(balanced, s => Assert.True(s.Moves.Min(m => m.Visits) > 40));
    }
}
