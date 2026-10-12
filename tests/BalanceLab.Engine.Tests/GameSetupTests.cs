namespace BalanceLab.Engine.Tests;

public sealed class GameSetupTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Accepts_player_counts_within_bounds(int players)
    {
        new GameSetup(players, 1).EnsurePlayers(2, 4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rejects_player_counts_outside_bounds(int players)
    {
        var ex = Assert.Throws<ArgumentException>(() => new GameSetup(players, 1).EnsurePlayers(2, 4));

        Assert.Equal($"Gra obsluguje od 2 do 4 graczy, podano {players}.", ex.Message);
    }
}
