using System.Collections.Immutable;

namespace BalanceLab.Games.Klejnoty;

/// <summary>
/// Wagi oceny pozycji. Rozne wagi to rozne style gry (profile strategii): turniej profili
/// pokazuje, czy jedna prosta strategia nie dominuje gry — wtedy zasady sa do poprawki.
/// </summary>
/// <param name="Points">Za kazdy punkt zwyciestwa.</param>
/// <param name="Bonuses">Za kazdy rabat (kupiona karte): trwaly "silnik" gospodarki.</param>
/// <param name="Gold">Za zloty zeton (zastepuje dowolny kolor).</param>
/// <param name="Tokens">Za zeton koloru.</param>
/// <param name="Reserved">Za karte zarezerwowana.</param>
/// <param name="Overflow">Kara za zeton ponad limit 10 (i tak trzeba go oddac).</param>
/// <param name="Target">Mnoznik postepu ku najlepszej karcie w zasiegu.</param>
/// <param name="Patrons">Za pelne spelnienie wymagan patrona (czesciowe proporcjonalnie).</param>
public sealed record KlejnotyWeights(
    double Points,
    double Bonuses,
    double Gold,
    double Tokens,
    double Reserved,
    double Overflow,
    double Target,
    double Patrons)
{
    /// <summary>Wagi dobrane recznie jako rozsadna gra "wszystkiego po trochu".</summary>
    public static KlejnotyWeights Balanced { get; } = new(20, 6, 3, 2, 1, 3, 1, 4);

    /// <summary>
    /// Nazwane profile do turniejow. Kazdy przesadza w jedna strone — jesli ktorys wygrywa
    /// z wyrownanym, gra nagradza zbyt prosta strategie.
    /// </summary>
    public static ImmutableSortedDictionary<string, KlejnotyWeights> Profiles { get; } =
        new Dictionary<string, KlejnotyWeights>
        {
            ["wyrownany"] = Balanced,
            // Punkty od razu, rabaty malo warte: kupuje drogie karty z punktami.
            ["punkty"] = Balanced with { Points = 30, Bonuses = 2, Target = 3 },
            // Silnik: najpierw duzo tanich rabatow, punkty pozniej.
            ["silnik"] = Balanced with { Points = 12, Bonuses = 12, Target = 0.5 },
            // Poluje na patronow: wymagania patronow wazniejsze niz pojedyncze karty.
            ["patroni"] = Balanced with { Patrons = 15 },
            // Chomik: zbiera zetony i zloto, rezerwuje na zapas.
            ["zetony"] = Balanced with { Gold = 6, Tokens = 4, Reserved = 4, Bonuses = 4 },
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
}
