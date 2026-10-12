using BalanceLab.Engine;
using static BalanceLab.Games.Klejnoty.Tests.Positions;

namespace BalanceLab.Games.Klejnoty.Tests;

public sealed class SetupAndTokenTests
{
    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    [InlineData(4, 7)]
    public void Setup_deals_the_market_patrons_and_a_bank_sized_for_the_table(int players, int perColor)
    {
        var state = Start(players, 9);

        Assert.Equal(Gems.Of(perColor, perColor, perColor, perColor, perColor, 5), state.Bank);
        Assert.Equal(players, state.Players.Length);
        Assert.All(state.Players, p => Assert.Equal(PlayerBoard.Empty, p));
        Assert.Equal([36, 26, 16], state.Decks.Select(d => d.Length));
        Assert.Equal(Enumerable.Range(0, 90), state.Market.Concat(state.Decks.SelectMany(d => d)).Order());
        Assert.All(Enumerable.Range(1, 3), tier => Assert.All(Enumerable.Range(0, 4), slot => Assert.Equal(tier, state.MarketCard(tier, slot)!.Tier)));
        Assert.Equal(players + 1, state.Patrons.Distinct().Count());
        Assert.Equal(state.Patrons.Order(), state.Patrons.ToArray());
        Assert.Equal((0, 0, false, false), (state.ToMove, state.Passes, state.FinalRound, state.Over));
    }

    [Fact]
    public void Market_comes_from_the_top_of_each_shuffled_deck()
    {
        var (deck, _) = Pcg32.Create(9).Shuffle(Cards.OfTier(1));

        var state = Start(2, 9);

        Assert.Equal(deck[^1], state.MarketCard(1, 0)!.Id);
        Assert.Equal(deck[^4], state.MarketCard(1, 3)!.Id);
        Assert.Equal(deck[..^4].ToArray(), state.Decks[0].ToArray());
    }

    [Fact]
    public void Seed_decides_the_deal()
    {
        Assert.Equal(Game.Hash(Start(3, 5)), Game.Hash(Start(3, 5)));
        Assert.NotEqual(Game.Hash(Start(3, 5)), Game.Hash(Start(3, 6)));
        Assert.Equal(3, Game.PlayerCount(Start(3, 5)));
        Assert.Throws<ArgumentException>(() => Start(5));
        Assert.Throws<ArgumentException>(() => Start(1));
    }

    [Fact]
    public void Opening_moves_are_takes_and_reservations()
    {
        var moves = Game.LegalMoves(Start());

        // 10 trojek kolorow, 5 par (bank po 4), 12 kart z rynku, 3 talie; na nic nie stac.
        Assert.Equal(10, moves.Count(m => m.Kind == MoveKind.TakeDifferent));
        Assert.Equal(5, moves.Count(m => m.Kind == MoveKind.TakeTwo));
        Assert.Equal(12, moves.Count(m => m.Kind == MoveKind.Reserve));
        Assert.Equal([1, 2, 3], moves.Where(m => m.Kind == MoveKind.ReserveFromDeck).Select(m => m.Tier));
        Assert.Equal(30, moves.Count);
        Assert.All(moves, m => Game.Apply(Start(), m));
    }

    [Fact]
    public void Taking_three_colors_moves_them_from_the_bank_and_passes_the_turn()
    {
        var take = Gems.Of(white: 1, green: 1, black: 1);

        var state = Start().Play(KlejnotyMove.Take(take));

        Assert.Equal(take, state.Players[0].Tokens);
        Assert.Equal(Gems.Of(3, 4, 3, 4, 3, 5), state.Bank);
        Assert.Equal(1, Game.CurrentPlayer(state));
    }

    [Fact]
    public void Fewer_than_three_colors_only_when_the_bank_has_fewer()
    {
        var twoColors = Start() with { Bank = Gems.Of(red: 2, black: 1, gold: 5) };

        var takes = Game.LegalMoves(twoColors).Where(m => m.Kind == MoveKind.TakeDifferent);

        Assert.Equal([Gems.Of(red: 1, black: 1)], takes.Select(m => m.Gems));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(twoColors, KlejnotyMove.Take(Gems.Of(red: 1))));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Take(Gems.Of(red: 1, black: 1))));
        Assert.DoesNotContain(Game.LegalMoves(Start() with { Bank = Gems.Of(gold: 5) }), m => m.Kind == MoveKind.TakeDifferent);
    }

    public static TheoryData<Gems> InvalidTakes =>
    [
        Gems.Of(white: 2, blue: 1),
        Gems.Of(white: 1, blue: 1, gold: 1),
        Gems.Of(white: 1, blue: 1, green: 1, red: 1),
        Gems.None,
    ];

    [Theory]
    [MemberData(nameof(InvalidTakes))]
    public void Invalid_takes_are_rejected(Gems take)
    {
        var ex = Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Take(take)));

        Assert.StartsWith($"Nie mozna wziac {take} z banku", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Take_from_an_empty_pile_is_rejected()
    {
        var state = Start() with { Bank = Gems.Of(blue: 4, green: 4, red: 4, black: 4) };

        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Take(Gems.Of(white: 1, blue: 1, green: 1))));
    }

    [Fact]
    public void Two_of_a_color_need_four_in_the_bank()
    {
        var state = Start() with { Bank = Gems.Of(3, 4, 4, 4, 4, 5) };

        Assert.DoesNotContain(KlejnotyMove.TakeTwo(Gem.White), Game.LegalMoves(state));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.TakeTwo(Gem.White)));
        Assert.Equal(Gems.Of(blue: 2), Game.Apply(state, KlejnotyMove.TakeTwo(Gem.Blue)).Players[0].Tokens);
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, new KlejnotyMove(MoveKind.TakeTwo, Gems.Of(blue: 1, green: 1), -1, 0)));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(state, new KlejnotyMove(MoveKind.TakeTwo, Gems.Of(gold: 2), -1, 0)));
    }

    [Fact]
    public void Over_ten_tokens_the_same_player_returns_the_excess()
    {
        var state = Start().Give(0, Gems.Of(white: 3, blue: 3, green: 3)).Play(KlejnotyMove.Take(Gems.Of(red: 1, black: 1, white: 1)));

        Assert.Equal(0, state.ToMove);
        Assert.Equal(12, state.Players[0].Tokens.Total);
        Assert.Equal(
            [Gem.White, Gem.Blue, Gem.Green, Gem.Red, Gem.Black],
            Game.LegalMoves(state).Select(m => Enum.GetValues<Gem>().Single(g => m.Gems[g] == 1)));
        Assert.Equal("Najpierw oddaj zetony ponad limit 10.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(state, KlejnotyMove.Take(Gems.Of(blue: 1, green: 1, red: 1)))).Message);

        var once = state.Play(KlejnotyMove.Return(Gem.White));
        var done = once.Play(KlejnotyMove.Return(Gem.Red));

        Assert.Equal((0, 11), (once.ToMove, once.Players[0].Tokens.Total));
        Assert.Equal((1, Gems.Of(white: 3, blue: 3, green: 3, black: 1)), (done.ToMove, done.Players[0].Tokens));
        Assert.Equal(Start().Bank - done.Players[0].Tokens, done.Bank);
    }

    [Fact]
    public void Returning_is_only_for_the_excess_and_only_what_you_have()
    {
        var over = Start().Give(0, Gems.Of(white: 4, blue: 4, green: 3));

        Assert.Equal("Oddaje sie tylko zetony ponad limit 10.",
            Assert.Throws<IllegalMoveException>(() => Game.Apply(Start(), KlejnotyMove.Return(Gem.White))).Message);
        Assert.Throws<IllegalMoveException>(() => Game.Apply(over, KlejnotyMove.Return(Gem.Red)));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(over, new KlejnotyMove(MoveKind.Return, Gems.Of(white: 2), -1, 0)));
        Assert.Throws<IllegalMoveException>(() => Game.Apply(over, new KlejnotyMove(MoveKind.Return, Gems.None, -1, 0)));
        Assert.Equal([Gem.White, Gem.Blue, Gem.Green, Gem.Gold],
            Game.LegalMoves(over.Give(0, Gems.Of(gold: 1))).Select(m => Enum.GetValues<Gem>().Single(g => m.Gems[g] == 1)));
    }
}
