using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Testing;

public readonly record struct NimState(int Pile, int PlayerCount, int ToMove, int? Winner);

/// <summary>
/// Najmniejsza sensowna gra do testow silnika i botow: gracze po kolei zabieraja 1-3
/// zetony ze stosu, wygrywa ten, kto wezmie ostatni. Wielkosc stosu (10-19) zalezy od ziarna.
/// </summary>
public sealed class NimGame : IGame<NimState, int>
{
    public string Name => "Nim";

    public int MinPlayers => 2;

    public int MaxPlayers => 4;

    public NimState Setup(GameSetup setup)
    {
        setup.EnsurePlayers(MinPlayers, MaxPlayers);
        var (extra, _) = Pcg32.Create(setup.Seed).NextInt(10);
        return new NimState(10 + extra, setup.PlayerCount, 0, null);
    }

    public int PlayerCount(NimState state) => state.PlayerCount;

    public int CurrentPlayer(NimState state) => state.ToMove;

    public IReadOnlyList<int> LegalMoves(NimState state) => [.. Enumerable.Range(1, Math.Min(3, state.Pile))];

    public NimState Apply(NimState state, int move)
    {
        if (move < 1 || move > Math.Min(3, state.Pile))
        {
            throw new IllegalMoveException($"Nie mozna wziac {move} ze stosu {state.Pile}.");
        }

        var pile = state.Pile - move;
        return new NimState(pile, state.PlayerCount, (state.ToMove + 1) % state.PlayerCount,
            pile == 0 ? state.ToMove : null);
    }

    public bool IsTerminal(NimState state) => state.Pile == 0;

    public ImmutableArray<double> Scores(NimState state) => !IsTerminal(state)
        ? throw new InvalidOperationException("Partia jeszcze trwa.")
        : [.. Enumerable.Range(0, state.PlayerCount).Select(p => p == state.Winner ? 1.0 : 0.0)];

    public ulong Hash(NimState state) =>
        StableHash.Empty.Add((ulong)state.Pile).Add((ulong)state.PlayerCount).Add((ulong)state.ToMove)
            .Add((ulong)(state.Winner + 1 ?? 0)).Value;
}

/// <summary>Gracz, ktory zawsze probuje tego samego ruchu, legalnego czy nie.</summary>
public sealed class FixedPlayer<TState, TMove>(TMove move) : IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    public int Calls { get; private set; }

    public TMove ChooseMove(IGame<TState, TMove> game, TState state)
    {
        Calls++;
        return move;
    }
}
