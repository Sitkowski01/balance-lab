namespace BalanceLab.Engine;

/// <summary>Ruch spoza <see cref="IGame{TState, TMove}.LegalMoves"/> albo ruch po koncu partii.</summary>
public sealed class IllegalMoveException : Exception
{
    public IllegalMoveException()
    {
    }

    public IllegalMoveException(string message)
        : base(message)
    {
    }

    public IllegalMoveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
