using BalanceLab.Engine;

namespace BalanceLab.Bots;

/// <summary>
/// Wybiera losowy legalny ruch. Punkt odniesienia dla silniejszych botow
/// i najszybszy sposob, zeby rozegrac duzo partii i sprawdzic zasady gry.
/// </summary>
/// <remarks>Ma wlasny generator, wiec ten sam bot z tym samym ziarnem gra tak samo.</remarks>
public sealed class RandomBot<TState, TMove>(ulong seed) : IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    private Pcg32 _rng = Pcg32.Create(seed);

    public TMove ChooseMove(IGame<TState, TMove> game, TState state)
    {
        ArgumentNullException.ThrowIfNull(game);

        var moves = game.LegalMoves(state);
        if (moves.Count == 0)
        {
            throw new InvalidOperationException("Brak legalnych ruchow - partia sie skonczyla.");
        }

        (var index, _rng) = _rng.NextInt(moves.Count);
        return moves[index];
    }
}
