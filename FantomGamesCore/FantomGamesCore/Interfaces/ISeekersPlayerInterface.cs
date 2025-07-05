namespace FantomGamesCore.Interfaces
{
    /// <summary>
    /// Contains utilities the Seekers can use during the game.
    /// </summary>
    public interface ISeekersPlayerInterface
    {


        /// <summary>
        /// Returns the public Game State, which might not contain the Fantom's position.
        /// </summary>
        /// <returns></returns>
        public ReturnCode<FantomGameState> GetGameState();

        /// <summary>
        /// Moves the specified Seeker via the specified Ticket to the given position.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tileIndex_Where"></param>
        /// <param name="ticketKind"></param>
        /// <returns>True, if successfully used the Ticket. Else False with reason in Code.</returns>
        public ReturnCode<bool> Move(int seekerIndex, int tileIndex_Where, TicketKinds ticketKind);


        /// <summary>
        /// At the start of the game if Seekers aren't placed on the Playing Board, must be placed via this.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tileIndex_Where"></param>
        /// <returns>True, if successfully placed. Else False with reason in Code.</returns>
        public ReturnCode<bool> PlaceAt(int seekerIndex, int tileIndex_Where);


        /// <summary>
        /// If none of the yet unmoved Seekers or the next Seeker Cannot Move this has to be called, to continue with the game.
        /// </summary>
        /// <returns>Return Code Ok if truly cannot Move else int id of the tile that can be Moved to (first one that fits) with Return Code Fail.</returns>
        public ReturnCode<int> CannotMove();

        /// <summary>
        /// The current Seekers index, if it makes sense in current context.
        /// </summary>
        /// <returns>The index of the current Seeker.</returns>
        public ReturnCode<int> SeekerIndex();

        /// <summary>
        /// If Seekers are currently playing.
        /// </summary>
        /// <returns>True if Seekers can play.</returns>
        public bool IsSeekerTurn();

        /// <summary>
        /// Required to be called by the user after the Seekers have played to continue the game.
        /// </summary>
        /// <returns></returns>
        public bool ConfirmTurnOver();
    }
}
