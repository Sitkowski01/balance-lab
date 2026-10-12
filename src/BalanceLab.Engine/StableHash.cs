namespace BalanceLab.Engine;

/// <summary>
/// FNV-1a (64 bity) po kolejnych liczbach. <see cref="HashCode"/> odpada: jest losowany
/// przy starcie procesu, a hash stanu musi byc ten sam w dzienniku, w CI i na innej maszynie.
/// </summary>
/// <remarks><c>default(StableHash)</c> jest rowny <see cref="Empty"/>.</remarks>
public readonly record struct StableHash
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    // Trzymamy wartosc XOR podstawa, zeby wyzerowana struktura byla poprawnym poczatkiem.
    private readonly ulong _shifted;

    private StableHash(ulong value) => _shifted = value ^ OffsetBasis;

    public static StableHash Empty => default;

    public ulong Value => _shifted ^ OffsetBasis;

    /// <summary>Dokleja liczbe bajt po bajcie (od najmlodszego), wiec kolejnosc ma znaczenie.</summary>
    public StableHash Add(ulong value)
    {
        var hash = Value;
        for (var i = 0; i < 8; i++)
        {
            hash = unchecked((hash ^ (byte)(value >> (i * 8))) * Prime);
        }

        return new StableHash(hash);
    }

    /// <summary>
    /// Dokleja dlugosc, a potem elementy. Bez dlugosci listy [1], [2, 3] i [1, 2], [3]
    /// dawalyby ten sam hash.
    /// </summary>
    public StableHash AddSequence(ReadOnlySpan<ulong> values)
    {
        var hash = Add((ulong)values.Length);
        foreach (var value in values)
        {
            hash = hash.Add(value);
        }

        return hash;
    }
}
