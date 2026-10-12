using System.Globalization;
using BalanceLab.Bots;
using BalanceLab.Engine;
using BalanceLab.Simulation;

namespace BalanceLab.Cli;

/// <summary>Bot z linii polecen: <c>random</c>, <c>mcts</c> albo <c>mcts:ITERACJE</c>.</summary>
internal static class BotSpec
{
    public const string Help = "random | mcts | mcts:ITERACJE (np. mcts:500)";

    public static Contestant<TState, TMove> Parse<TState, TMove>(string spec, string name)
        where TState : notnull
        where TMove : notnull
    {
        var parts = spec.Split(':', 2);
        return (parts[0].ToLowerInvariant(), parts.Length) switch
        {
            ("random", 1) => new(name, seed => new RandomBot<TState, TMove>(seed)),
            ("mcts", 1) => Mcts<TState, TMove>(name, null),
            ("mcts", 2) when int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0
                => Mcts<TState, TMove>(name, n),
            _ => throw new FormatException($"Nieznany bot '{spec}'. Dostepne: {Help}."),
        };
    }

    /// <summary>Nazwy do raportu: powtorzony bot dostaje numer, np. "random", "random#2".</summary>
    public static IReadOnlyList<string> UniqueNames(IReadOnlyList<string> specs) =>
        [.. specs.Select((spec, i) =>
        {
            var earlier = specs.Take(i).Count(s => s == spec);
            return earlier == 0 ? spec : $"{spec}#{earlier + 1}";
        })];

    private static Contestant<TState, TMove> Mcts<TState, TMove>(string name, int? iterations)
        where TState : notnull
        where TMove : notnull
    {
        var options = new MctsOptions { Iterations = iterations };
        return new(name, seed => new MctsBot<TState, TMove>(seed, options));
    }
}
