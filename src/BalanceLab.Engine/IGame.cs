using System.Collections.Immutable;

namespace BalanceLab.Engine;

/// <summary>
/// Zasady gry jako czyste funkcje na niemutowalnym stanie. Silnik, boty i symulator znaja
/// tylko ten kontrakt, wiec nowa gra nie wymaga zmian w zadnym z nich.
/// </summary>
/// <remarks>
/// Na razie gracz widzi caly stan — dobre dla gier z pelna informacja. Gry z ukrytymi kartami
/// (Klejnoty) dostana widok gracza i determinizacje razem z ISMCTS.
/// </remarks>
/// <typeparam name="TState">Niemutowalny stan; cala losowosc gry siedzi w nim (np. <see cref="Pcg32"/>).</typeparam>
/// <typeparam name="TMove">Ruch gracza.</typeparam>
public interface IGame<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    string Name { get; }

    int MinPlayers { get; }

    int MaxPlayers { get; }

    /// <summary>Stan poczatkowy. To samo <see cref="GameSetup"/> zawsze daje ten sam stan.</summary>
    TState Setup(GameSetup setup);

    /// <summary>Liczba graczy w partii (dlugosc tablicy z <see cref="Scores"/>).</summary>
    int PlayerCount(TState state);

    /// <summary>Indeks gracza na ruchu, od 0.</summary>
    int CurrentPlayer(TState state);

    /// <summary>Legalne ruchy gracza na ruchu; pusta lista tylko w stanie koncowym.</summary>
    IReadOnlyList<TMove> LegalMoves(TState state);

    /// <summary>
/// Nowy stan po ruchu. Nielegalny ruch (takze po koncu partii) konczy sie
/// <see cref="IllegalMoveException"/> — gra jest sedzia, gracz nie jest zaufany.
/// </summary>
    TState Apply(TState state, TMove move);

    bool IsTerminal(TState state);

    /// <summary>Wynik kazdego gracza w stanie koncowym: 1 wygrana, 0 przegrana, ulamki przy remisie.</summary>
    ImmutableArray<double> Scores(TState state);

    /// <summary>
    /// Hash stabilny miedzy procesami i maszynami (w przeciwienstwie do <c>GetHashCode</c>):
    /// po nim porownujemy partie i powtorki z dziennika.
    /// </summary>
    ulong Hash(TState state);
}
