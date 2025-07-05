using FantomGamesCore;

namespace FantomGamesIntermediary.Opponent.Interfaces
{
    internal interface IStateEvaluator<TSelf, MoveType, OpponentMoveType>
        where MoveType : struct, IActorMove<MoveType>
        where OpponentMoveType : struct, IActorMove<OpponentMoveType>
    {
        /// <summary>
        /// Called to initially setup the Evaluator at the start of a new Game.
        /// </summary>
        /// <param name="initState"></param>
        public void Reset(FantomGameState initState);

        /// <summary>
        /// Called when the Opponent has played.
        /// </summary>
        public void OpponentPlay(OpponentMoveType opMove);

        /// <summary>
        /// Called when own Player has played.
        /// </summary>        
        public void OwnPlay(MoveType move);

        /// <summary>
        /// Called after Plays are called, before the Search is begun.
        /// </summary>
        public void PrepareForSearch();

        /// <summary>
        /// Make the state evaluator assume that at the given level the playing entity made the given Move and forget later assumed Moves.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="level"></param>
        public void AssumeMove(MoveType move, int level);

        /// <summary>
        /// Make the state evaluator forget that the playing entity made any Move at the given level or later.
        /// </summary>
        /// <param name="level"></param>
        public void ForgetMove(int level);

        
        /// <summary>
        /// Update this state evaluator so that it gives the same results as the passed evaluator.
        /// </summary>
        /// <param name="otherEvaluator"></param>
        public void UpdateFrom(TSelf otherEvaluator);

        /// <summary>
        /// Mostly for debugging the evaluation of each individual tile.
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns></returns>
        public float Evaluate(int tileIndex);

        /// <summary>
        /// Evaluate the passed Move.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public float Evaluate(MoveType move);
    }
}
