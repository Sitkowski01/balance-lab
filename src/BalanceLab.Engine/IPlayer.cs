namespace BalanceLab.Engine;

/// <summary>Ktos, kto wybiera ruchy: bot albo (pozniej) czlowiek przez siec.</summary>
/// <remarks>
/// Instancja nalezy do jednej partii: moze trzymac stan (generator, drzewo MCTS) i nie jest
/// bezpieczna watkowo. Symulator tworzy graczy od nowa dla kazdej partii, z ziarnami
/// wyprowadzonymi z ziarna partii — dopiero wtedy to samo ziarno daje te sama partie.
/// </remarks>
public interface IPlayer<TState, TMove>
    where TState : notnull
    where TMove : notnull
{
    TMove ChooseMove(IGame<TState, TMove> game, TState state);
}
