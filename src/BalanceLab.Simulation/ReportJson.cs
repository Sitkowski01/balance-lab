using System.Text.Json;

namespace BalanceLab.Simulation;

/// <summary>
/// Kanoniczny zapis raportu. Sluzy tez do porownan: rekordy z <c>ImmutableArray</c> w srodku
/// porownuja tablice po referencji, wiec dwa identyczne raporty nie sa dla C# rowne.
/// </summary>
/// <remarks>Ziarna (ulong) ida jako tekst — JavaScript gubi cyfry liczb powyzej 2^53.</remarks>
public static class ReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string Serialize(TournamentReport report) => JsonSerializer.Serialize(report, Options);

    public static string Serialize(IEnumerable<GameRecord> games) => JsonSerializer.Serialize(games, Options);

    public static TournamentReport Deserialize(string json) =>
        JsonSerializer.Deserialize<TournamentReport>(json, Options) ?? throw new JsonException("Pusty raport.");
}
