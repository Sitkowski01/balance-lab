namespace BalanceLab.Simulation;

/// <summary>
/// Odsetek sukcesow z 95% przedzialem ufnosci Wilsona. Zwykle p ± 1,96·√(p(1−p)/n) klamie
/// przy malym n i przy p blisko 0 lub 1 (potrafi wyjsc poza [0, 1]); Wilson nie.
/// </summary>
public sealed record Proportion(int Successes, int Trials, double Rate, double Low, double High)
{
    private const double Z = 1.959963984540054; // kwantyl 97,5% rozkladu normalnego

    public static Proportion Of(int successes, int trials)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(successes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(successes, trials);
        if (trials == 0)
        {
            return new Proportion(0, 0, 0, 0, 1); // brak danych = nic nie wiadomo
        }

        var p = (double)successes / trials;
        var z2n = Z * Z / trials;
        var center = (p + (z2n / 2)) / (1 + z2n);
        var half = Z * Math.Sqrt((p * (1 - p) / trials) + (z2n / (4 * trials))) / (1 + z2n);
        // Na krancach wzor daje dokladnie 0 albo 1, ale w liczbach zmiennoprzecinkowych wychodzi
        // np. 0,9999999999 — mniej niz sam odsetek. Krance ustawiamy wprost.
        var low = successes == 0 ? 0 : Math.Max(0, center - half);
        var high = successes == trials ? 1 : Math.Min(1, center + half);
        return new Proportion(successes, trials, p, low, high);
    }

    /// <summary>Czy wartosc lezy poza przedzialem — np. 0,5 dla miejsca przy stole oznacza przewage.</summary>
    public bool Excludes(double value) => value < Low || value > High;
}
