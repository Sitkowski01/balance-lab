using System.Collections.Immutable;
using System.Text.Json.Serialization;
using BalanceLab.Engine;

namespace BalanceLab.Simulation;

/// <summary>Uczestnik turnieju: nazwa do raportu i fabryka gracza z ziarna (nowy gracz na kazda partie).</summary>
public sealed record Contestant<TState, TMove>(string Name, Func<ulong, IPlayer<TState, TMove>> Create)
    where TState : notnull
    where TMove : notnull;

/// <summary>Ile partii, z jakiego ziarna i na ilu watkach.</summary>
public sealed record TournamentSpec(int Games, ulong Seed)
{
    /// <summary>Liczba watkow (-1 = tyle, ile da pula watkow); nie wplywa na wyniki, tylko na czas.</summary>
    public int MaxDegreeOfParallelism { get; init; } = -1;

    /// <summary>Limit ruchow w partii; dluzsza partia jest liczona jako nieskonczona.</summary>
    public int MaxMoves { get; init; } = 10_000;
}

/// <summary>Jedna rozegrana partia: kto gdzie siedzial i z jakim wynikiem.</summary>
public sealed record GameRecord(
    int Index,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] ulong Seed,
    ImmutableArray<string> Seating,
    ImmutableArray<double> Scores,
    int Moves,
    bool Completed)
{
    /// <summary>Miejsce zwyciezcy albo null przy remisie (brak jedynego najlepszego wyniku) i partii nieskonczonej.</summary>
    [JsonIgnore]
    public int? WinningSeat
    {
        get
        {
            if (!Completed)
            {
                return null;
            }

            var best = Scores.Max();
            var winners = Enumerable.Range(0, Scores.Length).Where(seat => Scores[seat] == best).ToArray();
            return winners.Length == 1 ? winners[0] : null;
        }
    }
}

/// <summary>Wyniki uczestnika w ukonczonych partiach (ich liczba to <see cref="Proportion.Trials"/>).</summary>
public sealed record ContestantStats(string Name, Proportion Wins, double MeanScore);

/// <summary>
/// Wyniki miejsca przy stole. Przy remisach uczciwa miara przewagi to <see cref="MeanScore"/>
/// (remis = pol wygranej); brak przewagi to 1 / liczba graczy.
/// </summary>
public sealed record SeatStats(int Seat, Proportion Wins, double MeanScore);

/// <summary>Dlugosc ukonczonych partii w ruchach.</summary>
public sealed record LengthStats(double Mean, int Min, int Median, int P90, int Max);

/// <summary>Podsumowanie turnieju — to trafia do raportu JSON.</summary>
public sealed record TournamentReport(
    string Game,
    int Games,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] ulong Seed,
    int Completed,
    int Draws,
    ImmutableArray<ContestantStats> Contestants,
    ImmutableArray<SeatStats> Seats,
    LengthStats? Length);

public sealed record TournamentResult(TournamentReport Report, ImmutableArray<GameRecord> Games);
