using BalanceLab.Testing;
using FsCheck;
using FsCheck.Xunit;

namespace BalanceLab.Engine.Tests;

public sealed class MatchTests
{
    private static readonly NimGame Nim = new();

    [Property]
    public bool Every_nim_match_ends_with_exactly_one_winner(ulong seed, PositiveInt strategySeed)
    {
        var players = Players(3, (ulong)strategySeed.Get);

        var result = Match.Play(Nim, new GameSetup(3, seed), players);

        return result.Completed && result.Scores.Sum() == 1.0 && result.Moves.Sum() == Nim.Setup(new GameSetup(3, seed)).Pile;
    }

    [Fact]
    public void Players_move_in_turn_and_the_last_taker_wins()
    {
        // Stos 10 + NextInt(10) dla ziarna 0; kazdy bierze po 1, wiec ostatni ruch robi gracz (stos - 1) % 2.
        var setup = new GameSetup(2, 0);
        var pile = Nim.Setup(setup).Pile;
        var first = new FixedPlayer<NimState, int>(1);
        var second = new FixedPlayer<NimState, int>(1);

        var result = Match.Play(Nim, setup, [first, second]);

        Assert.Equal(Enumerable.Repeat(1, pile), result.Moves);
        Assert.Equal((pile + 1) / 2, first.Calls);
        Assert.Equal(pile / 2, second.Calls);
        Assert.Equal((pile - 1) % 2, result.FinalState.Winner);
        Assert.True(result.Completed);
    }

    [Property]
    public bool Same_seeds_give_the_same_match(ulong seed, ulong strategySeed)
    {
        var a = Match.Play(Nim, new GameSetup(2, seed), Players(2, strategySeed));
        var b = Match.Play(Nim, new GameSetup(2, seed), Players(2, strategySeed));

        return a.Moves.SequenceEqual(b.Moves) && Nim.Hash(a.FinalState) == Nim.Hash(b.FinalState);
    }

    [Fact]
    public void Illegal_move_from_a_player_stops_the_match()
    {
        var cheater = new FixedPlayer<NimState, int>(4);
        var pile = Nim.Setup(new GameSetup(2, 1)).Pile;

        var ex = Assert.Throws<IllegalMoveException>(() => Match.Play(Nim, new GameSetup(2, 1), [cheater, cheater]));

        Assert.Equal($"Nie mozna wziac 4 ze stosu {pile}.", ex.Message);
        Assert.Equal(1, cheater.Calls);
    }

    [Fact]
    public void Match_stops_at_the_move_limit_without_scores()
    {
        var setup = new GameSetup(2, 5);

        var result = Match.Play(Nim, setup, Players(2, 1), maxMoves: 3);

        Assert.False(result.Completed);
        Assert.Equal(3, result.Moves.Length);
        Assert.Empty(result.Scores);
        Assert.Equal(Nim.Setup(setup).Pile - result.Moves.Sum(), result.FinalState.Pile);
    }

    [Fact]
    public void Limit_of_exactly_the_match_length_still_completes_it()
    {
        var setup = new GameSetup(2, 0);
        var pile = Nim.Setup(setup).Pile;
        var one = new FixedPlayer<NimState, int>(1);

        var result = Match.Play(Nim, setup, [one, one], maxMoves: pile);

        Assert.True(result.Completed);
        Assert.Equal(pile, result.Moves.Length);
    }

    [Fact]
    public void Wrong_number_of_players_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() => Match.Play(Nim, new GameSetup(3, 1), Players(2, 1)));

        Assert.Equal("players", ex.ParamName);
        Assert.StartsWith("Partia na 3 graczy, a podano 2.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Negative_move_limit_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Match.Play(Nim, new GameSetup(2, 1), Players(2, 1), maxMoves: -1));
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        Assert.Equal("game", Assert.Throws<ArgumentNullException>(
            () => Match.Play<NimState, int>(null!, new GameSetup(2, 1), Players(2, 1))).ParamName);
        Assert.Equal("players", Assert.Throws<ArgumentNullException>(
            () => Match.Play(Nim, new GameSetup(2, 1), null!)).ParamName);
    }

    /// <summary>Gracze biora "losowo" 1-3, deterministycznie z ziarna — bez zaleznosci od projektu botow.</summary>
    private static IPlayer<NimState, int>[] Players(int count, ulong seed) =>
        [.. Enumerable.Range(0, count).Select(i => new SeededPlayer(seed + (ulong)i))];

    private sealed class SeededPlayer(ulong seed) : IPlayer<NimState, int>
    {
        private Pcg32 _rng = Pcg32.Create(seed);

        public int ChooseMove(IGame<NimState, int> game, NimState state)
        {
            var moves = game.LegalMoves(state);
            (var i, _rng) = _rng.NextInt(moves.Count);
            return moves[i];
        }
    }
}
