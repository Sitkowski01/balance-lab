using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Games.Klejnoty;

public static partial class KlejnotyRules
{
    /// <summary>
    /// Obserwator nie wie, w jakiej kolejnosci leza talie ani jakie karty inni wzieli w ciemno —
    /// zna tylko ich poziom (rewers). Te karty kazdego poziomu sa tasowane razem i rozkladane
    /// z powrotem na te same miejsca.
    /// </summary>
    public static KlejnotyState Determinize(KlejnotyState state, int observer, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfNegative(observer);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(observer, state.Players.Length);

        var rng = Pcg32.Create(seed);
        var decks = state.Decks.ToBuilder();
        var players = state.Players.ToBuilder();
        for (var tier = 1; tier <= Cards.Tiers; tier++)
        {
            // Sortujemy przed tasowaniem: inaczej wynik zalezalby od prawdziwej kolejnosci talii,
            // a wiec od czegos, czego obserwator nie zna.
            var hidden = HiddenReservations(state, observer, tier);
            var unseen = state.Decks[tier - 1].Concat(hidden.Select(h => state.Players[h.Player].Reserved[h.Index].Card)).Order().ToArray();
            (var shuffled, rng) = rng.Shuffle(unseen);

            var deckSize = state.Decks[tier - 1].Length;
            decks[tier - 1] = shuffled[..deckSize];
            for (var i = 0; i < hidden.Count; i++)
            {
                var (player, index) = hidden[i];
                var board = players[player];
                players[player] = board with { Reserved = board.Reserved.SetItem(index, new Reservation(shuffled[deckSize + i], true)) };
            }
        }

        return state with { Decks = decks.MoveToImmutable(), Players = players.MoveToImmutable() };
    }

    /// <summary>Zakryte rezerwacje innych graczy z danego poziomu, w stalej kolejnosci (gracz, miejsce).</summary>
    /// <remarks>Lista, nie <c>yield</c>: iterator z kompilatora czyta Environment.CurrentManagedThreadId, a test architektury slusznie tego zabrania.</remarks>
    private static List<(int Player, int Index)> HiddenReservations(KlejnotyState state, int observer, int tier)
    {
        var hidden = new List<(int Player, int Index)>();
        for (var player = 0; player < state.Players.Length; player++)
        {
            var reserved = state.Players[player].Reserved;
            for (var index = 0; index < reserved.Length; index++)
            {
                if (player != observer && reserved[index].Hidden && state.Variant.AllCards[reserved[index].Card].Tier == tier)
                {
                    hidden.Add((player, index));
                }
            }
        }

        return hidden;
    }
}
