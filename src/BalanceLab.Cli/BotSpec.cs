using System.Globalization;
using BalanceLab.Bots;
using BalanceLab.Engine;
using BalanceLab.Simulation;

namespace BalanceLab.Cli;

/// <summary>Bot z linii polecen: <c>random</c>, <c>greedy</c>, <c>mcts[:ITERACJE]</c> albo <c>ismcts[:ITERACJE]</c>.</summary>
internal static class BotSpec
{
    public const string Help = "random | greedy | mcts | mcts:ITERACJE | ismcts | ismcts:ITERACJE (np. mcts:500)";

    /// <param name="evaluator">Ocena pozycji gry; bez niej bot <c>greedy</c> jest niedostepny.</param>
    public static Contestant<TState, TMove> Parse<TState, TMove>(string spec, string name, IEvaluator<TState>? evaluator)
        where TState : notnull
        where TMove : notnull
    {
        var parts = spec.Split(':', 2);
        int? iterations = null;
        if (parts.Length == 2)
        {
            iterations = int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0
                ? n
                : throw Unknown(spec);
        }

        var options = new MctsOptions { Iterations = iterations };
        return (parts[0].ToLowerInvariant(), parts.Length) switch
        {
            ("random", 1) => new(name, seed => new RandomBot<TState, TMove>(seed)),
            ("greedy", 1) when evaluator is not null => new(name, seed => new GreedyBot<TState, TMove>(evaluator, seed)),
            ("greedy", 1) => throw new FormatException("Bot 'greedy' potrzebuje oceny pozycji, a ta gra jej nie ma."),
            ("mcts", _) => new(name, seed => new MctsBot<TState, TMove>(seed, options)),
            ("ismcts", _) => new(name, seed => new IsmctsBot<TState, TMove>(seed, options)),
            _ => throw Unknown(spec),
        };
    }

    /// <summary>Nazwy do raportu: powtorzony bot dostaje numer, np. "random", "random#2".</summary>
    public static IReadOnlyList<string> UniqueNames(IReadOnlyList<string> specs) =>
        [.. specs.Select((spec, i) =>
        {
            var earlier = specs.Take(i).Count(s => s == spec);
            return earlier == 0 ? spec : $"{spec}#{earlier + 1}";
        })];

    private static FormatException Unknown(string spec) => new($"Nieznany bot '{spec}'. Dostepne: {Help}.");
}
