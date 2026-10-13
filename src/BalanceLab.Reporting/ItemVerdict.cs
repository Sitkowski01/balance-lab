using BalanceLab.Simulation;

namespace BalanceLab.Reporting;

public enum ItemVerdict
{
    /// <summary>Nic podejrzanego przy tej liczbie partii.</summary>
    None,

    /// <summary>Wlasciciel wygrywa wyraznie czesciej niz przy przecietnej karcie — kandydat do oslabienia.</summary>
    Strong,

    /// <summary>Wlasciciel wygrywa wyraznie rzadziej — karta szkodzi temu, kto ja bierze.</summary>
    Weak,

    /// <summary>Prawie nikt jej nie bierze — martwa karta.</summary>
    Dead,
}

/// <summary>Ocena elementu gry dla projektanta: progi jawne, zeby dalo sie je wytlumaczyc i zmienic.</summary>
public static class ItemVerdicts
{
    /// <summary>Brana w mniej niz 5% partii = martwa.</summary>
    public const double DeadBelow = 0.05;

    /// <summary>
    /// Minimalna roznica wobec grupy (3 punkty procentowe). Przy tysiacach partii i dziesiatkach kart
    /// nawet znikoma roznica wychodzi "istotna"; projektanta obchodza tylko te, ktore cos znacza.
    /// </summary>
    public const double Margin = 0.03;

    /// <summary>
    /// "Mocna"/"slaba", gdy caly 95% przedzial wygranych wlasciciela lezy ponad/pod srednia jej grupy
    /// o wiecej niz <see cref="Margin"/>. Martwosc najpierw: z kilku partii przedzial i tak bylby szeroki.
    /// </summary>
    public static ItemVerdict Of(ItemStats item, Proportion group)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(group);

        if (item.Taken.Trials == 0)
        {
            return ItemVerdict.None; // zadna partia sie nie skonczyla: nic nie wiadomo
        }

        if (item.Taken.Rate < DeadBelow)
        {
            return ItemVerdict.Dead;
        }

        if (item.OwnerWins.Low > group.Rate + Margin)
        {
            return ItemVerdict.Strong;
        }

        return item.OwnerWins.High < group.Rate - Margin ? ItemVerdict.Weak : ItemVerdict.None;
    }

    public static string Describe(ItemVerdict verdict) => verdict switch
    {
        ItemVerdict.Strong => "za mocna",
        ItemVerdict.Weak => "za slaba",
        ItemVerdict.Dead => "martwa",
        _ => "",
    };
}
