using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using BalanceLab.Simulation;

namespace BalanceLab.Cli;

/// <summary><c>balance-lab simulate</c>: turniej botow, podsumowanie w konsoli i raport JSON.</summary>
internal static class SimulateCommand
{
    public static Command Create(TextWriter output, TextWriter error)
    {
        var game = new Option<string>("--game") { Description = $"Gra: {string.Join(", ", GameCatalog.Names)}.", Required = true };
        var bots = new Option<string[]>("--bots")
        {
            Description = $"Uczestnicy, po jednym na miejsce przy stole: {BotSpec.Help}.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var games = new Option<int>("--games") { Description = "Liczba partii (wielokrotnosc liczby botow).", DefaultValueFactory = _ => 1_000 };
        var seed = new Option<ulong>("--seed") { Description = "Ziarno turnieju; to samo ziarno = ten sam raport.", DefaultValueFactory = _ => 1 };
        var threads = new Option<int?>("--threads") { Description = "Liczba watkow (domyslnie wszystkie rdzenie)." };
        var outFile = new Option<FileInfo?>("--out") { Description = "Plik na raport JSON." };
        var gamesFile = new Option<FileInfo?>("--games-out") { Description = "Plik JSON z kazda partia (ziarno, miejsca, wynik)." };
        // Katalog sprawdzamy przed startem — inaczej literowka w sciezce wyszlaby po godzinie symulacji.
        outFile.Validators.Add(r => RequireDirectory(r.GetValueOrDefault<FileInfo?>(), "--out", r.AddError));
        gamesFile.Validators.Add(r => RequireDirectory(r.GetValueOrDefault<FileInfo?>(), "--games-out", r.AddError));

        var command = new Command("simulate", "Rozgrywa turniej botow i liczy statystyki balansu.")
        {
            game, bots, games, seed, threads, outFile, gamesFile,
        };
        command.SetAction(parse =>
        {
            Func<TournamentResult> tournament;
            try
            {
                var spec = new TournamentSpec(parse.GetValue(games), parse.GetValue(seed));
                // Liczba watkow z definicji nie zmienia wyniku, tylko czas.
                if (parse.GetValue(threads) is { } count)
                {
                    spec = spec with { MaxDegreeOfParallelism = count };
                }

                tournament = GameCatalog.Find(parse.GetRequiredValue(game)).Prepare(parse.GetRequiredValue(bots), spec);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                error.WriteLine(UserMessage(ex));
                return Cli.UsageError;
            }

            try
            {
                var started = Stopwatch.GetTimestamp();
                var result = tournament();
                Summary.Write(output, result.Report, Stopwatch.GetElapsedTime(started));
                if (parse.GetValue(outFile) is { } report)
                {
                    File.WriteAllText(report.FullName, ReportJson.Serialize(result.Report));
                    output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Raport: {report.FullName}"));
                }

                if (parse.GetValue(gamesFile) is { } gamesOut)
                {
                    File.WriteAllText(gamesOut.FullName, ReportJson.Serialize(result.Games));
                }

                return Cli.Ok;
            }
            catch (Exception ex) when (ex is GameFailedException or IOException or UnauthorizedAccessException)
            {
                error.WriteLine(ex.Message);
                return Cli.RuntimeError;
            }
        });
        return command;
    }

    private static void RequireDirectory(FileInfo? file, string option, Action<string> addError)
    {
        if (file?.Directory is { Exists: false } directory)
        {
            addError($"{option}: katalog {directory.FullName} nie istnieje.");
        }
    }

    /// <summary>Komunikat bez technicznych dopiskow .NET typu "(Parameter 'spec')" i "Actual value was 0.".</summary>
    private static string UserMessage(Exception ex)
    {
        // .NET skleja komunikat znakami nowej linii systemu (na Windowsie \r\n) — bierzemy tylko pierwsza linie.
        var message = ex.Message.ReplaceLineEndings("\n").Split('\n')[0];
        return ex is ArgumentException { ParamName: { } name } ? message.Replace($" (Parameter '{name}')", "", StringComparison.Ordinal) : message;
    }
}
