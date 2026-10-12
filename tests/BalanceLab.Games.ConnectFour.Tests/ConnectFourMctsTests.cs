using BalanceLab.Bots;
using BalanceLab.Engine;

namespace BalanceLab.Games.ConnectFour.Tests;

/// <summary>Testy sily MCTS na Czworkach; stale ziarna, wiec wynik jest powtarzalny w CI.</summary>
/// <remarks>Rozgrywaja setki partii, wiec Stryker je pomija (test-case-filter) — inaczej kazdy mutant gry czekalby na nie.</remarks>
[Trait("Category", "Strength")]
public sealed class ConnectFourMctsTests
{
    private static readonly ConnectFourGame Game = new();

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void Takes_a_diagonal_win_in_one(ulong seed)
    {
        var state = Board.Parse(".......", ".......", ".......", "..XO...", ".XOX...", "XOOX..O");

        Assert.Equal(new ConnectFourMove(3), Mcts(seed, 1_000).ChooseMove(Game, state));
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void Blocks_the_opponents_win(ulong seed)
    {
        // O grozi czworka w kolumnie 5; X nie ma wlasnej wygranej, wiec musi blokowac.
        var state = Board.Parse(".......", ".......", ".......", ".....O.", ".....O.", "X.X..OX");

        Assert.Equal(new ConnectFourMove(5), Mcts(seed, 2_000).ChooseMove(Game, state));
    }

    [Fact]
    public void Beats_the_random_bot_in_at_least_95_percent_of_games()
    {
        const int Games = 200;
        var wins = 0;
        for (var i = 0; i < Games; i++)
        {
            // Na zmiane zaczyna MCTS i bot losowy, zeby przewaga pierwszego gracza sie znosila.
            var mctsSeat = i % 2;
            IPlayer<ConnectFourState, ConnectFourMove> mcts = Mcts((ulong)i, 300);
            IPlayer<ConnectFourState, ConnectFourMove> random = new RandomBot<ConnectFourState, ConnectFourMove>((ulong)i + 1_000);
            var result = Match.Play(Game, new GameSetup(2, 0), mctsSeat == 0 ? [mcts, random] : [random, mcts]);

            wins += result.FinalState.Winner == mctsSeat ? 1 : 0;
        }

        Assert.True(wins >= Games * 95 / 100, $"MCTS wygral {wins} z {Games}");
    }

    [Fact]
    public void More_iterations_beat_fewer()
    {
        const int Games = 60;
        var strongWins = 0;
        var weakWins = 0;
        for (var i = 0; i < Games; i++)
        {
            var strongSeat = i % 2;
            var strong = Mcts((ulong)i, 1_000);
            var weak = Mcts((ulong)i + 500, 50);
            var winner = Match.Play(Game, new GameSetup(2, 0), strongSeat == 0 ? [strong, weak] : [weak, strong]).FinalState.Winner;

            strongWins += winner == strongSeat ? 1 : 0;
            weakWins += winner == 1 - strongSeat ? 1 : 0;
        }

        Assert.True(strongWins > 2 * weakWins, $"1000 iteracji: {strongWins}, 50 iteracji: {weakWins}");
    }

    private static MctsBot<ConnectFourState, ConnectFourMove> Mcts(ulong seed, int iterations) =>
        new(seed, new MctsOptions { Iterations = iterations });
}
