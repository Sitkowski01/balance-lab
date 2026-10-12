using BalanceLab.Engine;
using BalanceLab.Testing;

namespace BalanceLab.Bots.Tests;

public sealed class GreedyBotTests
{
    private static readonly NimGame Nim = new();

    [Fact]
    public void Picks_the_move_with_the_best_evaluation_for_itself()
    {
        // Ocena "mniej zetonow na stosie = lepiej" dla gracza 0, odwrotnie dla 1.
        var bot = new GreedyBot<NimState, int>(new PileEvaluator(), 1);

        Assert.Equal(3, bot.ChooseMove(Nim, new NimState(10, 2, 0, null)));
        Assert.Equal(1, bot.ChooseMove(Nim, new NimState(10, 2, 1, null)));
    }

    [Fact]
    public void Equal_evaluations_are_broken_at_random()
    {
        var moves = Enumerable.Range(0, 30)
            .Select(seed => new GreedyBot<NimState, int>(new ConstantEvaluator(1.0), (ulong)seed).ChooseMove(Nim, new NimState(10, 2, 0, null)))
            .ToHashSet();

        Assert.Equal([1, 2, 3], moves.Order());
    }

    [Fact]
    public void Only_the_best_moves_take_part_in_the_draw()
    {
        // Ruchy 2 i 3 rownie dobre, 1 gorszy: losowanie tylko miedzy dwoma najlepszymi.
        var moves = Enumerable.Range(0, 30)
            .Select(seed => new GreedyBot<NimState, int>(new CappedEvaluator(), (ulong)seed).ChooseMove(Nim, new NimState(10, 2, 0, null)))
            .ToHashSet();

        Assert.Equal([2, 3], moves.Order());
    }

    [Fact]
    public void Broken_evaluation_and_finished_games_are_reported()
    {
        var nan = new GreedyBot<NimState, int>(new ConstantEvaluator(double.NaN), 1);

        Assert.Contains("NaN", Assert.Throws<InvalidOperationException>(() => nan.ChooseMove(Nim, new NimState(5, 2, 0, null))).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => nan.ChooseMove(Nim, new NimState(0, 2, 0, 1)));
        Assert.Throws<ArgumentNullException>(() => nan.ChooseMove(null!, new NimState(5, 2, 0, null)));
        Assert.Throws<ArgumentNullException>(() => new GreedyBot<NimState, int>(null!, 1));
    }

    [Fact]
    public void Negative_evaluations_are_fine()
    {
        var bot = new GreedyBot<NimState, int>(new ConstantEvaluator(-5.0), 1);

        Assert.InRange(bot.ChooseMove(Nim, new NimState(5, 2, 0, null)), 1, 3);
    }

    private sealed class PileEvaluator : IEvaluator<NimState>
    {
        public double Evaluate(NimState state, int player) => player == 0 ? -state.Pile : state.Pile;
    }

    private sealed class ConstantEvaluator(double value) : IEvaluator<NimState>
    {
        public double Evaluate(NimState state, int player) => value;
    }

    private sealed class CappedEvaluator : IEvaluator<NimState>
    {
        public double Evaluate(NimState state, int player) => Math.Min(10 - state.Pile, 2);
    }
}
