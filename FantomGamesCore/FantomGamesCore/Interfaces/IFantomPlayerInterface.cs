namespace FantomGamesCore.Interfaces
{

    /// <summary>
    /// Contains utilities the Fantom can use during the game.
    /// </summary>
    public interface IFantomPlayerInterface
    {
        /// <summary>
        /// Returns the private Game State, which always contains the Fantom's position.
        /// </summary>
        /// <returns></returns>
        public ReturnCode<FantomGameState> GetGameState();

        /// <summary>
        /// If available, will use the Double Move Ticket. Afterwards two Moves are expected with other Ticket Kinds.
        /// </summary>
        /// <returns>True, if successfully used the Ticket. Else False with reason in Code.</returns>
        public ReturnCode<bool> UseDouble();

        /// <summary>
        /// Moves the Fantom via the specified Ticket to the given position.
        /// </summary>
        /// <param name="tileIndex_Where"></param>
        /// <param name="ticketKind"></param>
        /// <returns>True, if successfully used the Ticket. Else False with reason in Code.</returns>
        public ReturnCode<bool> Move(int tileIndex_Where, TicketKinds ticketKind);

        /// <summary>
        /// At the start of the game if Fantom isn't placed on the Playing Board, must be placed via this.
        /// </summary>
        /// <param name="tileIndex_Where"></param>
        /// <returns>True, if successfully placed. Else False with reason in Code.</returns>
        public ReturnCode<bool> PlaceAt(int tileIndex_Where);

        /// <summary>
        /// If the Fantom Cannot Move because no tiles or Tickets are available, this has to be called, to continue with the game.
        /// </summary>
        /// <returns>Return Code Ok if truly cannot Move else int id of the tile that can be Moved to (first one that fits) and Return Code Fail.</returns>
        public ReturnCode<int> CannotMove();

        /// <summary>
        /// If Fantom is currently playing.
        /// </summary>
        /// <returns>True if Fantom can play.</returns>
        public bool IsFantomTurn();


        /// <summary>
        /// Required to be called by the user after the Fantom has played to continue the game.
        /// </summary>
        /// <returns></returns>
        public bool ConfirmTurnOver();

    }
}
