using FantomGamesCore.Interfaces;

namespace FantomGamesIntermediary
{

    /// <summary>
    /// A loading class for the Fantom playing board using a special text file format. Requires the path to the text file. Any lines begging with a double slash are considered comments and are skipped. Expects the first line to contain an integer number for the number of tiles to be loaded <br>
    /// The following lines for each give information about each of the tiles.
    /// </summary>
    public class IntermediaryFantomBoardLoader : IGameBoardLoader
    {

        private readonly TextReader reader;
        /// <summary>
        /// Create a new instance of the loader using the text given file.
        /// </summary>
        /// <param name="filePath">Path to the text file.</param>
        /// <exception cref="GameBoardLoadingException"></exception>
        public IntermediaryFantomBoardLoader(string filePath)
        {
            reader = new StreamReader(filePath);
            try
            {
                var firstLine = SkipCommented() ?? 
                    throw new GameBoardLoadingException("File not beginning with tile count.");

                TileCount = int.Parse(firstLine);
            } catch (Exception)
            {
                reader.Close();
                reader.Dispose();

                throw;
            }
        }

        private string? SkipCommented()
        {
            string? temp = reader.ReadLine();
            
            while (temp is not null && temp.Trim().StartsWith("//"))
            {
                temp = reader.ReadLine();
            }

            return temp;
        }

        
        public int TileCount { get; init; }

        public IEnumerable<GameBoardLoadingTile> LoadedTiles()
        {
            for (int currentTile = 1; currentTile <= TileCount; ++currentTile) {
                string? boardTile = SkipCommented()
                    ?? throw new GameBoardLoadingException("File does not have enough Tile entries.");

                var tokens = boardTile.Split(':', StringSplitOptions.TrimEntries);

                if (tokens.Length != 6)
                    throw new GameBoardLoadingException($"Line doesn't contain correct number of data: {boardTile}");

                // first token is tile name, has to match currentTile
                if (int.Parse(tokens[0]) != currentTile)
                    throw new GameBoardLoadingException($"Tile name not matching current tile {tokens[0]} != {currentTile}");

                // for now, unused
                int properties = int.Parse(tokens[1]);

                // each section can have 0-n tiles separated by commas
                int[][] neighbors = new int[4][];
                for(int i= 0; i< 4; ++i)
                {
                    var neighborTokens = tokens[2 + i].Split(',', 
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    neighbors[i] = new int[neighborTokens.Length];
                    
                    for(int j= 0; j< neighbors[i].Length; ++j)
                        neighbors[i][j] = int.Parse(neighborTokens[j]);
                }

                // close the file when at the end
                if (currentTile == TileCount)
                {
                    reader.Close();
                    reader.Dispose();
                }

                yield return new GameBoardLoadingTile()
                {
                    TileProperties = TileProperties.None, // temp. properties not important yet.
                    Mode1Neighbors = neighbors[0],
                    Mode2Neighbors = neighbors[1],
                    Mode3Neighbors = neighbors[2],
                    RiverNeighbors = neighbors[3]
                };
            }
        }
    }
}
