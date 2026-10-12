using BalanceLab.Engine;
using BalanceLab.Games.ConnectFour;
using BalanceLab.Simulation;

namespace BalanceLab.Cli;

/// <summary>Gry dostepne z linii polecen. Nowa gra = jedna linia tutaj.</summary>
internal static class GameCatalog
{
    private static readonly Dictionary<string, IGameEntry> Games = new(StringComparer.OrdinalIgnoreCase)
    {
        ["connectfour"] = new GameEntry<ConnectFourState, ConnectFourMove>(new ConnectFourGame()),
    };

    public static IReadOnlyCollection<string> Names => Games.Keys;

    public static IGameEntry Find(string name) =>
        Games.TryGetValue(name, out var entry)
            ? entry
            : throw new FormatException($"Nieznana gra '{name}'. Dostepne: {string.Join(", ", Names)}.");
}

/// <summary>Ukrywa typy stanu i ruchu, zeby CLI moglo obslugiwac kazda gre tak samo.</summary>
internal interface IGameEntry
{
    /// <summary>
    /// Sprawdza boty i parametry turnieju (bledy wychodza od razu, przed rozgrywka) i zwraca
    /// gotowy do uruchomienia turniej.
    /// </summary>
    Func<TournamentResult> Prepare(IReadOnlyList<string> bots, TournamentSpec spec);
}

internal sealed class GameEntry<TState, TMove>(IGame<TState, TMove> game) : IGameEntry
    where TState : notnull
    where TMove : notnull
{
    public Func<TournamentResult> Prepare(IReadOnlyList<string> bots, TournamentSpec spec)
    {
        var names = BotSpec.UniqueNames(bots);
        var contestants = bots.Select((bot, i) => BotSpec.Parse<TState, TMove>(bot, names[i])).ToArray();
        Tournament.Validate(game, contestants, spec);
        return () => Tournament.Run(game, contestants, spec);
    }
}
