using BalanceLab.Testing;

namespace BalanceLab.Engine.Tests;

/// <summary>Partia pokazuje graczowi tylko jego wiedze — prawdziwy stan zostaje u sedziego.</summary>
public sealed class MatchViewTests
{
    private static readonly BluffGame Bluff = new();

    [Fact]
    public void Player_sees_own_card_but_only_a_guess_of_the_opponents()
    {
        var guessedRight = 0;
        for (var seed = 0UL; seed < 300; seed++)
        {
            var dealt = Bluff.Setup(new GameSetup(2, seed));
            var first = new SpyPlayer<BluffState, BluffMove>(); // zawsze sie zaklada
            var second = new SpyPlayer<BluffState, BluffMove>(); // zawsze sprawdza

            var result = Match.Play(Bluff, new GameSetup(2, seed), [first, second]);

            Assert.Equal(dealt.Card0, Assert.Single(first.Views).Card0);
            Assert.Equal(dealt.Card1, Assert.Single(second.Views).Card1);
            Assert.NotEqual(first.Views[0].Card0, first.Views[0].Card1);
            Assert.Equal(dealt.Card0 > dealt.Card1 ? 0 : 1, result.FinalState.Winner);
            guessedRight += first.Views[0].Card1 == dealt.Card1 ? 1 : 0;
        }

        // Zgadywanie jednej z dwoch kart: okolo polowy, a nie zawsze (podgladanie) ani nigdy.
        Assert.InRange(guessedRight, 120, 180);
    }

    [Fact]
    public void View_follows_the_games_determinization_with_the_view_seed()
    {
        var setup = new GameSetup(2, 42);
        var spy = new SpyPlayer<BluffState, BluffMove>();

        Match.Play(Bluff, setup, [spy, new SpyPlayer<BluffState, BluffMove>()]);

        Assert.Equal(Bluff.Determinize(Bluff.Setup(setup), 0, Match.ViewSeed(42, 0)), spy.Views[0]);
    }

    [Fact]
    public void View_seed_differs_per_move_and_from_the_setup_seed()
    {
        ulong[] seeds = [Match.ViewSeed(7, 0), Match.ViewSeed(7, 1), Match.ViewSeed(8, 0), 7];

        Assert.Equal(seeds.Length, seeds.Distinct().Count());
        Assert.Equal(Match.ViewSeed(7, 1), Match.ViewSeed(7, 1));
    }

    [Fact]
    public void Full_information_game_shows_the_real_state()
    {
        var setup = new GameSetup(2, 3);
        var spy = new SpyPlayer<NimState, int>();

        Match.Play(new NimGame(), setup, [spy, spy]);

        Assert.Equal(new NimGame().Setup(setup), spy.Views[0]);
        Assert.All(spy.Views.Zip(spy.Views.Skip(1)), pair => Assert.Equal(pair.First.Pile - 1, pair.Second.Pile));
    }
}
