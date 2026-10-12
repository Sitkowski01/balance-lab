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

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Column_outside_the_board_is_illegal(int column)
    {
        Assert.Throws<IllegalMoveException>(() => Game.Apply(Board.Play(), new ConnectFourMove(column)));
    }

    [Fact]
    public void Full_column_is_illegal_and_not_offered()
    {
        var state = Board.Play(2, 2, 2, 2, 2, 2);

        var ex = Assert.Throws<IllegalMoveException>(() => Game.Apply(state, new ConnectFourMove(2)));

        Assert.Equal("Nie mozna wrzucic do kolumny 2.", ex.Message);
        Assert.DoesNotContain(new ConnectFourMove(2), Game.LegalMoves(state));
        Assert.Equal(6, Game.LegalMoves(state).Count);
    }

    [Fact]
    public void Asking_for_legal_moves_allocates_nothing()
    {
        // MCTS pyta o ruchy przy kazdym kroku symulacji — wynik pochodzi z gotowej tablicy.
        var state = Board.Play(2, 2, 2, 2, 2, 2, 0);
        Game.LegalMoves(state);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var moves = Game.LegalMoves(state);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal([0, 1, 3, 4, 5, 6], moves.Select(m => m.Column));
        Assert.Same(moves, Game.LegalMoves(Board.Play(2, 2, 2, 2, 2, 2, 1)));
    }

    [Fact]
    public void No_move_after_the_game_ends()
    {
        var won = Board.Play(0, 1, 0, 1, 0, 1, 0);

        var ex = Assert.Throws<IllegalMoveException>(() => Game.Apply(won, new ConnectFourMove(5)));

        Assert.Equal("Partia sie skonczyla.", ex.Message);
    }

    [Fact]
    public void Scores_exist_only_at_the_end()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Game.Scores(Board.Play(3)));

        Assert.Equal("Partia jeszcze trwa.", ex.Message);
    }

    [Fact]
    public void Owner_reports_pieces_and_rejects_cells_outside_the_board()
    {
        var state = Board.Play(6, 6);

        Assert.Equal((0, 1, (int?)null), (state.Owner(6, 0), state.Owner(6, 1), state.Owner(6, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Owner(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Owner(7, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Owner(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Owner(0, 6));
    }

    [Fact]
    public void Hash_identifies_the_position_and_is_stable_between_versions()
    {
        var a = Board.Play(3, 4);
        var b = Board.Play(4, 3);

        Assert.Equal(Game.Hash(a), Game.Hash(Board.Play(3, 4)));
        Assert.NotEqual(Game.Hash(a), Game.Hash(b));
        Assert.Equal(StableHash.Empty.Add(1UL << 21).Add(1UL << 28).Value, Game.Hash(a));
        Assert.Equal("kolumna 3", new ConnectFourMove(3).ToString());
    }
}
