namespace FantomGamesIntermediary.Opponent.Interfaces
{
    internal interface IActorMove<TSelf>
        where TSelf : IActorMove<TSelf>
    {
        static abstract bool operator==(TSelf a, TSelf b);
        static abstract bool operator!=(TSelf a, TSelf b);

    }
}
