using System.Text.Json;
using BalanceLab.Bots;
using BalanceLab.Testing;

namespace BalanceLab.Simulation.Tests;

public sealed class ReportJsonTests
{
    private static readonly TournamentResult Result = Tournament.Run(new NimGame(),
        [new Contestant<NimState, int>("a", s => new RandomBot<NimState, int>(s)), new Contestant<NimState, int>("b", s => new RandomBot<NimState, int>(s))],
        new TournamentSpec(40, ulong.MaxValue));

    [Fact]
    public void Report_survives_a_round_trip()
    {
        var json = ReportJson.Serialize(Result.Report);

        Assert.Equal(json, ReportJson.Serialize(ReportJson.Deserialize(json)));
    }

    [Fact]
    public void Seeds_are_written_as_text_so_javascript_keeps_every_digit()
    {
        using var report = JsonDocument.Parse(ReportJson.Serialize(Result.Report));
        using var games = JsonDocument.Parse(ReportJson.Serialize(Result.Games));

        Assert.Equal("18446744073709551615", report.RootElement.GetProperty("seed").GetString());
        Assert.Equal(JsonValueKind.String, games.RootElement[0].GetProperty("seed").ValueKind);
    }

    [Fact]
    public void Report_uses_camel_case_and_skips_derived_fields()
    {
        var json = ReportJson.Serialize(Result.Report);
        var games = ReportJson.Serialize(Result.Games);

        Assert.Contains("\"contestants\": [", json, StringComparison.Ordinal);
        Assert.Contains("\"wins\": {", json, StringComparison.Ordinal);
        Assert.DoesNotContain("winningSeat", games, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_document_is_rejected()
    {
        Assert.Equal("Pusty raport.", Assert.Throws<JsonException>(() => ReportJson.Deserialize("null")).Message);
    }
}
