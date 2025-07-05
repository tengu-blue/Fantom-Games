using FantomGamesCore;

namespace FantomGamesSystemUtils
{
    /// <summary>
    /// Used to control the Seeker player.
    /// </summary>
    public interface ISeekersCommander : IGeneralCommander
    {
        /// <summary>
        /// Gets the public Game State which might not have Fantom's position.
        /// </summary>
        /// <returns>Game State possibly without Fantom's position.</returns>
        FantomGameState? GetPublicGameState();

        /// <summary>
        /// Returns true if it is the Seekers' turn.
        /// </summary>
        /// <returns>True if it is the Seekers' turn.</returns>
        bool IsOwnTurn();

        /// <summary>
        /// If in Choosing game state will try to place the specified Seeker at the specified position. Fails if that tile is not free to be placed at, or of not in the correct state.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tileIndex"></param>
        /// <returns>True if placed successfully.</returns>
        bool PlaceAt(int seekerIndex, int tile);

        /// <summary>
        /// If in Seekers' Turn state, Moves the specified Seeker to the specified tile using the given Ticket kind. Fails if that tile is not free or if Seeker doesn't have that Ticket or if not in the correct state or substate.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="via"></param>
        /// <returns>True if moved successfully.</returns>
        bool Move(int seekerIndex, int tile, TicketKinds via);

        /// <summary>
        /// If in Seekers' Turn and cannot Move, this can be used to skip the Turn
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tile"></param>
        /// <param name="via"></param>
        /// <returns>TileIndex of a possible tile that the first unmoved seeker can move to or null if successful.</returns>
        int? CannotMove();

        /// <summary>
        /// Call to end own Turn.
        /// </summary>
        /// <returns>True if the turn was ended.</returns>
        bool ConfirmTurnOver();

    }
}
