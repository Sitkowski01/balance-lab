using BalanceLab.Bots;
using BalanceLab.Games.ConnectFour;
using BalanceLab.Simulation;
using BenchmarkDotNet.Attributes;

namespace BalanceLab.Benchmarks;

/// <summary>Jak turniej skaluje sie z liczba watkow: 2000 partii MCTS(50) kontra bot losowy.</summary>
public class TournamentBenchmarks
{
    private readonly ConnectFourGame _game = new();
    private readonly Contestant<ConnectFourState, ConnectFourMove>[] _contestants =
    [
        new("mcts", seed => new MctsBot<ConnectFourState, ConnectFourMove>(seed, new MctsOptions { Iterations = 50 })),
        new("random", seed => new RandomBot<ConnectFourState, ConnectFourMove>(seed)),
    ];

    [Params(1, -1)]
    public int Threads { get; set; }

    [Benchmark]
    public int Run() =>
        Tournament.Run(_game, _contestants, new TournamentSpec(2_000, 1) { MaxDegreeOfParallelism = Threads }).Report.Completed;
}
