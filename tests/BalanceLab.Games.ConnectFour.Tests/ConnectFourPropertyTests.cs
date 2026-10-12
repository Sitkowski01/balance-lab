using BalanceLab.Bots;
using BalanceLab.Engine;
using FsCheck.Xunit;

namespace BalanceLab.Games.ConnectFour.Tests;

/// <summary>Tysiace losowych partii: bitboard kontra prosty, oczywiscie poprawny sedzia na siatce.</summary>
public sealed class ConnectFourPropertyTests
{
    private static readonly ConnectFourGame Game = new();

    [Property(MaxTest = 300)]
    public bool Bitboard_agrees_with_a_naive_referee_after_every_move(ulong seed)
    {
        var bot = new RandomBot<ConnectFourState, ConnectFourMove>(seed);
        var state = Game.Setup(new GameSetup(2, 0));
        while (!Game.IsTerminal(state))
        {
            var mover = Game.CurrentPlayer(state);
            state = Game.Apply(state, bot.ChooseMove(Game, state));

            if (state.Winner != (NaiveHasFour(state, mover) ? mover : null) || NaiveHasFour(state, 1 - mover))
            {
                return false;
            }

            var open = Enumerable.Range(0, ConnectFourState.Columns).Where(c => state.Owner(c, ConnectFourState.Rows - 1) is null);
            var expectedMoves = Game.IsTerminal(state) ? [] : open.Select(c => new ConnectFourMove(c));
            if (!Game.LegalMoves(state).SequenceEqual(expectedMoves))
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 300)]
    public bool Every_match_ends_within_42_moves_with_scores_summing_to_one(ulong seedA, ulong seedB)
    {
        var result = Match.Play(Game, new GameSetup(2, 0), Bots(seedA, seedB));

        return result.Completed && result.Moves.Length <= 42 && result.Scores.Sum() == 1.0
            && result.FinalState.MovesPlayed == result.Moves.Length;
    }

    [Property]
    public bool Same_seeds_replay_the_same_match(ulong seedA, ulong seedB)
    {
        var first = Match.Play(Game, new GameSetup(2, 0), Bots(seedA, seedB));
        var replay = Match.Play(Game, new GameSetup(2, 0), Bots(seedA, seedB));

        return first.Moves.SequenceEqual(replay.Moves) && Game.Hash(first.FinalState) == Game.Hash(replay.FinalState);
    }

    [Property]
    public bool Replaying_the_recorded_moves_gives_the_same_final_hash(ulong seedA, ulong seedB)
    {
        // Tak bedzie dzialac powtorka z dziennika: same ruchy, bez botow.
        var result = Match.Play(Game, new GameSetup(2, 0), Bots(seedA, seedB));

        var replayed = result.Moves.Aggregate(Game.Setup(new GameSetup(2, 0)), Game.Apply);

        return Game.Hash(replayed) == Game.Hash(result.FinalState) && replayed == result.FinalState;
    }

    [Fact]
    public void Random_bots_win_as_first_player_more_often()
    {
        // Znana wlasnosc Czworek: pierwszy gracz ma przewage. To zapowiedz raportu balansu.
        var firstWins = Enumerable.Range(0, 2_000)
            .Select(i => Match.Play(Game, new GameSetup(2, 0), Bots((ulong)i, (ulong)i + 10_000)))
            .Count(r => r.FinalState.Winner == 0);

        Assert.InRange(firstWins, 1_050, 1_400);
    }

    private static IPlayer<ConnectFourState, ConnectFourMove>[] Bots(ulong a, ulong b) =>
        [new RandomBot<ConnectFourState, ConnectFourMove>(a), new RandomBot<ConnectFourState, ConnectFourMove>(b)];

    private static bool NaiveHasFour(ConnectFourState state, int player)
    {
        (int Dc, int Dr)[] directions = [(1, 0), (0, 1), (1, 1), (1, -1)];
        for (var c = 0; c < ConnectFourState.Columns; c++)
        {
            for (var r = 0; r < ConnectFourState.Rows; r++)
            {
                foreach (var (dc, dr) in directions)
                {
                    if (Enumerable.Range(0, 4).All(k => Owns(state, player, c + (dc * k), r + (dr * k))))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool Owns(ConnectFourState state, int player, int column, int row) =>
        column is >= 0 and < ConnectFourState.Columns && row is >= 0 and < ConnectFourState.Rows
        && state.Owner(column, row) == player;
}
