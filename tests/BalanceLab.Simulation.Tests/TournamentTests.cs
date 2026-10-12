using BalanceLab.Bots;
using BalanceLab.Engine;
using BalanceLab.Games.ConnectFour;
using BalanceLab.Testing;

namespace BalanceLab.Simulation.Tests;

public sealed class TournamentTests
{
    private static readonly NimGame Nim = new();

    [Fact]
    public void Results_do_not_depend_on_the_number_of_threads()
    {
        var single = Run(new TournamentSpec(300, 7) { MaxDegreeOfParallelism = 1 });
        var four = Run(new TournamentSpec(300, 7) { MaxDegreeOfParallelism = 4 });
        var all = Run(new TournamentSpec(300, 7));

        // Porownanie przez JSON: rekordy z ImmutableArray porownuja tablice po referencji.
        Assert.Equal(ReportJson.Serialize(single.Games), ReportJson.Serialize(four.Games));
        Assert.Equal(ReportJson.Serialize(single.Games), ReportJson.Serialize(all.Games));
        Assert.Equal(ReportJson.Serialize(single.Report), ReportJson.Serialize(all.Report));
    }

    [Fact]
    public void Different_seed_gives_different_games()
    {
        var a = Run(new TournamentSpec(50, 1));
        var b = Run(new TournamentSpec(50, 2));

        Assert.NotEqual(a.Games.Select(g => g.Seed), b.Games.Select(g => g.Seed));
        Assert.Equal(a.Games.Select(g => g.Seed), Enumerable.Range(0, 50).Select(i => Tournament.SeedFor(1, i, 0)));
    }

    [Fact]
    public void Every_contestant_sits_in_every_seat_equally_often()
    {
        var result = Run(new TournamentSpec(90, 3), players: 3);

        foreach (var name in new[] { "bot0", "bot1", "bot2" })
        {
            Assert.All(Enumerable.Range(0, 3), seat => Assert.Equal(30, result.Games.Count(g => g.Seating[seat] == name)));
        }

        Assert.Equal(["bot0", "bot1", "bot2"], result.Games[0].Seating.ToArray());
        Assert.Equal(["bot2", "bot0", "bot1"], result.Games[1].Seating.ToArray());
    }

    [Fact]
    public void Report_counts_add_up()
    {
        var report = Run(new TournamentSpec(201, 4), players: 3).Report;

        Assert.Equal(("Nim", 201, 4UL, 201, 0), (report.Game, report.Games, report.Seed, report.Completed, report.Draws));
        Assert.Equal(201, report.Contestants.Sum(c => c.Wins.Successes));
        Assert.Equal(201, report.Seats.Sum(s => s.Wins.Successes));
        Assert.All(report.Contestants, c => Assert.Equal(201, c.Wins.Trials));
        Assert.Equal(1.0, report.Contestants.Sum(c => c.MeanScore), 9);
        Assert.Equal(1.0, report.Seats.Sum(s => s.MeanScore), 9);
        Assert.All(report.Seats, s => Assert.Equal((double)s.Wins.Successes / 201, s.MeanScore, 12));
        Assert.Equal([0, 1, 2], report.Seats.Select(s => s.Seat));
        Assert.Equal(["bot0", "bot1", "bot2"], report.Contestants.Select(c => c.Name));
    }

    [Fact]
    public void Draws_and_mean_scores_come_from_the_games()
    {
        var result = Tournament.Run(new ConnectFourGame(),
            [RandomContestant<ConnectFourState, ConnectFourMove>("a"), RandomContestant<ConnectFourState, ConnectFourMove>("b")],
            new TournamentSpec(400, 11));
        var report = result.Report;

        Assert.Equal(result.Games.Count(g => g.WinningSeat is null), report.Draws);
        Assert.Equal(report.Completed, report.Contestants.Sum(c => c.Wins.Successes) + report.Draws);
        var a = result.Games.Average(g => g.Scores[g.Seating.IndexOf("a")]);
        Assert.Equal(a, report.Contestants[0].MeanScore, 12);
    }

    [Fact]
    public void Random_connect_four_shows_the_first_player_advantage()
    {
        // Pierwsza prawdziwa obserwacja balansu: przy losowych graczach zaczynajacy wygrywa czesciej,
        // a przedzial ufnosci wyklucza 50%. Rotacja miejsc sprawia, ze sami gracze wychodza po rowno.
        var report = Tournament.Run(new ConnectFourGame(),
            [RandomContestant<ConnectFourState, ConnectFourMove>("a"), RandomContestant<ConnectFourState, ConnectFourMove>("b")],
            new TournamentSpec(4_000, 12)).Report;

        Assert.True(report.Seats[0].Wins.Low > 0.5, $"miejsce 0: {report.Seats[0].Wins}");
        Assert.True(report.Seats[1].Wins.High < 0.5);
        // Remisy: srednie wyniki miejsc sumuja sie do 1, odsetki wygranych juz nie.
        Assert.Equal(1.0, report.Seats.Sum(s => s.MeanScore), 9);
        Assert.True(report.Seats[0].MeanScore > 0.5);
        Assert.True(report.Draws > 0);
        Assert.All(report.Contestants, c => Assert.InRange(c.Wins.Rate, 0.45, 0.55));
    }

    private static TournamentResult Run(
        TournamentSpec spec,
        int players = 2,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default) =>
        Tournament.Run(Nim, [.. Enumerable.Range(0, players).Select(i => RandomContestant<NimState, int>($"bot{i}"))],
            spec, progress, cancellationToken);

    private static Contestant<TState, TMove> RandomContestant<TState, TMove>(string name)
        where TState : notnull
        where TMove : notnull =>
        new(name, seed => new RandomBot<TState, TMove>(seed));

    /// <summary>Progress&lt;T&gt; raportuje przez SynchronizationContext, wiec w tescie zbieramy wprost (z blokada).</summary>
    private sealed class SynchronousProgress(List<int> reports) : IProgress<int>
    {
        public void Report(int value)
        {
            lock (reports)
            {
                reports.Add(value);
            }
        }
    }
}
