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

            // Gracz dostaje tylko swoja wiedze: ukryte karty sa w widoku wylosowane od nowa,
            // wiec bot nie podejrzy talii ani cudzej reki, nawet gdyby chcial.
            var current = game.CurrentPlayer(state);
            var view = game.Determinize(state, current, ViewSeed(setup.Seed, moves.Count));
            var move = players[current].ChooseMove(game, view);
            // Gracz moze byc bledny albo nieuczciwy: Apply sprawdza legalnosc i rzuca IllegalMoveException.
            state = game.Apply(state, move);
            moves.Add(move);
        }

        return new MatchResult<TState, TMove>(moves.ToImmutable(), state, true, game.Scores(state));
    }

    /// <summary>
    /// Ziarno widoku przed ruchem nr <paramref name="move"/>. Dopisek celu odroznia je od ziarna
    /// rozdania — inaczej widok moglby wylosowac talie dokladnie tak, jak lezy naprawde.
    /// </summary>
    public static ulong ViewSeed(ulong setupSeed, int move) =>
        StableHash.Empty.Add(setupSeed).Add((ulong)move).Add(ViewPurpose).Value;

    private const ulong ViewPurpose = 0x5649_4557; // "VIEW"
}
