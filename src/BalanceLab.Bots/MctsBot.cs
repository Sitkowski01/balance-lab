using System.Collections.Immutable;
using System.Diagnostics;
using BalanceLab.Engine;

namespace BalanceLab.Bots;

/// <summary>
/// Monte Carlo Tree Search z wyborem UCT. Nie potrzebuje zadnej wiedzy o grze poza zasadami:
/// rozgrywa losowe partie od kandydujacych ruchow i coraz czesciej sprawdza te, ktore wygrywaja.
/// </summary>
/// <remarks>
/// Dziala dla dowolnej liczby graczy: kazdy wezel zbiera wynik gracza, ktory wykonal prowadzacy
/// do niego ruch, wiec kazdy gracz w drzewie wybiera to, co najlepsze dla siebie.
/// Jak kazdy <see cref="IPlayer{TState, TMove}"/> nalezy do jednej partii.
/// </remarks>
public sealed class MctsBot<TState, TMove> : IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    private readonly MctsOptions _options;
    private Pcg32 _rng;

    public MctsBot(ulong seed, MctsOptions? options = null)
    {
        _options = options ?? new MctsOptions();
        _options.Validate();
        _rng = Pcg32.Create(seed);
    }

    public TMove ChooseMove(IGame<TState, TMove> game, TState state)
    {
        ArgumentNullException.ThrowIfNull(game);

        // Ruch wymuszony: nie ma czego liczyc, a w symulacji tysiecy partii to sie sumuje.
        var moves = game.LegalMoves(state);
        return moves.Count == 1 ? moves[0] : Search(game, state).BestMove;
    }

    public MctsSearch<TMove> Search(IGame<TState, TMove> game, TState state)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.IsTerminal(state))
        {
            throw new InvalidOperationException("Partia sie skonczyla - nie ma czego szukac.");
        }

        var root = CreateNode(game, state, parent: null, move: default!, playerJustMoved: -1);
        var clock = _options.TimeLimit is null ? null : Stopwatch.StartNew();
        var iterations = 0;
        do
        {
            Iterate(game, root);
            iterations++;
        }
        while (iterations < _options.IterationLimit && (clock is null || clock.Elapsed < _options.TimeLimit));

        // Najczesciej odwiedzany ruch ("robust child") jest pewniejszy niz ten z najlepsza srednia,
        // ktora przy kilku odwiedzinach moze byc szczesliwym przypadkiem.
        ImmutableArray<MctsMoveStats<TMove>> moves =
        [
            .. root.Children!
                .Select(c => new MctsMoveStats<TMove>(c.Move, c.Visits, c.Reward / c.Visits))
                .OrderByDescending(s => s.Visits)
                .ThenByDescending(s => s.MeanScore),
        ];
        return new MctsSearch<TMove>(moves[0].Move, iterations, moves);
    }

    private void Iterate(IGame<TState, TMove> game, Node root)
    {
        // 1. Wybor: schodzimy po najlepszym UCB, dopoki wezel ma sprawdzone wszystkie ruchy.
        var node = root;
        while (node.Expanded == node.Moves.Count && node.Children is not null)
        {
            node = SelectChild(node);
        }

        // 2. Rozwiniecie: dokladamy jeden nowy ruch (w stanie koncowym nie ma czego dokladac).
        if (node.Expanded < node.Moves.Count)
        {
            var move = node.Moves[(node.Offset + node.Expanded) % node.Moves.Count];
            node.Expanded++;
            var child = CreateNode(game, game.Apply(node.State, move), node, move, game.CurrentPlayer(node.State));
            (node.Children ??= new List<Node>(node.Moves.Count)).Add(child);
            node = child;
        }

        // 3. Symulacja i 4. propagacja wyniku w gore drzewa.
        var scores = Playout(game, node.State);
        var draw = 1.0 / game.PlayerCount(node.State);
        for (var n = node; n.Parent is not null; n = n.Parent)
        {
            n.Visits++;
            n.Reward += scores.IsEmpty ? draw : scores[n.PlayerJustMoved];
        }

        root.Visits++;
    }

    private Node SelectChild(Node node)
    {
        var children = node.Children!;
        var logVisits = DeterministicMath.Ln(node.Visits);
        var best = children[0];
        var bestValue = double.NegativeInfinity;
        foreach (var child in children)
        {
            // UCB1: srednia (eksploatacja) + premia dla rzadko sprawdzanych ruchow (eksploracja).
            var value = (child.Reward / child.Visits) + (_options.Exploration * Math.Sqrt(logVisits / child.Visits));
            if (value > bestValue)
            {
                best = child;
                bestValue = value;
            }
        }

        return best;
    }

    private ImmutableArray<double> Playout(IGame<TState, TMove> game, TState state)
    {
        for (var steps = 0; !game.IsTerminal(state); steps++)
        {
            if (steps == _options.MaxPlayoutMoves)
            {
                return [];
            }

            var moves = game.LegalMoves(state);
            (var index, _rng) = _rng.NextInt(moves.Count);
            state = game.Apply(state, moves[index]);
        }

        return game.Scores(state);
    }

    private Node CreateNode(IGame<TState, TMove> game, TState state, Node? parent, TMove move, int playerJustMoved)
    {
        var moves = game.LegalMoves(state);
        // Losowy punkt startu zamiast zawsze od pierwszego ruchu: przy malym budzecie
        // kolejnosc rozwijania nie faworyzuje ruchow z poczatku listy.
        var offset = 0;
        if (moves.Count > 1)
        {
            (offset, _rng) = _rng.NextInt(moves.Count);
        }

        return new Node(state, parent, move, playerJustMoved, moves, offset);
    }

    private sealed class Node(TState state, Node? parent, TMove move, int playerJustMoved, IReadOnlyList<TMove> moves, int offset)
    {
        public TState State { get; } = state;

        public Node? Parent { get; } = parent;

        public TMove Move { get; } = move;

        /// <summary>Gracz, ktory wykonal <see cref="Move"/>; jego wynik zbiera <see cref="Reward"/>.</summary>
        public int PlayerJustMoved { get; } = playerJustMoved;

        public IReadOnlyList<TMove> Moves { get; } = moves;

        public int Offset { get; } = offset;

        public int Expanded { get; set; }

        /// <summary>Tworzone przy pierwszym rozwinieciu — wiekszosc lisci nigdy nie dostaje dzieci.</summary>
        public List<Node>? Children { get; set; }

        public int Visits { get; set; }

        public double Reward { get; set; }
    }
}
