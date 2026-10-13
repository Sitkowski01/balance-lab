using System.Collections.Immutable;
using BalanceLab.Engine;

namespace BalanceLab.Testing;

/// <summary>Nim z elementami podanymi przez test — do sprawdzania, jak symulator obchodzi sie z ich numerami.</summary>
public sealed class TrackedNimGame(int itemCount, Func<NimState, int, IReadOnlyList<int>> itemsOf) : IGame<NimState, int>, IItemTracking<NimState>
{
    private readonly NimGame _nim = new();

    public string Name => _nim.Name;

    public int MinPlayers => _nim.MinPlayers;

    public int MaxPlayers => _nim.MaxPlayers;

    public int ItemCount => itemCount;

    public NimState Setup(GameSetup setup) => _nim.Setup(setup);

    public int PlayerCount(NimState state) => _nim.PlayerCount(state);

    public int CurrentPlayer(NimState state) => _nim.CurrentPlayer(state);

    public IReadOnlyList<int> LegalMoves(NimState state) => _nim.LegalMoves(state);

    public NimState Apply(NimState state, int move) => _nim.Apply(state, move);

    public bool IsTerminal(NimState state) => _nim.IsTerminal(state);

    public ImmutableArray<double> Scores(NimState state) => _nim.Scores(state);

    public ulong Hash(NimState state) => _nim.Hash(state);

    public NimState Determinize(NimState state, int observer, ulong seed) => state;

    public string ItemName(int item) => $"element {item}";

    public string ItemGroup(int item) => "wszystkie";

    public IReadOnlyList<int> ItemsOf(NimState state, int player) => itemsOf(state, player);
}
