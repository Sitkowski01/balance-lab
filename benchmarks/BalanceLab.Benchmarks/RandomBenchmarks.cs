using BalanceLab.Engine;
using BenchmarkDotNet.Attributes;

namespace BalanceLab.Benchmarks;

/// <summary>
/// Ile kosztuje deterministyczny generator w porownaniu z System.Random.
/// MCTS losuje miliony razy na sekunde, wiec to ma znaczenie.
/// </summary>
[MemoryDiagnoser]
public class RandomBenchmarks
{
    private const int Draws = 1_000;
    private readonly int[] _deck = Enumerable.Range(0, 90).ToArray();
    private readonly Random _random = new(42);
    private Pcg32 _pcg = Pcg32.Create(42);

    [Benchmark(Baseline = true)]
    public int SystemRandomNextInt()
    {
        var sum = 0;
        for (var i = 0; i < Draws; i++)
        {
            sum += _random.Next(6);
        }

        return sum;
    }

    [Benchmark]
    public int Pcg32NextInt()
    {
        var sum = 0;
        var rng = _pcg;
        for (var i = 0; i < Draws; i++)
        {
            (var value, rng) = rng.NextInt(6);
            sum += value;
        }

        _pcg = rng;
        return sum;
    }

    [Benchmark]
    public int Pcg32ShuffleDeckOf90()
    {
        (var shuffled, _pcg) = _pcg.Shuffle(_deck);
        return shuffled[0];
    }
}
