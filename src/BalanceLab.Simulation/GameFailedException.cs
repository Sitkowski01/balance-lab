namespace BalanceLab.Simulation;

/// <summary>Partia turnieju przerwana bledem; numer i ziarno wystarcza, zeby ja odtworzyc.</summary>
public sealed class GameFailedException : Exception
{
    public GameFailedException()
    {
    }

    public GameFailedException(string message)
        : base(message)
    {
    }

    public GameFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GameFailedException(int game, ulong seed, Exception innerException)
        : base($"Partia {game} (ziarno {seed}) przerwana: {innerException?.Message}", innerException)
    {
        Game = game;
        Seed = seed;
    }

    public int Game { get; }

    public ulong Seed { get; }
}
