namespace FantomGamesCore.Interfaces
{
    public interface IReadOnlyFantomBoard
    {

        // TODO: GS3 

        /// <summary>
        /// Returns the number of neighbors for the specified Travel Mode and tile name.
        /// </summary>
        /// <param name="travelMode"></param>
        /// <param name="tileIndex"></param>
        /// <returns></returns>
        public int CountNeighbors(TravelModes travelMode, int tileIndex);

        /// <summary>
        /// Get the specified neighbor of the specified Travel Mode tile.
        /// </summary>
        /// <param name="travelMode"></param>
        /// <param name="tileIndex"></param>
        /// <param name="neighborIndex"></param>
        /// <returns></returns>
        public int GetNeighbor(TravelModes travelMode, int tileIndex, int neighborIndex);


        /// <summary>
        /// Checks if indexB tile of the specified Travel Mode is neighbor of tile indexA.
        /// </summary>
        /// <param name="travelMode"></param>
        /// <param name="indexA"></param>
        /// <param name="indexB"></param>
        /// <returns></returns>
        public bool IsNeighbor(TravelModes travelMode, int indexA, int indexB);


        /// <summary>
        /// Checks if indexB tile is neighbor of tile indexA for any Travel Mode.
        /// </summary>
        /// <returns></returns>
        public bool IsNeighbor(int indexA, int indexB);


        /// <summary>
        /// Returns all neighbors of the specified Travel Mode's tile.
        /// </summary>
        /// <param name="travelMode"></param>
        /// <param name="tileIndex"></param>
        /// <returns></returns>
        public int[] GetNeighbors(TravelModes travelMode, int tileIndex);

        /// <summary>
        /// Returns the total number of loaded tiles.
        /// </summary>
        public int TileCount { get; }

        /// <summary>
        /// Returns an enumerable of all tile positions
        /// </summary>
        public IEnumerable<int> AllTiles => Enumerable.Range(1, TileCount);
    }

}
