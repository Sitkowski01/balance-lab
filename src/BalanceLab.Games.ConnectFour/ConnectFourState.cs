namespace BalanceLab.Games.ConnectFour;

/// <summary>Wrzucenie zetonu do kolumny 0..6.</summary>
public readonly record struct ConnectFourMove(int Column)
{
    public override string ToString() => $"kolumna {Column}";
}

/// <summary>
/// Plansza 7x6 jako dwa bitboardy (po jednym na gracza). Kolumna zajmuje 7 bitow:
/// 6 pol od dolu i pusty bit straznika, dzieki ktoremu przesuniecia przy szukaniu
/// czworek nie przeskakuja z gory jednej kolumny na dol nastepnej.
/// </summary>
public readonly record struct ConnectFourState
{
    public const int Columns = 7;
    public const int Rows = 6;
    internal const int ColumnHeight = Rows + 1;

    internal ConnectFourState(ulong first, ulong second, int movesPlayed, int? winner)
    {
        First = first;
        Second = second;
        MovesPlayed = movesPlayed;
        Winner = winner;
    }

    /// <summary>Pola gracza 0 (zaczyna).</summary>
    public ulong First { get; }

    /// <summary>Pola gracza 1.</summary>
    public ulong Second { get; }

    public int MovesPlayed { get; }

    /// <summary>Zwyciezca albo null (partia trwa lub remis).</summary>
    public int? Winner { get; }

    /// <summary>Wlasciciel pola albo null, gdy puste. Wiersz 0 to dol planszy.</summary>
    public int? Owner(int column, int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);

        var bit = 1UL << ((column * ColumnHeight) + row);
        return (First & bit) != 0 ? 0 : (Second & bit) != 0 ? 1 : null;
    }

    /// <summary>Plansza tekstem, gora na poczatku: X gracz 0, O gracz 1, kropka puste.</summary>
    public override string ToString()
    {
        var self = this;
        return string.Join('\n', Enumerable.Range(0, Rows).Reverse().Select(row =>
            new string([.. Enumerable.Range(0, Columns).Select(column => self.Owner(column, row) switch
            {
                0 => 'X',
                1 => 'O',
                _ => '.',
            })])));
    }
}
