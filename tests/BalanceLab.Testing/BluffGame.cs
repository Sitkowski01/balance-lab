using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Testing;

public enum BluffMove
{
    Bet,
    Fold,
    Call,
}

/// <param name="Card0">Karta gracza 0 (z talii 1-3); kazdy widzi tylko swoja.</param>
/// <param name="Card1">Karta gracza 1.</param>
/// <param name="Stage">0 — gracz 0 zaklada sie albo pasuje, 1 — gracz 1 sprawdza albo pasuje, 2 — koniec.</param>
public readonly record struct BluffState(int Card0, int Card1, int Stage, int? Winner)
{
    public int CardOf(int player) => player == 0 ? Card0 : Card1;
}

/// <summary>
/// Najmniejsza gra z ukryta informacja (uproszczony poker Kuhna): kazdy dostaje jedna z kart 1-3.
/// Gracz 0 pasuje (przegrywa) albo sie zaklada; wtedy gracz 1 pasuje (przegrywa) albo sprawdza
/// i wygrywa wyzsza karta. Bot, ktory podejrzalby karte rywala, gralby tu wyraznie inaczej.
/// </summary>
public sealed class BluffGame : IGame<BluffState, BluffMove>
{
    private static readonly IReadOnlyList<BluffMove> FirstMoves = [BluffMove.Bet, BluffMove.Fold];
    private static readonly IReadOnlyList<BluffMove> SecondMoves = [BluffMove.Call, BluffMove.Fold];
    private static readonly int[] Deck = [1, 2, 3];

    public string Name => "Bluff";

    public int MinPlayers => 2;

    public int MaxPlayers => 2;

    public BluffState Setup(GameSetup setup)
    {
        setup.EnsurePlayers(MinPlayers, MaxPlayers);
        var (deck, _) = Pcg32.Create(setup.Seed).Shuffle(Deck);
        return new BluffState(deck[0], deck[1], 0, null);
    }

    public int PlayerCount(BluffState state) => 2;

    public int CurrentPlayer(BluffState state) => state.Stage == 1 ? 1 : 0;

    public IReadOnlyList<BluffMove> LegalMoves(BluffState state) => state.Stage switch
    {
        0 => FirstMoves,
        1 => SecondMoves,
        _ => [],
    };

    public BluffState Apply(BluffState state, BluffMove move)
    {
        if (!LegalMoves(state).Contains(move))
        {
            throw new IllegalMoveException($"Ruch {move} niedozwolony na etapie {state.Stage}.");
        }

        return (state.Stage, move) switch
        {
            (0, BluffMove.Bet) => state with { Stage = 1 },
            (0, _) => state with { Stage = 2, Winner = 1 },
            (_, BluffMove.Fold) => state with { Stage = 2, Winner = 0 },
            _ => state with { Stage = 2, Winner = state.Card0 > state.Card1 ? 0 : 1 },
        };
    }

    public bool IsTerminal(BluffState state) => state.Stage == 2;

    public ImmutableArray<double> Scores(BluffState state) => state.Winner switch
    {
        0 => [1.0, 0.0],
        1 => [0.0, 1.0],
        _ => throw new InvalidOperationException("Partia jeszcze trwa."),
    };

    public ulong Hash(BluffState state) =>
        StableHash.Empty.Add((ulong)state.Card0).Add((ulong)state.Card1).Add((ulong)state.Stage)
            .Add((ulong)(state.Winner + 1 ?? 0)).Value;

    /// <summary>Karta rywala losowana z dwoch, ktorych obserwator nie ma. Po koncu karty sa odkryte.</summary>
    public BluffState Determinize(BluffState state, int observer, ulong seed)
    {
        if (IsTerminal(state))
        {
            return state;
        }

        var own = state.CardOf(observer);
        int[] unseen = [.. Deck.Where(c => c != own)];
        var (index, _) = Pcg32.Create(seed).NextInt(unseen.Length);
        return observer == 0 ? state with { Card1 = unseen[index] } : state with { Card0 = unseen[index] };
    }
}

/// <summary>Zapamietuje kazdy widok, jaki dostal od partii; ruch wybiera pierwszy legalny.</summary>
public sealed class SpyPlayer<TState, TMove> : IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    public List<TState> Views { get; } = [];

    public TMove ChooseMove(IGame<TState, TMove> game, TState state)
    {
        Views.Add(state);
        return game.LegalMoves(state)[0];
    }
}
