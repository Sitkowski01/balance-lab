namespace BalanceLab.Games.ConnectFour.Tests;

/// <summary>Pozycja z obrazka: wiersze od gory, X gracz 0, O gracz 1, kropka puste.</summary>
internal static class Board
{
    public static ConnectFourState Parse(params string[] rowsFromTop)
    {
        ulong first = 0, second = 0;
        var pieces = 0;
        for (var i = 0; i < rowsFromTop.Length; i++)
        {
            var row = rowsFromTop.Length - 1 - i;
            for (var column = 0; column < ConnectFourState.Columns; column++)
            {
                var bit = 1UL << ((column * ConnectFourState.ColumnHeight) + row);
                switch (rowsFromTop[i][column])
                {
                    case 'X': first |= bit; pieces++; break;
                    case 'O': second |= bit; pieces++; break;
                }
            }
        }

        return new ConnectFourState(first, second, pieces, null);
    }

    /// <summary>Partia rozegrana podanymi kolumnami od pustej planszy.</summary>
    public static ConnectFourState Play(params int[] columns)
    {
        var game = new ConnectFourGame();
        var state = game.Setup(new Engine.GameSetup(2, 0));
        foreach (var column in columns)
        {
            state = game.Apply(state, new ConnectFourMove(column));
        }

        return state;
    }
}
