using BalanceLab.Engine;

namespace BalanceLab.Bots;

/// <summary>
/// Patrzy jeden ruch do przodu: wykonuje kazdy legalny ruch i bierze ten, po ktorym ocena gry
/// jest najlepsza dla niego. Szybki i przewidywalny — dobry przeciwnik do sprawdzania, czy prosta
/// strategia (np. "kupuj, co sie da") nie dominuje gry.
/// </summary>
/// <remarks>Rowne oceny rozstrzyga losowo, zeby nie faworyzowac ruchow z poczatku listy.</remarks>
public sealed class GreedyBot<TState, TMove>(IEvaluator<TState> evaluator, ulong seed) : IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    private readonly IEvaluator<TState> _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    private Pcg32 _rng = Pcg32.Create(seed);

    public TMove ChooseMove(IGame<TState, TMove> game, TState state)
    {
        ArgumentNullException.ThrowIfNull(game);

        var moves = game.LegalMoves(state);
        if (moves.Count == 0)
        {
            throw new InvalidOperationException("Brak legalnych ruchow - partia sie skonczyla.");
        }

        var player = game.CurrentPlayer(state);
        var best = new List<TMove>();
        var bestValue = double.NegativeInfinity;
        foreach (var move in moves)
        {
            var value = _evaluator.Evaluate(game.Apply(state, move), player);
            if (double.IsNaN(value))
            {
                throw new InvalidOperationException($"Ocena ruchu {move} to NaN - blad w ocenie pozycji.");
            }

            if (value > bestValue)
            {
                best.Clear();
                bestValue = value;
            }

            if (value == bestValue)
            {
                best.Add(move);
            }
        }

        (var index, _rng) = _rng.NextInt(best.Count);
        return best[index];
    }
}
