using BalanceLab.Engine;
using BalanceLab.Testing;
using FsCheck.Xunit;

namespace BalanceLab.Bots.Tests;

public sealed class RandomBotTests
{
    private static readonly NimGame Nim = new();

    [Property]
    public bool Always_picks_a_legal_move(ulong seed, byte pile)
    {
        var state = new NimState(1 + (pile % 20), 2, 0, null);
        var bot = new RandomBot<NimState, int>(seed);

        return Enumerable.Range(0, 20).All(_ => Nim.LegalMoves(state).Contains(bot.ChooseMove(Nim, state)));
    }

    [Property]
    public bool Same_seed_plays_the_same_moves(ulong seed)
    {
        var state = new NimState(10, 2, 0, null);
        var a = new RandomBot<NimState, int>(seed);
        var b = new RandomBot<NimState, int>(seed);

        return Enumerable.Range(0, 30).All(_ => a.ChooseMove(Nim, state) == b.ChooseMove(Nim, state));
    }

    [Fact]
    public void Picks_every_legal_move_equally_often()
    {
        const int Rounds = 30_000;
        var state = new NimState(10, 2, 0, null);
        var bot = new RandomBot<NimState, int>(7);
        var counts = new int[4];
        for (var i = 0; i < Rounds; i++)
        {
            counts[bot.ChooseMove(Nim, state)]++;
        }

        // Chi-kwadrat dla 2 stopni swobody; 13.8 to wartosc krytyczna dla p = 0.001.
        const double Expected = Rounds / 3.0;
        var chiSquare = counts.Skip(1).Sum(c => (c - Expected) * (c - Expected) / Expected);
        Assert.True(chiSquare < 13.8, $"chi^2 = {chiSquare}");
    }

    [Fact]
    public void Single_legal_move_is_always_chosen()
    {
        var bot = new RandomBot<NimState, int>(3);

        Assert.All(Enumerable.Range(0, 10), _ => Assert.Equal(1, bot.ChooseMove(Nim, new NimState(1, 2, 0, null))));
    }

    [Fact]
    public void Refuses_to_move_when_the_game_is_over()
    {
        var bot = new RandomBot<NimState, int>(3);

        var ex = Assert.Throws<InvalidOperationException>(() => bot.ChooseMove(Nim, new NimState(0, 2, 0, 1)));

        Assert.Contains("Brak legalnych ruchow", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_null_game()
    {
        var bot = new RandomBot<NimState, int>(3);

        Assert.Throws<ArgumentNullException>(() => bot.ChooseMove(null!, new NimState(5, 2, 0, null)));
    }

    [Fact]
    public void Plays_full_matches_against_itself()
    {
        var result = Match.Play(Nim, new GameSetup(4, 9),
            [.. Enumerable.Range(0, 4).Select(i => new RandomBot<NimState, int>((ulong)i))]);

        Assert.True(result.Completed);
        Assert.Equal(1.0, result.Scores.Sum());
    }
}
