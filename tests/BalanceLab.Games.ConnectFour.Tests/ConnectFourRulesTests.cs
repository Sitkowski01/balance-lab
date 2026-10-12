using BalanceLab.Engine;

namespace BalanceLab.Games.ConnectFour.Tests;

public sealed class ConnectFourRulesTests
{
    private static readonly ConnectFourGame Game = new();

    [Fact]
    public void Starts_with_an_empty_board_and_the_first_player()
    {
        var state = Game.Setup(new GameSetup(2, 123));

        Assert.Equal(0UL, state.First | state.Second);
        Assert.Equal(0, Game.CurrentPlayer(state));
        Assert.Equal(Enumerable.Range(0, 7), Game.LegalMoves(state).Select(m => m.Column));
        Assert.False(Game.IsTerminal(state));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Is_a_game_for_exactly_two(int players)
    {
        Assert.Throws<ArgumentException>(() => Game.Setup(new GameSetup(players, 1)));
        Assert.Equal((2, 2), (Game.MinPlayers, Game.MaxPlayers));
        Assert.Equal("ConnectFour", Game.Name);
    }

    [Fact]
    public void Pieces_fall_to_the_lowest_free_cell_and_players_alternate()
    {
        var state = Board.Play(3, 3, 3, 4);

        Assert.Equal(
            ".......\n.......\n.......\n...X...\n...O...\n...XO..",
            state.ToString());
        Assert.Equal(4, state.MovesPlayed);
        Assert.Equal(0, Game.CurrentPlayer(state));
    }

    public static TheoryData<string, int, string[]> Wins => new()
    {
        { "pionowo", 0, [".......", ".......", ".......", "X......", "X......", "XOOO..."] },
        { "poziomo", 3, [".......", ".......", ".......", ".......", "OOO....", "XXX...."] },
        { "ukos /", 3, [".......", ".......", ".......", "..XO...", ".XOX...", "XOOX..O"] },
        { "ukos \\", 3, [".......", ".......", ".......", "...OX..", "...XOX.", "O..XOOX"] },
    };

    [Theory]
    [MemberData(nameof(Wins))]
    public void Four_in_a_line_wins(string direction, int column, string[] board)
    {
        var before = Board.Parse(board);
        Assert.Null(before.Winner);

        var after = Game.Apply(before, new ConnectFourMove(column));

        Assert.True(after.Winner == 0, $"Brak wygranej: {direction}");
        Assert.True(Game.IsTerminal(after));
        Assert.Equal([1.0, 0.0], Game.Scores(after).ToArray());
        Assert.Empty(Game.LegalMoves(after));
    }

    [Fact]
    public void Second_player_can_win_too()
    {
        var state = Board.Play(0, 1, 0, 1, 2, 1, 6, 1);

        Assert.Equal(1, state.Winner);
        Assert.Equal([0.0, 1.0], Game.Scores(state).ToArray());
    }

    [Fact]
    public void Line_does_not_wrap_from_the_top_of_one_column_to_the_bottom_of_the_next()
    {
        // Bity: X na gorze kolumny 0 i na dole kolumny 1 leza obok siebie w liczbie;
        // bez pustego bitu straznika przesuniecie widzialoby tu pionowa czworke.
        var state = Board.Parse("X......", "X......", "O......", "O......", "X......", "OX....O");

        var after = Game.Apply(state, new ConnectFourMove(1));

        Assert.Null(after.Winner);
    }

    [Fact]
    public void Full_board_without_four_is_a_draw()
    {
        var state = Board.Parse(".OXXOOX", "XXOOXXO", "OOXXOOX", "XXOOXXO", "OOXXOOX", "XXOOXXO");
        Assert.Equal(1, Game.CurrentPlayer(state));
        Assert.Equal([new ConnectFourMove(0)], Game.LegalMoves(state));

        var after = Game.Apply(state, new ConnectFourMove(0));

        Assert.Null(after.Winner);
        Assert.True(Game.IsTerminal(after));
        Assert.Equal([0.5, 0.5], Game.Scores(after).ToArray());
    }

}
