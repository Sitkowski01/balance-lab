using BalanceLab.Bots;
using BalanceLab.Games.ConnectFour;
using BenchmarkDotNet.Attributes;

namespace BalanceLab.Benchmarks;

/// <summary>Koszt jednego ruchu MCTS z pustej planszy Czworek przy roznych budzetach iteracji.</summary>
[MemoryDiagnoser]
public class MctsBenchmarks
{
    private readonly ConnectFourGame _game = new();
    private readonly ConnectFourState _start = new ConnectFourGame().Setup(new Engine.GameSetup(2, 0));

    [Params(1_000, 10_000)]
    public int Iterations { get; set; }

    [Benchmark]
    public ConnectFourMove Search() =>
        new MctsBot<ConnectFourState, ConnectFourMove>(42, new MctsOptions { Iterations = Iterations }).ChooseMove(_game, _start);
}
