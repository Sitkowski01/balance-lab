using System.Globalization;
using BalanceLab.Simulation;

namespace BalanceLab.Cli;

/// <summary>Czytelne podsumowanie turnieju w konsoli; pelne dane sa w raporcie JSON.</summary>
internal static class Summary
{
    public static void Write(TextWriter output, TournamentReport report, TimeSpan elapsed)
    {
        var c = CultureInfo.InvariantCulture;
        output.WriteLine(string.Create(c, $"{report.Game}: {report.Games} partii, ziarno {report.Seed}, ukonczone {report.Completed}, remisy {report.Draws}"));
        output.WriteLine();
        output.WriteLine("Uczestnik            wygrane  95% przedzial     sredni wynik");
        foreach (var s in report.Contestants)
        {
            output.WriteLine(string.Create(c, $"{s.Name,-20} {Rate(s.Wins),7}  {Interval(s.Wins),-16}  {Score(s.Wins, s.MeanScore)}"));
        }

        output.WriteLine();
        output.WriteLine("Miejsce przy stole   wygrane  95% przedzial     sredni wynik");
        foreach (var s in report.Seats)
        {
            output.WriteLine(string.Create(c, $"{s.Seat + 1,-20} {Rate(s.Wins),7}  {Interval(s.Wins),-16}  {Score(s.Wins, s.MeanScore)}"));
        }

        if (report.Length is { } length)
        {
            output.WriteLine();
            output.WriteLine(string.Create(c, $"Dlugosc partii (ruchy): srednio {length.Mean:0.0}, mediana {length.Median}, 90% ponizej {length.P90}, od {length.Min} do {length.Max}"));
        }

        var perSecond = report.Games / Math.Max(elapsed.TotalSeconds, 1e-9);
        output.WriteLine(string.Create(c, $"Czas: {elapsed.TotalSeconds:0.00} s ({perSecond:0} partii/s)"));
    }

    // Bez ukonczonych partii "0%" sugerowaloby zmierzony wynik, a to brak danych.
    private static string Rate(Proportion p) => p.Trials == 0 ? "-" : Percent(p.Rate);

    private static string Interval(Proportion p) => p.Trials == 0 ? "brak danych" : $"{Percent(p.Low)} - {Percent(p.High)}";

    private static string Score(Proportion p, double score) => p.Trials == 0 ? "-" : string.Create(CultureInfo.InvariantCulture, $"{score,5:0.000}");

    private static string Percent(double rate) => string.Create(CultureInfo.InvariantCulture, $"{rate * 100:0.0}%");
}
