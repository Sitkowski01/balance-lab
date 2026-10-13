using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

/// <summary>
/// Klejnoty: gracze zbieraja zetony, kupuja karty kopalni (kazda daje staly rabat) i przyciagaja
/// patronow. Kto pierwszy ma 15 punktow, konczy gre — runda jest dogrywana do ostatniego gracza.
/// Ukryte sa talie i karty zarezerwowane w ciemno; te drugie widzi tylko ich wlasciciel.
/// </summary>
/// <remarks>
/// Same zasady siedza w <see cref="KlejnotyRules"/>; ta klasa podpina je pod kontrakt silnika.
/// Dwa swiadome uproszczenia wobec Splendora, zeby boty mialy mniej rownowaznych ruchow:
/// placenie jest automatyczne, a gdy pasuje kilku patronow, przychodzi ten o najnizszym numerze.
/// </remarks>
/// <param name="variant">Zasady partii; domyslnie <see cref="KlejnotyVariant.Standard"/>.</param>
public sealed class KlejnotyGame(KlejnotyVariant? variant = null) : IGame<KlejnotyState, KlejnotyMove>
{
    public KlejnotyVariant Variant { get; } = variant ?? KlejnotyVariant.Standard;

    /// <summary>"Klejnoty", a dla wariantu z opisem zmian — zeby raport mowil, co bylo grane.</summary>
    public string Name => ReferenceEquals(Variant, KlejnotyVariant.Standard) ? "Klejnoty" : $"Klejnoty ({Variant.Name})";

    public int MinPlayers => KlejnotyRules.MinPlayers;

    public int MaxPlayers => KlejnotyRules.MaxPlayers;

    public KlejnotyState Setup(GameSetup setup) => KlejnotyRules.Setup(setup, Variant);

    public int PlayerCount(KlejnotyState state) => state.Players.Length;

    public int CurrentPlayer(KlejnotyState state) => state.ToMove;

    public IReadOnlyList<KlejnotyMove> LegalMoves(KlejnotyState state) => KlejnotyRules.LegalMoves(state);

    public KlejnotyState Apply(KlejnotyState state, KlejnotyMove move) => KlejnotyRules.Apply(state, move);

    public bool IsTerminal(KlejnotyState state) => state.Over;

    public ImmutableArray<double> Scores(KlejnotyState state) => KlejnotyRules.Scores(state);

    public ulong Hash(KlejnotyState state) => KlejnotyRules.Hash(state);

    public KlejnotyState Determinize(KlejnotyState state, int observer, ulong seed) =>
        KlejnotyRules.Determinize(state, observer, seed);

}
