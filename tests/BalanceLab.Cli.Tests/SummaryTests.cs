using BalanceLab.Simulation;

namespace BalanceLab.Cli.Tests;

public sealed class SummaryTests
{
    [Fact]
    public void Prints_a_readable_summary()
    {
        var report = new TournamentReport("ConnectFour", 200, 42, 198, 3,
            [new ContestantStats("mcts:200", Proportion.Of(150, 198), 0.765), new ContestantStats("random", Proportion.Of(45, 198), 0.235)],
            [new SeatStats(0, Proportion.Of(110, 198), 0.563), new SeatStats(1, Proportion.Of(85, 198), 0.437)],
            new LengthStats(21.25, 7, 20, 31, 42));
        using var output = new StringWriter();

        Summary.Write(output, report, TimeSpan.FromSeconds(0.5));

        Assert.Equal(
            """
            ConnectFour: 200 partii, ziarno 42, ukonczone 198, remisy 3

            Uczestnik            wygrane  95% przedzial     sredni wynik
            mcts:200               75.8%  69.3% - 81.2%     0.765
            random                 22.7%  17.4% - 29.1%     0.235

            Miejsce przy stole   wygrane  95% przedzial     sredni wynik
            1                      55.6%  48.6% - 62.3%     0.563
            2                      42.9%  36.2% - 49.9%     0.437

            Dlugosc partii (ruchy): srednio 21.3, mediana 20, 90% ponizej 31, od 7 do 42
            Czas: 0.50 s (400 partii/s)

            """.ReplaceLineEndings(), output.ToString());
    }

    [Fact]
    public void Skips_lengths_when_no_game_finished_and_survives_zero_time()
    {
        var report = new TournamentReport("Nim", 5, 1, 0, 0,
            [new ContestantStats("a", Proportion.Of(0, 0), 0)], [new SeatStats(0, Proportion.Of(0, 0), 0)], null);
        using var output = new StringWriter();

        Summary.Write(output, report, TimeSpan.Zero);

        Assert.DoesNotContain("Dlugosc", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("a                          -  brak danych       -", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("1                          -  brak danych       -", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Czas: 0.00 s (5000000000 partii/s)", output.ToString(), StringComparison.Ordinal);
    }
}
