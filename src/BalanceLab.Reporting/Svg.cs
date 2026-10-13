using System.Text;

namespace BalanceLab.Reporting;

/// <summary>Wiersz wykresu przedzialow: wartosc z 95% przedzialem; <see cref="Hot"/> wyroznia go kolorem.</summary>
internal sealed record IntervalRow(string Label, double Value, double Low, double High, string Tooltip, bool Hot = false);

/// <summary>Punkt wykresu rozrzutu; <see cref="Label"/> podpisany wprost tylko, gdy niepusty.</summary>
internal sealed record ScatterPoint(double X, double Y, string Tooltip, string? Label = null, bool Hot = false);

/// <summary>
/// Wykresy jako SVG skladany z tekstu. Kazdy znak danych ma &lt;title&gt; — przegladarka pokazuje go
/// po najechaniu myszka, bez JavaScriptu. Kolory przez klasy CSS, wiec dzialaja w obu trybach.
/// </summary>
internal static class Svg
{
    private const double Width = 640;
    private const double PlotLeft = 180;
    private const double PlotRight = 560; // miejsce na podpis wartosci po prawej
    private const double Row = 28;

    /// <summary>Pozycja wartosci na osi poziomej wykresu wierszowego.</summary>
    public static double X(double value, double min, double max) =>
        PlotLeft + ((Math.Clamp(value, min, max) - min) / (max - min) * (PlotRight - PlotLeft));

    /// <summary>Kropka z wasami przedzialu w kazdym wierszu i przerywana linia odniesienia (np. 1/n).</summary>
    public static string Intervals(IReadOnlyList<IntervalRow> rows, double min, double max, double reference, Func<double, string> format)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(max, min);
        var height = (rows.Count * Row) + 36;
        var svg = Open(height, "Wartosci z 95% przedzialami ufnosci");
        foreach (var tick in new[] { min, reference, max }.Distinct())
        {
            var x = X(tick, min, max);
            svg.Append(Html.Culture, $"<line class=\"{(tick == reference ? "ref" : "grid")}\" x1=\"{x:0.#}\" y1=\"8\" x2=\"{x:0.#}\" y2=\"{height - 24}\"/>");
            svg.Append(Html.Culture, $"<text x=\"{x:0.#}\" y=\"{height - 8}\" text-anchor=\"middle\">{Html.Encode(format(tick))}</text>");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var (r, y) = (rows[i], 8 + (i * Row) + (Row / 2));
            svg.Append(Html.Culture, $"<g{(r.Hot ? " class=\"hot\"" : "")}><title>{Html.Encode(r.Tooltip)}</title>");
            svg.Append(Html.Culture, $"<text class=\"label\" x=\"{PlotLeft - 10}\" y=\"{y + 4:0.#}\" text-anchor=\"end\">{Html.Encode(r.Label)}</text>");
            svg.Append(Html.Culture, $"<line class=\"whisker\" x1=\"{X(r.Low, min, max):0.#}\" y1=\"{y:0.#}\" x2=\"{X(r.High, min, max):0.#}\" y2=\"{y:0.#}\"/>");
            svg.Append(Html.Culture, $"<circle class=\"dot\" cx=\"{X(r.Value, min, max):0.#}\" cy=\"{y:0.#}\" r=\"6\"/>");
            svg.Append(Html.Culture, $"<text x=\"{PlotRight + 8}\" y=\"{y + 4:0.#}\">{Html.Encode(format(r.Value))}</text></g>");
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>Rozrzut: os pozioma 0-100%, pionowa od <paramref name="yMin"/> do <paramref name="yMax"/> z linia zera.</summary>
    public static string Scatter(IReadOnlyList<ScatterPoint> points, string xTitle, string yTitle, double yMin, double yMax)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(yMax, yMin);
        const double left = 56, right = 620, top = 12, bottom = 300, height = 344;
        double Px(double v) => left + (Math.Clamp(v, 0, 1) * (right - left));
        double Py(double v) => bottom - ((Math.Clamp(v, yMin, yMax) - yMin) / (yMax - yMin) * (bottom - top));

        var svg = Open(height, $"{yTitle} wzgledem {xTitle}");
        for (var i = 0; i <= 4; i++)
        {
            var (x, y) = (i / 4.0, yMin + (i / 4.0 * (yMax - yMin)));
            svg.Append(Html.Culture, $"<line class=\"grid\" x1=\"{left}\" y1=\"{Py(y):0.#}\" x2=\"{right}\" y2=\"{Py(y):0.#}\"/>");
            svg.Append(Html.Culture, $"<text x=\"{left - 6}\" y=\"{Py(y) + 4:0.#}\" text-anchor=\"end\">{y * 100:+0;-0;0} pp</text>");
            svg.Append(Html.Culture, $"<text x=\"{Px(x):0.#}\" y=\"{bottom + 18}\" text-anchor=\"middle\">{Html.Percent(x)}</text>");
        }

        svg.Append(Html.Culture, $"<line class=\"ref\" x1=\"{left}\" y1=\"{Py(0):0.#}\" x2=\"{right}\" y2=\"{Py(0):0.#}\"/>");
        svg.Append(Html.Culture, $"<text x=\"{(left + right) / 2}\" y=\"{height - 6}\" text-anchor=\"middle\">{Html.Encode(xTitle)}</text>");
        svg.Append(Html.Culture, $"<text x=\"14\" y=\"{(top + bottom) / 2}\" text-anchor=\"middle\" transform=\"rotate(-90 14 {(top + bottom) / 2})\">{Html.Encode(yTitle)}</text>");

        // Wyroznione na koncu: rysowane na wierzchu, nie przykryte zwyklymi punktami.
        foreach (var p in points.OrderBy(p => p.Hot))
        {
            svg.Append(Html.Culture, $"<g{(p.Hot ? " class=\"hot\"" : "")}><title>{Html.Encode(p.Tooltip)}</title>");
            svg.Append(Html.Culture, $"<circle class=\"dot\" cx=\"{Px(p.X):0.#}\" cy=\"{Py(p.Y):0.#}\" r=\"5\"/>");
            if (p.Label is { } label)
            {
                svg.Append(Html.Culture, $"<text class=\"label\" x=\"{Px(p.X) + 8:0.#}\" y=\"{Py(p.Y) + 4:0.#}\">{Html.Encode(label)}</text>");
            }

            svg.Append("</g>");
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>Slupki od wartosci srodkowej (np. Elo 1500): w prawo lepiej niz srednio, w lewo gorzej.</summary>
    public static string Bars(IReadOnlyList<(string Label, double Value, string Tooltip)> rows, double center, Func<double, string> format)
    {
        var spread = Math.Max(1, rows.Select(r => Math.Abs(r.Value - center)).DefaultIfEmpty(0).Max() * 1.1);
        var (min, max) = (center - spread, center + spread);
        var height = (rows.Count * Row) + 16;
        var svg = Open(height, "Ranking");
        var mid = X(center, min, max);
        svg.Append(Html.Culture, $"<line class=\"axis\" x1=\"{mid:0.#}\" y1=\"4\" x2=\"{mid:0.#}\" y2=\"{height - 4}\"/>");
        for (var i = 0; i < rows.Count; i++)
        {
            var (label, value, tooltip) = rows[i];
            var (y, x) = (8 + (i * Row), X(value, min, max));
            svg.Append(Html.Culture, $"<g><title>{Html.Encode(tooltip)}</title>");
            svg.Append(Html.Culture, $"<text class=\"label\" x=\"{PlotLeft - 10}\" y=\"{y + 16}\" text-anchor=\"end\">{Html.Encode(label)}</text>");
            svg.Append(Html.Culture, $"<rect class=\"bar{(value < center ? " neg" : "")}\" x=\"{Math.Min(x, mid):0.#}\" y=\"{y + 4}\" width=\"{Math.Max(2, Math.Abs(x - mid)):0.#}\" height=\"{Row - 10}\" rx=\"4\"/>");
            svg.Append(Html.Culture, $"<text x=\"{PlotRight + 8}\" y=\"{y + 16}\">{Html.Encode(format(value))}</text></g>");
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>
    /// Macierz: komorka (wiersz, kolumna) = sredni wynik wiersza przeciw kolumnie. Skala rozbiezna:
    /// niebieski = wiersz wygrywa, czerwony = przegrywa, szary = remis 50%.
    /// </summary>
    public static string Matrix(IReadOnlyList<string> names, Func<int, int, double?> score, Func<int, int, string> tooltip)
    {
        var size = Math.Min(80, (PlotRight - PlotLeft) / Math.Max(1, names.Count));
        var height = 40 + (names.Count * size);
        var svg = Open(height, "Macierz: kto bije kogo");
        for (var j = 0; j < names.Count; j++)
        {
            svg.Append(Html.Culture, $"<text class=\"label\" x=\"{PlotLeft + (j * size) + (size / 2):0.#}\" y=\"24\" text-anchor=\"middle\">{Html.Encode(Short(names, j))}</text>");
        }

        for (var i = 0; i < names.Count; i++)
        {
            var y = 32 + (i * size);
            svg.Append(Html.Culture, $"<text class=\"label\" x=\"{PlotLeft - 10}\" y=\"{y + (size / 2) + 4:0.#}\" text-anchor=\"end\">{Html.Encode(names[i])}</text>");
            for (var j = 0; j < names.Count; j++)
            {
                if (score(i, j) is not { } value)
                {
                    continue;
                }

                var (x, bucket) = (PlotLeft + (j * size), Bucket(value));
                svg.Append(Html.Culture, $"<g><title>{Html.Encode(tooltip(i, j))}</title>");
                svg.Append(Html.Culture, $"<rect class=\"c{bucket}\" x=\"{x + 1:0.#}\" y=\"{y + 1:0.#}\" width=\"{size - 2:0.#}\" height=\"{size - 2:0.#}\" rx=\"4\"/>");
                svg.Append(Html.Culture, $"<text class=\"cell\" x=\"{x + (size / 2):0.#}\" y=\"{y + (size / 2) + 4:0.#}\">{Html.Percent(value)}</text></g>");
            }
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>Stopien skali rozbieznej: -3 (prawie zawsze przegrywa) .. 0 (po rowno) .. 3 (prawie zawsze wygrywa).</summary>
    public static int Bucket(double score) => score switch
    {
        < 0.2 => -3,
        < 0.35 => -2,
        < 0.45 => -1,
        <= 0.55 => 0,
        <= 0.65 => 1,
        <= 0.8 => 2,
        _ => 3,
    };

    /// <summary>
    /// Naglowek kolumny: bez wspolnego przedrostka typu "greedy:" (jesli nazwy zostaja rozne), potem skrot do 10 znakow.
    /// </summary>
    internal static string Short(IReadOnlyList<string> names, int index)
    {
        var tails = names.Select(n => n[(n.IndexOf(':', StringComparison.Ordinal) + 1)..]).ToArray();
        var name = tails.Distinct().Count() == names.Count ? tails[index] : names[index];
        return name.Length <= 10 ? name : name[..9] + "…";
    }

    private static StringBuilder Open(double height, string label) =>
        new StringBuilder().Append(Html.Culture, $"<svg viewBox=\"0 0 {Width} {height:0.#}\" role=\"img\" aria-label=\"{Html.Encode(label)}\">");
}
