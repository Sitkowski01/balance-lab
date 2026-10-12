using FsCheck.Xunit;

namespace BalanceLab.Engine.Tests;

public sealed class StableHashTests
{
    [Fact]
    public void Empty_hash_is_the_fnv_offset_basis()
    {
        Assert.Equal(0xcbf29ce484222325UL, StableHash.Empty.Value);
    }

    [Fact]
    public void Reference_bytes_match_the_published_fnv1a_vector()
    {
        // Sprawdzamy najpierw sama referencje z testow: FNV-1a 64 dla "a" z opisu algorytmu.
        Assert.Equal(0xaf63dc4c8601ec8cUL, ReferenceFnv1a([(byte)'a']));
    }

    [Property]
    public bool Add_hashes_the_little_endian_bytes_of_the_number(ulong value) =>
        StableHash.Empty.Add(value).Value == ReferenceFnv1a(BitConverter.GetBytes(value));

    [Property]
    public bool Adding_in_sequence_hashes_all_bytes_in_order(ulong a, ulong b) =>
        StableHash.Empty.Add(a).Add(b).Value
            == ReferenceFnv1a([.. BitConverter.GetBytes(a), .. BitConverter.GetBytes(b)]);

    [Fact]
    public void Default_value_is_the_empty_hash()
    {
        Assert.Equal(StableHash.Empty, default);
        Assert.Equal(StableHash.Empty.Add(7), new StableHash().Add(7));
    }

    [Property]
    public bool Sequence_is_its_length_followed_by_its_items(ulong[] items) =>
        StableHash.Empty.AddSequence(items) == items.Aggregate(StableHash.Empty.Add((ulong)items.Length), (h, x) => h.Add(x));

    [Fact]
    public void Sequences_with_the_same_items_but_different_split_differ()
    {
        var a = StableHash.Empty.AddSequence([1]).AddSequence([2, 3]);
        var b = StableHash.Empty.AddSequence([1, 2]).AddSequence([3]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Order_of_values_matters()
    {
        Assert.NotEqual(StableHash.Empty.Add(1).Add(2), StableHash.Empty.Add(2).Add(1));
    }

    [Fact]
    public void Value_is_stable_between_versions()
    {
        // Hashe trafia do dziennika partii — zmiana algorytmu uniewaznilaby stare zapisy.
        Assert.Equal(0x07cef17b81aac8a8UL, StableHash.Empty.Add(42).Add(2026).Value);
    }

    private static ulong ReferenceFnv1a(byte[] bytes)
    {
        var hash = 0xcbf29ce484222325UL;
        foreach (var b in bytes)
        {
            hash = unchecked((hash ^ b) * 0x100000001b3UL);
        }

        return hash;
    }
}
