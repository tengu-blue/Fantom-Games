namespace FantomGamesIntermediary.Opponent.Interfaces
{
    internal interface ILegalMovesGenerator<MoveType, InputState>
        where MoveType : struct, IActorMove<MoveType>
        where InputState : struct, IActorState<InputState, MoveType>
    {

        public bool IsLegal(InputState state, MoveType move);
        public IEnumerable<MoveType> PossibleMoves(InputState state);
    }
}
