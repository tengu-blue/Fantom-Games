using FantomGamesCore;

namespace FantomGamesSystemUtils
{
    /// <summary>
    /// Output interface for the Game System. For updating game state via eg. visuals.
    /// </summary>
    public interface IFantomGamesFacade
    {

        // Util ---------------
        /// <summary>
        /// Called when an error occurs when doing some command.
        /// </summary>
        /// <param name="message">The description of the error.</param>
        void ErrorMessage(string message);

        // Game state ---------
        /// <summary>
        /// Called when the game starts.
        /// </summary>
        /// <param name="gameState">The initial game state.</param>
        void GameStarted(FantomGameState gameState);
        /// <summary>
        /// Called when the game is reset with current valid settings.
        /// </summary>
        void GameReset();
        /// <summary>
        /// Called when the game is restarted with new valid settings.
        /// </summary>
        /// <param name="newSettings">The new valid settings.</param>
        void GameRestarted(FantomGameSettings newSettings);
        /// <summary>
        /// Called when the game is over.
        /// </summary>
        /// <param name="gameResult">The game's results.</param>
        void GameFinished(FantomGameResult gameResult);

        /// <summary>
        /// Called when the Fantom's turn begins.
        /// </summary>
        /// <param name="fantomMove"></param>
        void FantomTurnBegin(uint fantomMove);
        /// <summary>
        /// Called when the Fantom's turn ends.
        /// </summary>
        void FantomTurnEnd();

        /// <summary>
        /// Called when a Seeker's turn begins.
        /// </summary>
        /// <param name="seekerIndex">The index of the currently playing seeker if ordering ís enabled.</param>
        void SeekerTurnBegin(int seekerIndex);
        /// <summary>
        /// Called when a Seeker's turn ends.
        /// </summary>
        /// <param name="seekerIndex">The index of the seeker that finished playing if ordering is enabled.</param>
        void SeekerTurnEnd(int seekerIndex);

        /// <summary>
        /// Called when the round is over.
        /// </summary>
        /// <param name="round">The number of the ended round.</param>
        void RoundOver(uint round);

        // Actors movement etc. ---
        /// <summary>
        /// Called when the Fantom's figure is placed manually.
        /// </summary>
        /// <param name="tile">The location of the placed Fantom.</param>
        void FantomPlacedAt(int tile);
        /// <summary>
        /// Called when a Seeker's figure is placed manually.
        /// </summary>
        /// <param name="seekerIndex">The index of the placed Seeker.</param>
        /// <param name="tile">The location of the placed Seeker.</param>
        void SeekerPlacedAt(int seekerIndex, int tile);

        /// <summary>
        /// Called when the Fantom uses a Double Ticket.
        /// </summary>
        void FantomUsedDouble();
        /// <summary>
        /// Called when the Fantom uses a movement Ticket.
        /// </summary>
        /// <param name="ticketKind">The ticket used by the Fantom.</param>
        void FantomUsedTicket(TicketKinds ticketKind);

        /// <summary>
        /// Called when the Fantom moves.
        /// </summary>
        /// <param name="tile">The location where the Fantom moves to.</param>
        /// <param name="via">The used ticket to do the move.</param>
        void FantomMovedTo(int tile, TicketKinds via);

        /// <summary>
        /// Called when the Fantom moves to a tile, for history keeping in the Tickets table.
        /// </summary>
        /// <param name="tile"></param>
        void FantomHistoryMoveTo(int tile);

        /// <summary>
        /// Called when the Fantom skips their turn.
        /// </summary>
        void FantomCouldNotBeMoved();
        /// <summary>
        /// Called when the Fantom is revealed at a location.
        /// </summary>
        /// <param name="tile">The location where he revealed.</param>
        void FantomRevealedAt(int tile);
        /// <summary>
        /// Called when a Seeker moves to a location.
        /// </summary>
        /// <param name="seekerIndex">The index of the moved Seeker.</param>
        /// <param name="tile">The location the Seeker moved to.</param>
        /// <param name="via">The ticket used by that Seeker.</param>
        void SeekerMovedTo(int seekerIndex, int tile, TicketKinds via);

        /// <summary>
        /// Called when a Seeker passed their turn.
        /// </summary>
        /// <param name="seekerIndex">The index of that Seeker.</param>
        void SeekerCouldNotBeMoved(int seekerIndex);
        /// <summary>
        /// Called when all Seekers skip their turn.
        /// </summary>
        void SeekersCouldNotBeMoved();
           
    }
}
