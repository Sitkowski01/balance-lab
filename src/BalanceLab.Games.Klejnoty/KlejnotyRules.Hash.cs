using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

public static partial class KlejnotyRules
{
    public static ulong Hash(KlejnotyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var hash = StableHash.Empty.Add(state.Bank.Packed).Add((ulong)state.ToMove).Add((ulong)state.Passes)
            .Add(state.FinalRound ? 1UL : 0).Add(state.Over ? 1UL : 0);
        hash = AddAll(AddAll(hash, state.Market), state.Patrons);
        foreach (var deck in state.Decks)
        {
            hash = AddAll(hash, deck);
        }

        foreach (var p in state.Players)
        {
            hash = AddAll(AddAll(hash.Add(p.Tokens.Packed).Add(p.Bonuses.Packed).Add((ulong)p.Points), p.Bought), p.Patrons);
            hash = AddAll(hash, [.. p.Reserved.Select(r => (r.Card * 2) + (r.Hidden ? 1 : 0))]);
        }

        return hash.Value;
    }

    private static StableHash AddAll(StableHash hash, ImmutableArray<int> values)
    {
        hash = hash.Add((ulong)values.Length);
        foreach (var value in values)
        {
            hash = hash.Add(unchecked((ulong)value));
        }

        return hash;
    }
}
