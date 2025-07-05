using FantomGamesCore;

namespace FantomGamesSystemUtils
{    
    /// <summary>
    /// Input interface for controlling the Game. Moving Pieces as well as asking for current state.
    /// </summary>
    public interface IFantomGamesCommander : IFantomCommander, ISeekersCommander
    {

        /// <summary>
        /// If the game is valid, will return the private level Game State (with Fantom position).
        /// </summary>
        /// <returns>Current private Game State or null if not valid.</returns>
        FantomGameState? GetGameState(); // private game state


        /// <summary>
        /// If in Choosing game state will try to place Fantom at the specified position. Fails if that tile is not free to be placed at, or of not in the correct state or substate.
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns>True if placed successfully.</returns>
        bool PlaceFantomAt(int tileIndex);

        /// <summary>
        /// If in Fantom's Turn state, Moves the Fantom to the specified tile using the given Ticket kind. Fails if that tile is not free or if Fantom doesn't have that Ticket or if not in the correct state.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="via"></param>
        /// <returns>True if moved successfully.</returns>
        bool MoveFantom(int tile, TicketKinds via);

        /// <summary>
        /// If in Fantom's Turn and cannot Move, this can be used to skip the Turn.
        /// </summary>
        /// <returns>A tile that the Fantom can be Moved to - if -, null otherwise.</returns>
        int? CannotMoveFantom();

        /// <summary>
        /// If in Choosing game state will try to place the specified Seeker at the specified position. Fails if that tile is not free to be placed at, or of not in the correct state.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tileIndex"></param>
        /// <returns>True if placed successfully.</returns>
        bool PlaceSeekerAt(int seekerIndex, int tileIndex);

        /// <summary>
        /// If in Seekers' Turn state, Moves the specified Seeker to the specified tile using the given Ticket kind. Fails if that tile is not free or if Seeker doesn't have that Ticket or if not in the correct state or substate.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="via"></param>
        /// <returns>True if moved successfully.</returns>
        bool MoveSeeker(int seekerIndex, int tile, TicketKinds via);

        /// <summary>
        /// If in Seekers' Turn and cannot Move, this can be used to skip the Turn.
        /// </summary>
        /// <returns>A tile that the current (or any one of the unmoved) Seeker(s) can be Moved to - if -, null otherwise.</returns>
        int? CannotMoveSeeker();
    }
}
