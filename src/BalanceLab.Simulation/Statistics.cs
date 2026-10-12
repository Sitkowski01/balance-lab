namespace BalanceLab.Simulation;

/// <summary>Liczy raport z rozegranych partii. Partie nieskonczone nie wchodza do odsetkow wygranych.</summary>
internal static class Statistics
{
    public static TournamentReport Summarize(string game, TournamentSpec spec, IReadOnlyList<string> names, IReadOnlyList<GameRecord> records)
    {
        var completed = records.Where(r => r.Completed).ToArray();
        var winners = completed.Select(r => r.WinningSeat).ToArray(); // liczone raz, nie dla kazdego uczestnika

        var contestants = names.Select(name =>
        {
            var wins = completed.Where((r, i) => winners[i] is { } seat && r.Seating[seat] == name).Count();
            var meanScore = completed.Length == 0 ? 0 : completed.Average(r => r.Scores[r.Seating.IndexOf(name)]);
            return new ContestantStats(name, Proportion.Of(wins, completed.Length), meanScore);
        });

        // Sredni wynik miejsca liczy remis jako pol wygranej; sam odsetek wygranych traktuje remis jak
        // przegrana, a wtedy w zbalansowanej grze z remisami kazde miejsce wyglada na stratne.
        var seats = Enumerable.Range(0, names.Count).Select(seat => new SeatStats(
            seat,
            Proportion.Of(winners.Count(w => w == seat), completed.Length),
            completed.Length == 0 ? 0 : completed.Average(r => r.Scores[seat])));

        return new TournamentReport(
            game,
            spec.Games,
            spec.Seed,
            completed.Length,
            winners.Count(w => w is null),
            [.. contestants],
            [.. seats],
            Lengths(completed));
    }

    private static LengthStats? Lengths(GameRecord[] completed)
    {
        if (completed.Length == 0)
        {
            return null;
        }

        var sorted = completed.Select(r => r.Moves).Order().ToArray();
        // Percentyl metoda "najblizszej rangi": wartosc, ponizej ktorej jest co najmniej p% partii.
        int Percentile(double p) => sorted[(int)Math.Ceiling(p * sorted.Length) - 1];

        return new LengthStats(sorted.Average(), sorted[0], Percentile(0.5), Percentile(0.9), sorted[^1]);
    }
}
