using FantomGamesCore;

namespace FantomGamesSystemUtils
{
    /// <summary>
    /// Used to control the Fantom player.
    /// </summary>
    public interface IFantomCommander : IGeneralCommander
    {

        /// <summary>
        /// Gets the private Game State containing Fantom's position.
        /// </summary>
        /// <returns>Game State with Fantom's position.</returns>
        FantomGameState? GetPrivateGameState();

        /// <summary>
        /// Returns true if it is the Fantom's turn.
        /// </summary>
        /// <returns>True if it is the Fantom's turn.</returns>
        bool IsOwnTurn();

        /// <summary>
        /// If in Choosing game state will try to place Fantom at the specified position. Fails if that tile is not free to be placed at, or of not in the correct state or substate.
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns>True if placed successfully.</returns>
        bool PlaceAt(int tile);

        /// <summary>
        /// If in Fantom's Turn lets the Fantom do two Moves in the same turn. Fails if Fantom doesn't have that Ticket or not in the correct state.
        /// </summary>
        /// <returns>True if Double Ticket used successfully.</returns>
        bool UseDouble();

        /// <summary>
        /// If in Fantom's Turn state, Moves the Fantom to the specified tile using the given Ticket kind. Fails if that tile is not free or if Fantom doesn't have that Ticket or if not in the correct state.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="via"></param>
        /// <returns>True if moved successfully.</returns>
        bool Move(int tile, TicketKinds via);

        /// <summary>
        /// If in Fantom's Turn and cannot Move, this can be used to skip the Turn.
        /// </summary>
        /// <returns>A tile that the Fantom can be Moved to - if -, null otherwise.</returns>
        int? CannotMove();


        /// <summary>
        /// Call to end own Turn.
        /// </summary>
        /// <returns>True if the turn was ended.</returns>
        bool ConfirmTurnOver();
    }
}
