using System.Collections.Immutable;

namespace BalanceLab.Engine;

/// <summary>Przebieg jednej partii: ruchy po kolei, stan koncowy i wyniki graczy.</summary>
public sealed record MatchResult<TState, TMove>(
    ImmutableArray<TMove> Moves,
    TState FinalState,
    bool Completed,
    ImmutableArray<double> Scores)
    where TState : notnull
    where TMove : notnull;

/// <summary>Rozgrywa partie od poczatku do konca.</summary>
public static class Match
{
    /// <param name="maxMoves">
    /// Bezpiecznik na gry, ktore moga sie zapetlic; taka partia konczy sie z
    /// <see cref="MatchResult{TState, TMove}.Completed"/> = false i bez wynikow.
    /// </param>
    public static MatchResult<TState, TMove> Play<TState, TMove>(
        IGame<TState, TMove> game,
        GameSetup setup,
        IReadOnlyList<IPlayer<TState, TMove>> players,
        int maxMoves = 10_000)
        where TState : notnull
        where TMove : notnull
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMoves);
        if (players.Count != setup.PlayerCount)
        {
            throw new ArgumentException($"Partia na {setup.PlayerCount} graczy, a podano {players.Count}.", nameof(players));
        }

        var state = game.Setup(setup);
        var moves = ImmutableArray.CreateBuilder<TMove>();
        while (!game.IsTerminal(state))
        {
            if (moves.Count == maxMoves)
            {
                return new MatchResult<TState, TMove>(moves.ToImmutable(), state, false, []);
            }

            var move = players[game.CurrentPlayer(state)].ChooseMove(game, state);
            // Gracz moze byc bledny albo nieuczciwy: Apply sprawdza legalnosc i rzuca IllegalMoveException.
            state = game.Apply(state, move);
            moves.Add(move);
        }

        return new MatchResult<TState, TMove>(moves.ToImmutable(), state, true, game.Scores(state));
    }
}
