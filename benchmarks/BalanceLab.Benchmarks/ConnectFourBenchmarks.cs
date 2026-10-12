using BalanceLab.Bots;
using BalanceLab.Engine;
using BalanceLab.Games.ConnectFour;
using BenchmarkDotNet.Attributes;

namespace BalanceLab.Benchmarks;

/// <summary>
/// Losowa partia od pustej planszy do konca — dokladnie to, co MCTS robi tysiace razy
/// przy kazdym ruchu, wiec liczba partii na sekunde wyznacza sile bota przy danym czasie.
/// </summary>
[MemoryDiagnoser]
public class ConnectFourBenchmarks
{
    private readonly ConnectFourGame _game = new();
    private ulong _seed;

    [Benchmark]
    public int RandomPlayout()
    {
        _seed++;
        IPlayer<ConnectFourState, ConnectFourMove>[] players =
            [new RandomBot<ConnectFourState, ConnectFourMove>(_seed), new RandomBot<ConnectFourState, ConnectFourMove>(~_seed)];
        return Match.Play(_game, new GameSetup(2, 0), players).Moves.Length;
    }
}
