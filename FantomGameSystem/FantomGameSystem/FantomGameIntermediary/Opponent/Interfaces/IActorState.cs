using FantomGamesCore;

namespace FantomGamesIntermediary.Opponent.Interfaces
{
    internal interface IActorState<TSelf, SelfMove> 
        where TSelf : struct, IActorState<TSelf, SelfMove>
        where SelfMove : struct, IActorMove<SelfMove>
    {
        static abstract TSelf FromState(FantomGameState state);
        static abstract TSelf operator +(TSelf state, SelfMove move);
    }
}
