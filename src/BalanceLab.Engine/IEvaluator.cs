namespace BalanceLab.Engine;

/// <summary>
/// Wiedza o grze w jednej liczbie: jak dobra jest pozycja dla gracza. Gra moze ja dostarczyc
/// obok zasad; boty heurystyczne korzystaja z niej, nie znajac samej gry.
/// </summary>
public interface IEvaluator<in TState>
    where TState : notnull
{
    /// <summary>Im wiecej, tym lepiej dla <paramref name="player"/>; skala dowolna, ale stala w obrebie gry.</summary>
    double Evaluate(TState state, int player);
}
