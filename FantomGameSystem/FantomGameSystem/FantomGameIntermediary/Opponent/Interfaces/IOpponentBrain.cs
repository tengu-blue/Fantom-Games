using FantomGamesSystemUtils;

namespace FantomGamesIntermediary.Opponent.Interfaces
{
    public interface IOpponent<TSelf> : IFantomGamesFacade
        where TSelf : IOpponent<TSelf>
    {
        /// <summary>
        /// Pause the Opponent, stop using resources.
        /// </summary>
        public void Sleep();

        /// <summary>
        /// Start the Opponent, expect a restart / reset / start a new from the Game
        /// </summary>
        public void Wake();

        /// <summary>
        /// End the Opponent, free all resources.
        /// </summary>
        public void Terminate();
    }


    internal interface IOpponentBrain<MoveType>
        where MoveType : struct, IActorMove<MoveType>
    {

        /// <summary>
        /// For making the currently best considered Move
        /// </summary>
        void MakeBestMove();

        /// <summary>
        /// Check if can update with proposed new best value Move
        /// </summary>
        /// <param name="proposedValue"></param>
        /// <returns></returns>
        bool CanSetWith(float newWorst, float proposedValue);

        /// <summary>
        /// A way to check if proposed sequence might be used to update the current best with.
        /// </summary>
        /// <param name="proposedSequence"></param>
        /// <returns></returns>
        bool CanSetWith(MovesSequence<MoveType> proposedSequence);

        /// <summary>
        /// For updating the values in the known sequence (or overriding it)
        /// </summary>
        /// <param name="updatedWorstValue">The worst value of the updated sequence</param>
        /// <param name="updatedBestValue">The best value of the updated sequence</param>
        /// <param name="proposedSequence">The updated sequence</param>
        void UpdateMoveSequence(float updatedWorstValue, float updatedBestValue, MovesSequence<MoveType> proposedSequence);

        /// <summary>
        /// For updating the sequence only if its value is greater than the value within, thread-safely.
        /// </summary>
        /// <param name="newWorstValue">The worst value of the new sequence</param>
        /// <param name="newBestValue">The best value of the new sequence</param>
        /// <param name="proposedSequence">The new proposed sequence</param>
        void SetBestMoveSequence(float newWorstValue, float newBestValue, MovesSequence<MoveType> proposedSequence);

        /// <summary>
        /// For retrieving the stored sequence.
        /// </summary>
        /// <returns></returns>
        MovesSequence<MoveType> GetMovesSequence();

        /// <summary>
        /// For checking if the next Move has been made yet.
        /// </summary>
        /// <returns>True if has made the move returned by <see cref="GetNextMove"/></returns>
        bool HasMoved { get; }

        /// <summary>
        /// Called before a new Search begins for confirming the last Move in the sequence was made.
        /// </summary>
        void NewSearchSetup();

        /// <summary>
        /// For Resetting the brain between Games.
        /// </summary>
        void Reset();
    }
}
