using BalanceLab.Bots;
using BalanceLab.Engine;
using BalanceLab.Games.Klejnoty;
using BenchmarkDotNet.Attributes;

namespace BalanceLab.Benchmarks;

/// <summary>
/// Klejnoty: losowa partia (to robi kazda iteracja MCTS) i sama determinizacja (to dochodzi
/// w kazdej iteracji ISMCTS). Stosunek obu mowi, ile kosztuje ukrycie informacji.
/// </summary>
[MemoryDiagnoser]
public class KlejnotyBenchmarks
{
    private readonly KlejnotyGame _game = new();
    private readonly KlejnotyState _midGame;
    private ulong _seed;

    public KlejnotyBenchmarks()
    {
        IPlayer<KlejnotyState, KlejnotyMove>[] players =
            [new RandomBot<KlejnotyState, KlejnotyMove>(1), new RandomBot<KlejnotyState, KlejnotyMove>(2)];
        _midGame = Match.Play(_game, new GameSetup(2, 7), players, maxMoves: 30).FinalState;
    }

    /// <summary>Petla jak w symulacji MCTS: legalne ruchy, losowy wybor, Apply — bez widokow z Match.</summary>
    [Benchmark]
    public int RandomPlayout()
    {
        var rng = Pcg32.Create(++_seed);
        var state = _game.Setup(new GameSetup(2, _seed));
        var moves = 0;
        while (!_game.IsTerminal(state) && moves < 10_000)
        {
            var legal = _game.LegalMoves(state);
            (var index, rng) = rng.NextInt(legal.Count);
            state = _game.Apply(state, legal[index]);
            moves++;
        }

        return moves;
    }

    [Benchmark]
    public int Determinize() => _game.Determinize(_midGame, 0, ++_seed).Decks[0].Length;
}
