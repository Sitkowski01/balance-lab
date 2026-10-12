namespace BalanceLab.Engine;

/// <summary>Wszystko, od czego zalezy stan poczatkowy: liczba graczy i ziarno losowosci.</summary>
public readonly record struct GameSetup(int PlayerCount, ulong Seed)
{
    /// <summary>Rzuca, gdy gra nie obsluguje takiej liczby graczy.</summary>
    public void EnsurePlayers(int min, int max)
    {
        if (PlayerCount < min || PlayerCount > max)
        {
            throw new ArgumentException($"Gra obsluguje od {min} do {max} graczy, podano {PlayerCount}.");
        }
    }
}
