namespace BalanceLab.Simulation.Tests;

public sealed class StatisticsTests
{
    [Fact]
    public void Game_length_uses_nearest_rank_percentiles()
    {
        GameRecord[] games = [.. Enumerable.Range(1, 10).Reverse().Select(moves => Game(moves, [1.0, 0.0]))];

        var length = Statistics.Summarize("x", new TournamentSpec(10, 1), ["a", "b"], games).Length;

        Assert.Equal(new LengthStats(5.5, 1, 5, 9, 10), length! with { StdDev = 0 });
        Assert.Equal(Math.Sqrt(82.5 / 9), length.StdDev, 12);
    }

    [Fact]
    public void Unfinished_games_are_left_out_of_lengths_and_rates()
    {
        GameRecord[] games = [Game(4, [1.0, 0.0]), Game(1_000, [], completed: false), Game(6, [0.5, 0.5])];

        var report = Statistics.Summarize("x", new TournamentSpec(3, 1), ["a", "b"], games);

        Assert.Equal((2, 1), (report.Completed, report.Draws));
        Assert.Equal(new LengthStats(5, 4, 4, 6, 6) { StdDev = Math.Sqrt(2) }, report.Length);
        Assert.Equal(Proportion.Of(1, 2), report.Contestants[0].Wins);
        Assert.Equal(0.75, report.Contestants[0].MeanScore);
        Assert.Equal((0.75, 0.25), (report.Seats[0].MeanScore, report.Seats[1].MeanScore));
    }

    [Theory]
    [InlineData(new[] { 1.0, 0.0, 0.0 }, 0)]
    [InlineData(new[] { 0.0, 0.0, 1.0 }, 2)]
    [InlineData(new[] { 0.5, 0.5, 0.0 }, null)]
    [InlineData(new[] { 0.5, 0.5 }, null)]
    public void Winner_is_the_single_best_score(double[] scores, int? winner)
    {
        Assert.Equal(winner, Game(5, scores).WinningSeat);
        Assert.Null(Game(5, scores, completed: false).WinningSeat);
    }

    private static GameRecord Game(int moves, double[] scores, bool completed = true) =>
        new(0, 0, [.. Seats[..Math.Max(2, scores.Length)]], [.. scores], moves, completed);

    private static readonly string[] Seats = ["a", "b", "c"];
}
