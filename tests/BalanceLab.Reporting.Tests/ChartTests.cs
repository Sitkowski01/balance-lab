using System.Text.RegularExpressions;
using BalanceLab.Simulation;

namespace BalanceLab.Reporting.Tests;

public sealed partial class ChartTests
{
    [Theory]
    [InlineData(0.0, 180)]
    [InlineData(0.5, 370)]
    [InlineData(1.0, 560)]
    [InlineData(-3.0, 180)] // poza skala: przyklejone do krawedzi, nie poza wykres
    [InlineData(7.0, 560)]
    public void Value_maps_onto_the_plot_width(double value, double x)
    {
        Assert.Equal(x, Svg.X(value, 0, 1), 9);
    }

    [Fact]
    public void Interval_chart_draws_a_dot_and_whisker_per_row()
    {
        IntervalRow[] rows = [new("a", 0.5, 0.4, 0.6, "a: tip"), new("b<x>", 0.9, 0.8, 1.0, "b", Hot: true)];

        var svg = Svg.Intervals(rows, 0, 1, 0.5, Html.Percent);

        Assert.Equal(2, Count(svg, "<circle class=\"dot\""));
        Assert.Equal(2, Count(svg, "<line class=\"whisker\""));
        Assert.Contains("<line class=\"whisker\" x1=\"332\" y1=\"22\" x2=\"408\" y2=\"22\"/>", svg, StringComparison.Ordinal);
        Assert.Contains("<g class=\"hot\"><title>b</title>", svg, StringComparison.Ordinal);
        Assert.Contains("<g><title>a: tip</title>", svg, StringComparison.Ordinal);
        Assert.Contains("b&lt;x&gt;", svg, StringComparison.Ordinal);
        Assert.Equal(1, Count(svg, "class=\"ref\"")); // linia 50% raz, choc to tez srodek skali
        Assert.Contains("viewBox=\"0 0 640 92\"", svg, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => Svg.Intervals(rows, 1, 1, 0.5, Html.Percent));
    }

    [Fact]
    public void Scatter_labels_only_named_points_and_draws_hot_ones_last()
    {
        ScatterPoint[] points = [new(0.5, 0.1, "goraca", "#1", Hot: true), new(0, -0.1, "zimna"), new(2, 9, "poza skala")];

        var svg = Svg.Scatter(points, "x", "y", -0.1, 0.1);

        Assert.Equal(3, Count(svg, "<circle"));
        Assert.Equal(1, Count(svg, ">#1</text>"));
        Assert.True(svg.IndexOf("zimna", StringComparison.Ordinal) < svg.IndexOf("goraca", StringComparison.Ordinal));
        Assert.Contains("cx=\"620\" cy=\"12\"", svg, StringComparison.Ordinal); // przyciete do rogu
        Assert.Contains("cx=\"56\" cy=\"300\"", svg, StringComparison.Ordinal);
        Assert.Contains("y1=\"156\" x2=\"620\" y2=\"156\"/>", svg, StringComparison.Ordinal); // zero w polowie
        Assert.Contains(">+10 pp</text>", svg, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => Svg.Scatter(points, "x", "y", 1, 1));
    }

    [Fact]
    public void Bars_grow_from_the_center_in_both_directions()
    {
        var svg = Svg.Bars([("mocny", 1600, "m"), ("slaby", 1400, "s"), ("sredni", 1500, "r")], 1500, v => $"{v}");

        Assert.Equal(1, Count(svg, "class=\"bar neg\""));
        Assert.Equal(3, Count(svg, "<rect"));
        Assert.Contains("width=\"2\"", svg, StringComparison.Ordinal); // rowny srodkowi wciaz widoczny
        Assert.Contains("x=\"370\"", svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.0, -3)]
    [InlineData(0.2, -2)]
    [InlineData(0.35, -1)]
    [InlineData(0.45, 0)]
    [InlineData(0.55, 0)]
    [InlineData(0.56, 1)]
    [InlineData(0.65, 1)]
    [InlineData(0.8, 2)]
    [InlineData(0.81, 3)]
    public void Matrix_buckets_are_symmetric_around_a_draw(double score, int bucket)
    {
        Assert.Equal(bucket, Svg.Bucket(score));
    }

    [Fact]
    public void Matrix_skips_cells_without_a_score()
    {
        string[] names = ["greedy:silnik", "greedy:punkty"];

        var svg = Svg.Matrix(names, (i, j) => i == j ? null : 0.9 - (0.8 * i), (i, j) => $"{i}-{j}");

        Assert.Equal(2, Count(svg, "<rect"));
        Assert.Contains("class=\"c3\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"c-3\"", svg, StringComparison.Ordinal);
        Assert.Contains("<title>0-1</title>", svg, StringComparison.Ordinal);
        Assert.Contains(">silnik</text>", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Column_names_drop_a_shared_prefix_only_when_they_stay_distinct()
    {
        Assert.Equal("silnik", Svg.Short(["greedy:silnik", "mcts:500"], 0));
        Assert.Equal("greedy:a", Svg.Short(["greedy:a", "random:a"], 0));
        Assert.Equal("bardzodlu…", Svg.Short(["bardzodluganazwa", "x"], 0));
        Assert.Equal("random", Svg.Short(["random", "mcts:10"], 0));
    }

    [Fact]
    public void Formats_are_culture_independent_and_honest_about_missing_data()
    {
        Assert.Equal("54.3%", Html.Percent(0.5432));
        Assert.Equal("brak danych", Html.WithInterval(Proportion.Of(0, 0)));
        Assert.Equal("50.0% (", Html.WithInterval(Proportion.Of(5, 10))[..7]);
        Assert.Equal("+20.0 pp (+5.2 - +33.4)", Html.Points(new Difference(0.6, 0.8, 0.2, 0.0524, 0.3339)));
        Assert.Equal("-1.0 pp (-3.0 - 0.0)", Html.Points(new Difference(0.5, 0.49, -0.01, -0.03, 0)));
    }

    [Fact]
    public void Table_escapes_headers_and_can_be_collapsed()
    {
        var open = Html.Table(["<a>"], [["x"]]);
        var collapsed = Html.Table(["a"], [["1"], ["2"]], collapsed: true);

        Assert.StartsWith("<table><thead><tr><th>&lt;a&gt;</th>", open, StringComparison.Ordinal);
        Assert.StartsWith("<details><summary>", collapsed, StringComparison.Ordinal);
        Assert.Equal(2, Count(collapsed, "<tr><td>"));
    }

    [Fact]
    public void Page_is_self_contained_and_has_a_dark_theme()
    {
        var page = new Html().Heading("T<1>", "lead").Section("S", "opis", "<svg></svg>", "<table></table>").Render("Tytul & co");

        Assert.StartsWith("<!doctype html><html lang=\"pl\">", page, StringComparison.Ordinal);
        Assert.Contains("<title>Tytul &amp; co</title>", page, StringComparison.Ordinal);
        Assert.Contains("<h1>T&lt;1&gt;</h1>", page, StringComparison.Ordinal);
        Assert.Contains("<section><p class=\"note\">opis</p><svg></svg><table></table></section>", page, StringComparison.Ordinal);
        Assert.Contains("prefers-color-scheme: dark", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page, StringComparison.Ordinal);
        Assert.DoesNotContain("http", page, StringComparison.Ordinal); // nic z sieci: dziala offline
    }

    private static int Count(string text, string fragment) => Regex.Count(text, Regex.Escape(fragment));
}
