using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Bots;

/// <summary>Wspolne kawalki MCTS i ISMCTS: losowa symulacja do konca partii i ziarna z generatora.</summary>
internal static class Rollout
{
    /// <summary>
    /// Losowe ruchy az do konca partii. Pusty wynik, gdy partia przekroczy limit ruchow —
    /// wolajacy liczy ja wtedy jak remis.
    /// </summary>
    public static ImmutableArray<double> Play<TState, TMove>(IGame<TState, TMove> game, TState state, ref Pcg32 rng, int maxMoves)
        where TState : notnull
        where TMove : notnull
    {
        for (var steps = 0; !game.IsTerminal(state); steps++)
        {
            if (steps == maxMoves)
            {
                return [];
            }

            var moves = game.LegalMoves(state);
            (var index, rng) = rng.NextInt(moves.Count);
            state = game.Apply(state, moves[index]);
        }

        return game.Scores(state);
    }

    /// <summary>Wynik gracza z symulacji; przerwana symulacja to remis (1 / liczba graczy).</summary>
    public static double ScoreOf(ImmutableArray<double> scores, int player, int playerCount) =>
        scores.IsEmpty ? 1.0 / playerCount : scores[player];

    /// <summary>64-bitowe ziarno z dwoch losowan 32-bitowych.</summary>
    public static ulong NextSeed(ref Pcg32 rng)
    {
        (var high, rng) = rng.NextUInt();
        (var low, rng) = rng.NextUInt();
        return ((ulong)high << 32) | low;
    }
}
