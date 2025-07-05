using FantomGamesCore.Interfaces;

namespace FantomGamesCore
{

    /// <summary>
    /// Will hold the representation of the current game board. 
    /// 
    /// Currently using an Adjacency list kind of representation. Only modified at the start. Fast retrieval of all neighbors. Slower to check if A and B are neighbors (but not by much as there's generally only a few neighbors in total).
    /// 
    /// If later need for faster lookups, can modify the representation.
    /// 
    /// Using indexing from 1, as it is used in the Board anyway and is convenient for this kind of representation. Indices have to be positive integers. There cannot be gaps, unless the tile count is adjusted to account for them.
    /// </summary>
    internal class FantomBoard : IReadOnlyFantomBoard
    {
        
        public int TileCount { get; private init; }
        
        // Contains modifiable properties for all tiles using TileProperties enum flags
        private TileProperties[] _tileProperties;

        // The four supported graphs overlaid on top of each other (sharing tiles)
        private AdjacencyGraph _mode1;
        private AdjacencyGraph _mode2;
        private AdjacencyGraph _mode3;
        private AdjacencyGraph _river;


        public FantomBoard(IGameBoardLoader loader) 
        {
            TileCount = loader.TileCount;

            // tile properties
            _tileProperties = new TileProperties[TileCount];

            // convenient extra index at the beginning
            // _mode0_tileNeighborsIndices[0] = 0; etc.
            var _mode1_tileNeighborsIndices = new int[1 + TileCount];
            var _mode2_tileNeighborsIndices = new int[1 + TileCount];
            var _mode3_tileNeighborsIndices = new int[1 + TileCount];
            var _river_tileNeighborsIndices = new int[1 + TileCount];


            // create a list for the neighbors, this means slower to create but that's fine, then convert to array.
            List<int> _mode1_tileNeighbors = [];
            List<int> _mode2_tileNeighbors = [];
            List<int> _mode3_tileNeighbors = [];
            List<int> _river_tileNeighbors = [];

            var index = 0;
            // iterate over all the tiles
            foreach (var tile in loader.LoadedTiles())
            {
                // check the indexes are within correct bounds

                _tileProperties[index] = tile.TileProperties;
                foreach (var neighbor in tile.Mode1Neighbors)
                    _mode1_tileNeighbors.Add(BoundedIndex(neighbor));

                foreach (var neighbor in tile.Mode2Neighbors)
                    _mode2_tileNeighbors.Add(BoundedIndex(neighbor));

                foreach (var neighbor in tile.Mode3Neighbors)
                    _mode3_tileNeighbors.Add(BoundedIndex(neighbor));

                foreach (var neighbor in tile.RiverNeighbors)
                    _river_tileNeighbors.Add(BoundedIndex(neighbor));

                ++index;

                _mode1_tileNeighborsIndices[index] = _mode1_tileNeighbors.Count;
                _mode2_tileNeighborsIndices[index] = _mode2_tileNeighbors.Count;
                _mode3_tileNeighborsIndices[index] = _mode3_tileNeighbors.Count;
                _river_tileNeighborsIndices[index] = _river_tileNeighbors.Count;

                // force stop in case of more tiles than necessary
                if (index == TileCount)
                    break;
            }

            // not enough tiles given -> error
            if (index != TileCount)
                throw new GameBoardLoadingException("Not enough tiles given!");


            _mode1 = new(_mode1_tileNeighborsIndices, [.. _mode1_tileNeighbors]);
            _mode2 = new(_mode2_tileNeighborsIndices, [.. _mode2_tileNeighbors]);
            _mode3 = new(_mode3_tileNeighborsIndices, [.. _mode3_tileNeighbors]);
            _river = new(_river_tileNeighborsIndices, [.. _river_tileNeighbors]);

            int BoundedIndex(int index)
            {
                if (index <= 0 || index > TileCount)
                    throw new GameBoardLoadingException($"Invalid tile index given. Has to be between 1 and {TileCount}. Got {index}.");
                return index;
            }
        }

        // Basic interface implementation -------------------------------------

        public int CountNeighbors(TravelModes travelMode, int tileIndex)
        {
            return travelMode switch
            {
                TravelModes.Mode1 => _mode1.CountNeighbors(tileIndex),
                TravelModes.Mode2 => _mode2.CountNeighbors(tileIndex),
                TravelModes.Mode3 => _mode3.CountNeighbors(tileIndex),
                TravelModes.River => _river.CountNeighbors(tileIndex),
                _ => -1,
            };
        }

        public int GetNeighbor(TravelModes travelMode, int tileIndex, int neighborIndex)
        {
            return travelMode switch
            {
                TravelModes.Mode1 => _mode1.GetNeighbor(tileIndex, neighborIndex),
                TravelModes.Mode2 => _mode2.GetNeighbor(tileIndex, neighborIndex),
                TravelModes.Mode3 => _mode3.GetNeighbor(tileIndex, neighborIndex),
                TravelModes.River => _river.GetNeighbor(tileIndex, neighborIndex),
                _ => -1,
            };
        }

        public int[] GetNeighbors(TravelModes travelMode, int tileIndex)
        {
            return travelMode switch
            {
                TravelModes.Mode1 => _mode1.GetNeighbors(tileIndex),
                TravelModes.Mode2 => _mode2.GetNeighbors(tileIndex),
                TravelModes.Mode3 => _mode3.GetNeighbors(tileIndex),
                TravelModes.River => _river.GetNeighbors(tileIndex),
                _ => [],
            };
        }

        public bool IsNeighbor(TravelModes travelMode, int indexA, int indexB)
        {
            return travelMode switch
            {
                TravelModes.Mode1 => _mode1.IsNeighbor(indexA, indexB),
                TravelModes.Mode2 => _mode2.IsNeighbor(indexA, indexB),
                TravelModes.Mode3 => _mode3.IsNeighbor(indexA, indexB),
                TravelModes.River => _river.IsNeighbor(indexA, indexB),
                _ => false,
            };
        }

        public bool IsNeighbor(int indexA, int indexB)
        {
            // Go over all, the first one that returns true -> return true; if none then return false
            foreach(TravelModes travelMode in Enum.GetValues<TravelModes>()) {
                if(IsNeighbor(travelMode, indexA, indexB)) 
                    return true;
            }

            return false;
        }
        
        public bool IsValidIndex(int index)
        {
            return index >= 1 && index <= TileCount;
        }
            
        /// <summary>
        /// 1-based indexing support
        /// </summary>
        internal struct AdjacencyGraph
        {
            // Contains indices into the _tileNeighborsLists array to tell the number of neighbors, and from which index to start reading them
            private int[] _tileNeighborsIndices;
            // Contains all neighbors for all tiles
            private int[] _tileNeighbors;

            public AdjacencyGraph(int[] tileNeighborsIndices, int[] tileNeighbors)
            {
                _tileNeighborsIndices = tileNeighborsIndices;
                _tileNeighbors = tileNeighbors;
            } 

            // TODO: interface for tile retrieval as well as neighbors etc.
            /// <summary>
            /// Check if the given index is valid for the graph nodes.
            /// </summary>
            /// <param name="index"></param>
            /// <exception cref="IndexOutOfRangeException"></exception>
            private void CheckBounds(int index)
            {
                if (index < 1 || index >= _tileNeighborsIndices.Length)
                    throw new IndexOutOfRangeException($"Tile indices start at 1. Got {index}");
            }

            /// <summary>
            /// Counts how many neighbors this graph node has.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public int CountNeighbors(int index)
            {
                CheckBounds(index);

                return _tileNeighborsIndices[index] - _tileNeighborsIndices[index - 1];
            }

            /// <summary>
            /// Potentially slower and more expensive than manual iteration over all neighbors as it has to create a new array for the result.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public int[] GetNeighbors(int index)
            {
                CheckBounds(index);

                int[] neighbors = new int[CountNeighbors(index)];
                Array.Copy(_tileNeighbors, _tileNeighborsIndices[index - 1], neighbors, 0, neighbors.Length);
                return neighbors;
            }

            /// <summary>
            /// For manual iteration over all valid neighbors of the given node.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="neighborIndex"></param>
            /// <returns></returns>
            /// <exception cref="IndexOutOfRangeException"></exception>
            public int GetNeighbor(int index, int neighborIndex)
            {               
                if (neighborIndex < 0 || neighborIndex >= CountNeighbors(index))
                    throw new IndexOutOfRangeException($"The neighbor index has to be between 0 and {CountNeighbors(index)}. Got {neighborIndex}.");

                return _tileNeighbors[_tileNeighborsIndices[index - 1] + neighborIndex];
            }

            /// <summary>
            /// Checks if indexB is one of indexA's neighbors. For directed graphs the order matters.
            /// </summary>
            /// <param name="indexA"></param>
            /// <param name="indexB"></param>
            /// <returns></returns>
            public bool IsNeighbor(int indexA, int indexB)
            {
                CheckBounds(indexA);
                // if B outside of bounds will just return false (probably)
                // CheckBounds(indexB);

                for (int i= _tileNeighborsIndices[indexA-1]; i< _tileNeighborsIndices[indexA]; ++i)
                {
                    if (_tileNeighbors[i] == indexB)
                        return true;
                }

                return false;
            }
        }

    }


    
}
