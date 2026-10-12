using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty.Tests;

/// <summary>Uklada pozycje wprost: start z ziarna i podmiana tego, co test sprawdza.</summary>
internal static class Positions
{
    public static readonly KlejnotyGame Game = new();

    public static KlejnotyState Start(int players = 2, ulong seed = 1) => Game.Setup(new GameSetup(players, seed));

    public static KlejnotyState WithPlayer(this KlejnotyState state, int player, Func<PlayerBoard, PlayerBoard> change) =>
        state with { Players = state.Players.SetItem(player, change(state.Players[player])) };

    public static KlejnotyState WithCurrent(this KlejnotyState state, Func<PlayerBoard, PlayerBoard> change) =>
        state.WithPlayer(state.ToMove, change);

    /// <summary>Daje graczowi zetony z banku (zetony sie nie tworza, wiec bank je traci).</summary>
    public static KlejnotyState Give(this KlejnotyState state, int player, Gems tokens) =>
        state.WithPlayer(player, p => p with { Tokens = p.Tokens + tokens }) with { Bank = state.Bank - tokens };

    /// <summary>Pierwsza karta rynku danego poziomu.</summary>
    public static Card FirstOnMarket(this KlejnotyState state, int tier) => state.MarketCard(tier, 0)!;

    public static KlejnotyState Play(this KlejnotyState state, params KlejnotyMove[] moves) => moves.Aggregate(state, Game.Apply);
}
