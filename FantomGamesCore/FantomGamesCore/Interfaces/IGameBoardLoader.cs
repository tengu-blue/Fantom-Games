namespace FantomGamesCore.Interfaces
{
    /// <summary>
    /// An exception class representing an error during the loading of a Game Board.
    /// </summary>
    /// <remarks>
    /// Allows passing the message describing the error.
    /// </remarks>
    /// <param name="message">The error description</param>
    public class GameBoardLoadingException(string? message) : Exception(message)
    {
    }

    /// <summary>
    /// Used by the Core to load the playing board. Has to give a number of total tiles to be loaded and implement a method to retrieve those tiles via an Enumerable.
    /// </summary>
    public interface IGameBoardLoader
    {
        /// <summary>
        /// How many tiles the game board has in total. How many times the Enumerable will be called.
        /// </summary>
        public int TileCount { get; }

        /// <summary>
        /// All loaded tiles exposed to the GameBoard.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GameBoardLoadingTile> LoadedTiles();
    }

    /// <summary>
    /// A representation of a single tile of the Playing Board. Intermediary data type for easier Game Board loading disconnected from the actual representation.
    /// 
    /// </summary>
    public readonly record struct GameBoardLoadingTile
    {
        /// <summary>
        /// Properties for this tile used as flags.
        /// </summary>
        public required readonly TileProperties TileProperties { get; init; }
        /// <summary>
        /// All neighbors using the Mode 1 of travel for this tile.
        /// </summary>
        public required readonly int[] Mode1Neighbors { get; init; }
        /// <summary>
        /// All neighbors using the Mode 2 of travel for this tile.
        /// </summary>
        public required readonly int[] Mode2Neighbors { get; init; }
        /// <summary>
        /// All neighbors using the Mode 3 of travel for this tile.
        /// </summary>
        public required readonly int[] Mode3Neighbors { get; init; }
        /// <summary>
        /// All neighbors using the River for travel for this tile.
        /// </summary>
        public required readonly int[] RiverNeighbors { get; init; }
    }

    // F11 - Tile properties that can be modified at runtime
    
    /// <summary>
    /// Known tile properties for playing board tiles.
    /// </summary>
    [Flags]
    public enum TileProperties
    {
        None = 0,
        /// <summary>
        /// This tile is in district I.
        /// </summary>
        District_I = 1,
        /// <summary>
        /// This tile is in district II.
        /// </summary>
        District_II = 2,
        /// <summary>
        /// This tile is in district III.
        /// </summary>
        District_III = 4,
        /// <summary>
        /// This tile is in district IV.
        /// </summary>
        District_IV = 8,
        /// <summary>
        /// This tile is a Park.
        /// </summary>
        Park = 16,
        /// <summary>
        /// This tile is a Landmark.
        /// </summary>
        Landmark = 32,
        /// <summary>
        /// This tile is a River tile.
        /// </summary>
        River = 64,
        /// <summary>
        /// This tile has a Fantom hideout.
        /// </summary>
        Hideout = 128,
        /// <summary>
        /// This tile has a bank.
        /// </summary>
        Bank = 256,
        /// <summary>
        /// On this tile, the Fantom can commit a crime.
        /// </summary>
        Crime = 512,
        /// <summary>
        /// This tile is blocked and the Fantom cannot use it.
        /// </summary>
        Blocked = 1024
    }

    /// <summary>
    /// All available supported modes of travel. A subset of the Ticket types
    /// </summary>
    public enum TravelModes
    {
        /// <summary>
        /// The most common travel mode.
        /// </summary>
        Mode1,
        /// <summary>
        /// The second most common travel mode.
        /// </summary>
        Mode2,
        /// <summary>
        /// The rare travel mode.
        /// </summary>
        Mode3,
        /// <summary>
        /// The river mode of travel.
        /// </summary>
        River
    }

}
