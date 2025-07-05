namespace FantomGamesCore.Managers
{
    /// <summary>
    /// Holds all active Actors in the game. Makes sure two Actors don't share the same tile.
    /// 
    /// Knows about tiles, indexing starts at 1 as well.
    /// </summary>
    internal class ActorsManager
    {
        /*    For Actor Positions
         *    
         *    Positive values for tiles are used for actual tile locations. (0 is unused)
         *    Negative values represent special configurations, such as:
         *     -2 initial unspecified position for all Actors
         *     -1 when an Actor has been moved off the board by another Actor
         */


        public int ActorsCount { get; private set; }

        private int[] _actorPosition;
        private int[] _actorAt;

        public ActorsManager()
        {
            _actorPosition = [];
            _actorAt = [];
        }

        public ActorsManager(int actorCount, int tilesCount)
        {
            Reset(actorCount, tilesCount);
        }

        public void Reset(int actorCount, int tilesCount)
        {
            ActorsCount = actorCount;

            // each actor is places at a tile
            _actorPosition = new int[actorCount];
            for (int i = 0; i < actorCount; ++i)
                _actorPosition[i] = -2;

            // each tile can have at most one actor on it
            _actorAt = new int[1 + tilesCount];
            for (int i = 0; i < 1 + actorCount; ++i)
                _actorAt[i] = -2;
        }

        public void ResetAllActors()
        {
            for (int i = 0; i < _actorPosition.Length; ++i)
                _actorPosition[i] = -2;

            for (int i = 0; i < _actorAt.Length; ++i)
                _actorAt[i] = -1;
        }

        public bool IsActorAt(int actorIndex, int tileIndex)
        {
            return GetActorAt(tileIndex) == actorIndex;
        }

        public bool IsActorAt(int tileIndex)
        {
            return _actorAt[tileIndex] >= 0;
        }

        public int GetActorAt(int tileIndex)
        {
            return _actorAt[tileIndex];
        }

        public int GetTileOf(int actorIndex)
        {
            return _actorPosition[actorIndex];
        }

        public bool IsActorOnBoard(int actorIndex)
        {
            return GetTileOf(actorIndex) > 0;
        }

        public void RemoveActorFromBoard(int actorIndex)
        {
            // was on a valid board tile
            if (_actorPosition[actorIndex] > 0)
            {
                // remove him from that tile
                _actorAt[_actorPosition[actorIndex]] = -1;
                _actorPosition[actorIndex] = -1;
            }
        }

        public void Move(int actorIndex, int tileIndex)
        {
            // if there's another Actor on the final tile, move it away
            if (IsActorAt(tileIndex))
            {
                RemoveActorFromBoard(GetActorAt(tileIndex));
            }
            // remove this actor from the board temporarily
            RemoveActorFromBoard(actorIndex);

            // place at the new position that is definitely empty
            _actorPosition[actorIndex] = tileIndex;
            _actorAt[tileIndex] = actorIndex;
        }

    }
}
