using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

/// <summary>
/// Prosta ocena pozycji: punkty licza sie najbardziej, potem rabaty (trwale), potem zetony.
/// Do tego postep ku najlepszej osiagalnej karcie i ku patronom — inaczej bot zbieralby zetony
/// bez celu. Wagi to profil strategii (<see cref="KlejnotyWeights.Profiles"/>); domyslnie wyrownany.
/// </summary>
public sealed class KlejnotyEvaluator(KlejnotyWeights? weights = null) : IEvaluator<KlejnotyState>
{
    public KlejnotyWeights Weights { get; } = weights ?? KlejnotyWeights.Balanced;

    public double Evaluate(KlejnotyState state, int player)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Over)
        {
            // Koniec partii przebija kazda heurystyke: wygrana jest warta wiecej niz dowolna pozycja.
            return 10_000 * KlejnotyRules.Scores(state)[player];
        }

        var me = state.Players[player];
        var w = Weights;
        var value = (w.Points * me.Points) + (w.Bonuses * me.Bonuses.Total) + (w.Gold * me.Tokens[Gem.Gold])
            + (w.Tokens * (me.Tokens.Total - me.Tokens[Gem.Gold])) + (w.Reserved * me.Reserved.Length)
            // Nadmiar ponad 10 i tak trzeba oddac: bez kary bot bralby zetony, ktore zaraz zwroci.
            - (w.Overflow * Math.Max(0, me.Tokens.Total - KlejnotyState.MaxTokens));

        // Najlepsza karta w zasiegu: wartosciowa i z malym brakiem zetonow.
        var targets = state.Market.Where(id => id >= 0).Concat(me.Reserved.Select(r => r.Card));
        value += w.Target * targets.Select(id => ((3.0 * state.Variant.AllCards[id].Points) + 1) / (1 + Shortfall(me, state.Variant.AllCards[id]))).DefaultIfEmpty(0).Max();

        // Patroni: udzial juz zebranych rabatow w wymaganiu.
        foreach (var patron in state.Patrons.Select(id => state.Variant.AllPatrons[id]))
        {
            var have = Enumerable.Range(0, Gems.Colors).Sum(c => Math.Min(me.Bonuses[(Gem)c], patron.Requirement[(Gem)c]));
            value += w.Patrons * have / patron.Requirement.Total;
        }

        return value;
    }

    /// <summary>Ilu zetonow brakuje do kupienia karty, po rabatach i zlocie.</summary>
    public static int Shortfall(PlayerBoard player, Card card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        var missing = 0;
        for (var color = Gem.White; color < Gem.Gold; color++)
        {
            missing += Math.Max(0, card.Cost[color] - player.Bonuses[color] - player.Tokens[color]);
        }

        return Math.Max(0, missing - player.Tokens[Gem.Gold]);
    }
}
