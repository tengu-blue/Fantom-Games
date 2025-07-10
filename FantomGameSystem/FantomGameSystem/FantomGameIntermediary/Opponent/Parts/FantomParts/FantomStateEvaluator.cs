using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesIntermediary.Opponent.Parts.SeekerParts;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent.Parts.FantomParts
{

    /*
     * Unlike the Seeker counterpart, there isn't much sense in trying to predict how the situation will change
     * when the Seekers have played. Mainly there's not much to do when presuming a Move is going to be made, 
     * apart from changing the current level and evaluation slightly.
     * 
     * Instead we need to keep track of possible Seeker Positions, to avoid them as best as possible. Additionally
     * it makes sense to keep track of own possible positions, ie. where they can assume the Fantom is.
     * 
     * Another important part to consider is when Fantom's going to be revealed. This is going to be key when 
     * Assuming Moves. Preferring Moves that keep the Fog high, but also saving Black and Double Tickets when they
     * are most likely to be useful.
     * 
     */

    internal class FantomStateEvaluator(
        int _seekersCount,
        int _detectivesCount,
        IReadOnlyFantomBoard _fantomBoard,
        IEnumerable<int> _startingPositions,
        uint maxRounds,
        IEnumerable<uint> revealingMoves)
        : IStateEvaluator<FantomStateEvaluator, FantomMove, SeekersMove>
    {
        const int FUTURE_STEPS = 8;
        const float MAX_EVAL_VALUE = 100f;

        const float CURRENT_COST = 100f;
        const float FUTURE_COST_FALLOFF = 0.5f;

        bool[] _revealingMoves = MakeRevealing(revealingMoves);
        uint _lastMove = 0;

        uint _lastReveal = 0;
        uint _nextReveal = 0;
        uint _roundNumber = 0;
        uint _roundsRemaining = 0;

        private static bool[] MakeRevealing(IEnumerable<uint> revealingMoves)
        {
            var max = revealingMoves.Max() + 1;
            bool[] revealingMovesB = new bool[max];
            foreach (var move in revealingMoves)
                revealingMovesB[move] = true;
            return revealingMovesB;
        }

        private uint NextRevealing(uint from)
        {
            for (uint i = from + 1; i < _revealingMoves.Length; ++i)
            {
                if (_revealingMoves[i])
                    return i;
            }

            // useful to be bigger than max
            return maxRounds + 1;
        }

        // FUTURE:STEPS + 1 because 0 is now; so actually have that many futures

        // Seeker positions and their Tickets to predict where they might be in the future
        // Again _future level 0 is now, have to keep Seekers separate
        int[,,] _futurePossiblePositionsSeekers = new int[_seekersCount,
                                                   FUTURE_STEPS + 1,
                                                   _fantomBoard.TileCount + 1];
        // How many positions total, are there for any given seeker in the future
        // So index 0 is always 1, because the Seeker is only at that one position
        float[,] _futurePossiblePositionsSeekersCountsInverse = new float[_seekersCount, FUTURE_STEPS + 1];

        int[,] _detectiveTickets = new int[_detectivesCount, 3];

        int[] _tempPositions1 = new int[_fantomBoard.TileCount + 1];      
        float[] _tempPositionValues1 = new float[_fantomBoard.TileCount + 1];
        float[] _tempPositionValues2 = new float[_fantomBoard.TileCount + 1];

        // TODO: 
        // Own position and where the Seekers might think Fantom is and will be in the future
        // When assuming Moves, remember to mask out if going to be Revealed (?) - we know it will happen, that might
        // be enough,
        // we care about the number of tiles more than the actual tiles
        int[] _fantomTickets = new int[FantomGameSettings.TICKET_KINDS_COUNT];

        int[] _doubleTickets = new int[FUTURE_STEPS + 1];

        // int _fantomPosition not needed ?
        // level 0 is where they might know he is right now, the others are for predictions
        // maybe by mode again ?
        int[,] _futurePossibleFantomPositions = new int[FUTURE_STEPS + 1,
                                                  _fantomBoard.TileCount + 1];

        // For each Ticket kind - 3 modes and black, keep track of how many tiles will be reachable 
        // if using that ticket from the specified position
        int[,,] _seekerReachableFromRevealCount = new int[_seekersCount, 4, _fantomBoard.TileCount + 1];
        float[,,] _seekerCatchPotentialByMode = new float[FUTURE_STEPS + 1, 4, _fantomBoard.TileCount + 1];

        // Cached evaluation for fast retrieval for multitudes of States
        float[] _tilesEvaluation = new float[_fantomBoard.TileCount + 1];
        int TileCount { get => _fantomBoard.TileCount; }

        int _currentLevel = 0;

        float[,] _cornered = new float[FUTURE_STEPS + 1, _fantomBoard.TileCount + 1];

        int[] _reachableTilesByMode = new int[3];

        // The exact OWN position, for future with Assuming steps
        int[] _fantomCurrentPos = new int[FUTURE_STEPS + 1];

        
        public void Reset(FantomGameState initState)
        {
            Array.Clear(_futurePossiblePositionsSeekers);
            Array.Clear(_futurePossibleFantomPositions);
            Array.Clear(_seekerReachableFromRevealCount);
            Array.Clear(_detectiveTickets);
            Array.Clear(_fantomTickets);
            Array.Clear(_tilesEvaluation);
            Array.Clear(_doubleTickets);

            _roundsRemaining = maxRounds;

            _currentLevel = 0;
            _roundNumber = 0;
            _lastMove = 0;

            _lastReveal = 0;
            _nextReveal = NextRevealing(0);

            initState.CopyFantomTicketsTo(_fantomTickets);
            _doubleTickets[0] = _fantomTickets[(int)TicketKinds.Double];
            initState.CopyDetectiveTicketsTo(_detectiveTickets);

            // Mark possible starting positions for the Fantom
            foreach (var pos in _startingPositions)
            {
                _futurePossibleFantomPositions[0, pos] = 1;
            }

            // Mark starting position for each seeker
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                _futurePossiblePositionsSeekers[seekerIndex, 0, initState.GetSeekerPosition(seekerIndex)] = 1;
            }

            // No need to clear, since 'only' (and lower) current level is valid
            _fantomCurrentPos[0] = initState.FantomPosition;

        }

        public void UpdateFrom(FantomStateEvaluator otherEvaluator)
        {
            // TODO
            throw new NotImplementedException();
        }



        public float Evaluate(int tileIndex)
        {
            return _tilesEvaluation[tileIndex];
        }

        public float Evaluate(FantomMove move)
        {

            // !!!! BIG BUG, when reveal don't go next to a Seeker that can reach WTF?

            float value = 0f;

            // tiles evaluation now contains likeliness of Seeker in the future
            // we wish to avoid the Seekers as much as possible, so likely Seeker positions 

            // for double moves, we only care about the final destination
            var position = move.IsDouble ? move.GetDestination(1) : move.GetDestination(0);
            Debug.Assert(position != null);
            value = _tilesEvaluation[position.Value];

            // Black Ticket Moves slightly less than normal Ticket Moves (Done later)
            // Double Moves slightly less than non-double Moves (TODO: based on how cornered)

            value *= (1000 + _doubleTickets[_currentLevel]) / 1002f;

            // make double moves less valuable to only use them when necessary to save them till later in game
            if (move.IsDouble)
            {
                // value = value * 0.5f;                
                // NOTE: this return seems to better for some reason
                //return value * _cornered[_currentLevel, _fantomCurrentPos[_currentLevel]];
                value *= _cornered[_currentLevel, _fantomCurrentPos[_currentLevel]];
            }

            // Seeker positions only certain for the first Level
            if (_currentLevel == 0)
            {
                // Next Move will reveal
                if ((move.IsDouble && move.WasRevealing(1)) || (!move.IsDouble && move.WasRevealing(0)))
                {
                    if (IsAnySeekerReachable(1, position.Value))
                    {
                        // Will definitely get caught
                        return 0;
                    }
                }
            }
            
            // From - assume revealed at td, moving to position via tc
            var fd = !move.IsDouble ? _fantomCurrentPos[_currentLevel] : move.GetDestination(0);
            var tc = !move.IsDouble ? move.GetTicketAsInt(0) : move.GetTicketAsInt(1);
            Debug.Assert(fd is not null);

            
            // the probability of being caught only applies if that destination is actually reachable
            if (IsAnySeekerReachable(_currentLevel + 1, position.Value))
                value *= (1 - _seekerCatchPotentialByMode[_currentLevel, tc, fd.Value]);


            // Technically the Black Ticket mult. depends on current 'effect' so seeing how many new positions can be
            // reached taking what Seekers 'can' know into account. So only accurate for _currentLevel == 0
            // But since recalculating on every turn anyway, this should be fine.
            if (move.GetTicketUsed(0) == TicketKinds.Black)
            {
                value *= DetermineBlackTicketMultiplier(_fantomCurrentPos[_currentLevel], _lastMove + (uint)_currentLevel);
            }
            if (move.IsDouble && move.GetTicketUsed(1) == TicketKinds.Black)
                value *= DetermineBlackTicketMultiplier(move.GetDestination(0).Value, _lastMove + (uint)_currentLevel + 1);


            // TODO: if game will end after this move and can move such that no Seeker can reach in next round (but maybe in future ones could - those will not happen) then definitely win - MAX_EVAL
            return Math.Max(0, value);
        }


        // TODO: figure out what to change, most likely FOG and Tickets - FantomMasks probably
        public void AssumeMove(FantomMove move, int level)
        {
            // only if move.Moved // not too likely that won'
            _currentLevel = level + 1;

            _doubleTickets[_currentLevel] = _doubleTickets[_currentLevel - 1];
            if (move.IsDouble)
            {
                _doubleTickets[_currentLevel]--;
            }

            var d = move.IsDouble ? move.GetDestination(1) : move.GetDestination(0);
            Debug.Assert(d != null);
            _fantomCurrentPos[_currentLevel] = d.Value;
            // _roundsRemaining--;

            // TODO: Black Tickets budget for predictions !!! 

            // RecalculateCornered();
            RecalculateEvaluation();
        }

        public void ForgetMove(int level)
        {
            _currentLevel = level;
            // _roundsRemaining++;
            RecalculateEvaluation();
        }



        public void OpponentPlay(SeekersMove opMove)
        {
            // reset current positions
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    _futurePossiblePositionsSeekers[seekerIndex, 0, tileIndex] = 0;
                }

            }

            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                // Mark the positions as occupied 
                _futurePossiblePositionsSeekers[seekerIndex, 0, opMove.GetDestination(seekerIndex)] = 1;

                // actually made a Move, update Tickets                  
                if (opMove.Moved(seekerIndex) && seekerIndex < _detectivesCount)
                {
                    _detectiveTickets[seekerIndex, (int)opMove.GetTicketUsed(seekerIndex)]--;
                }

                // TODO: Mark Fantom FOG positions as 0 as well
            }
        }

        public void OwnPlay(FantomMove move)
        {

            _roundNumber++;
            _lastMove++;
            if (move.IsDouble)
            {
                _doubleTickets[0]--;
                _lastMove++;
            }
            if (_roundNumber == _nextReveal)
            {
                _lastReveal = _nextReveal;
                _nextReveal = NextRevealing(_roundNumber);
            }

            // See where Seekers might think Fantom is with max information
            for (int i = 0; i < move.MovesCount; ++i)
            {
                if (move.Moved(i))
                {
                    RecalculateOwnPossiblePositions(move.GetTicketUsed(i), move.WasRevealing(i) ? move.GetDestination(i) : null);
                }
            }


            var d = !move.IsDouble ? move.GetDestination(0) : move.GetDestination(1);
            Debug.Assert(d is not null);
            _fantomCurrentPos[0] = d.Value;

        }

        public void PrepareForSearch()
        {
            // Clear 'masks' - assumptions
            _currentLevel = 0;

            RecalculateFuturePositions();
            RecalculateSeekerCatchPotentials();



            RecalculateCornered();

            RecalculateReachableCounts();
            
        }


        // ----------------------------------------------------------------------------------------------

        static readonly TravelModes[] TRAVEL_MODES_AS_ARRAY = {
                                                                TravelModes.Mode1,
                                                                TravelModes.Mode2,
                                                                TravelModes.Mode3
                                                        };

        private static readonly TravelModes[][] TRAVEL_MODES_BY_TICKET = [
            [TravelModes.Mode1],
            [TravelModes.Mode2],
            [TravelModes.Mode3],
            [TravelModes.Mode1, TravelModes.Mode2, TravelModes.Mode3, TravelModes.River],
            [], // Double Ticket not possible
            [TravelModes.River],
            []  // Balloon not implemented
        ];


        // Call separately for double moves
        private float DetermineBlackTicketMultiplier(int fromTile, uint moveCount)
        {            
            float blackTicketMultiplier;

            // offset by future prediction
            var x = _roundNumber + _currentLevel + 1;

            var lastReveal = _lastReveal;
            var nextReveal = _nextReveal;

            if (moveCount > nextReveal)
            {
                lastReveal = nextReveal;
                nextReveal = NextRevealing(moveCount);
            }

            // Determine a value - a multiplier for Black Ticket use.
            // There are situations where using a Black Ticket makes no sense, so those ones the multiplier should be close
            // to 0.
            // Similarly there are situations where it makes absolute sense to use it, so those should be close to 1
            // Occasionally if there are only a few turns remaining and hasn't used them yet, then the multiplier might even be 
            // more than 1, to really make sure those get used.

            // To begin with, have x tickets
            var blackTicketBudget = _fantomTickets[(int)TicketKinds.Black];
            // if cannot afford, strange to be asking...
            if (blackTicketBudget == 0)
                return 0;

            // the desire to use Tickets roughly equally over the span of the game
            var c1 = (float)x / maxRounds * blackTicketBudget;

            // more likely after reveal, less closer to the next reveal (zero if going to be revealed)
            var c2 = Math.Min(1.4f, Math.Max(0, (float)1.4f * (nextReveal - x) / (nextReveal - lastReveal)));

            // not needed
            // var c3 = 1f/possibleFantomTiles[_currentLevel]
            var c3 = 1f;

            // roughly, if has more Black Tickets than rounds / moves remaining (doubles a bit more problematic)
            // then definitely just use them
            var c4 = Math.Min(1.1f, Math.Max(0f, 1.1f * (x - maxRounds + blackTicketBudget)));

            blackTicketMultiplier = Math.Min(1.1f, Math.Max(0f, c1 * c2 * c3));

            // First move makes no sense mostly 
            if (moveCount == 0)
                blackTicketMultiplier = 0;


            // At a River tile
            if (_fantomBoard.CountNeighbors(TravelModes.River, fromTile) > 0)
            {
                blackTicketMultiplier = 0.9f;
            }
            else
            {
                // Knowing possible Fantom positions (From Seekers POV) what is the effect of using Black instead of any 
                // other specific one
                var blackReach = _reachableTilesByMode[0] + _reachableTilesByMode[1] + _reachableTilesByMode[2];
                if (_reachableTilesByMode[0] == blackReach || _reachableTilesByMode[1] == blackReach || _reachableTilesByMode[2] == blackReach)
                    blackTicketMultiplier = 0;

                // Will be revealed so no sense using the Black
                if (moveCount == nextReveal)
                    blackTicketMultiplier = 0;
            }

            // NOTE: this could be removed, but keep just in case
            // Cannot afford some other mode of transport            
            if (_fantomTickets[0] <= 0 || _fantomTickets[1] <= 0 || _fantomTickets[2] <= 0)
            {
                blackTicketMultiplier = 1f;
            }

            // c4 wins over all
            blackTicketMultiplier = Math.Max(blackTicketMultiplier, c4);


            return blackTicketMultiplier;
        }

        // When making a Move what do the possible positions look like to the Seekers
        private void RecalculateOwnPossiblePositions(TicketKinds ticket, int? revealedAt)
        {

            if (revealedAt is not null)
            {
                // forget where was
                Array.Clear(_futurePossibleFantomPositions, 0, TileCount + 1);
                _futurePossibleFantomPositions[0, revealedAt.Value] = 1;
            }
            else
            {

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    if (IsFantomReachable(0, tileIndex))
                    {
                        foreach (var mode in TRAVEL_MODES_BY_TICKET[(int)ticket])
                        {
                            var neighborCount = _fantomBoard.CountNeighbors(mode, tileIndex);
                            for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                            {
                                _tempPositions1[_fantomBoard.GetNeighbor(mode, tileIndex, neighborIndex)] = 1;
                            }
                        }
                    }
                }

                // Copy temp to actual positions now
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    _futurePossibleFantomPositions[0, tileIndex] = _tempPositions1[tileIndex];
                }
            }

            // Remove Seeker positions, cannot be at them
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    if (IsSeekerReachable(seekerIndex, 0, tileIndex))
                        _futurePossibleFantomPositions[0, tileIndex] = 0;
                }
            }

        }


        private bool IsFantomReachable(int futureLevel, int tileIndex)
        {
            return _futurePossibleFantomPositions[futureLevel, tileIndex] > 0;
        }

        private bool IsAnySeekerReachable(int futureLevel, int tileIndex)
        {
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
                if (IsSeekerReachable(seekerIndex, futureLevel, tileIndex))
                    return true;
            return false;
        }

        private bool IsSeekerReachable(int seekerIndex, int futureLevel, int tileIndex)
        {
            return _futurePossiblePositionsSeekers[seekerIndex, futureLevel, tileIndex] > 0;
        }

        private void RecalculateFuturePositions()
        {
            // level 0 of _futurePossiblePositionsSeekers has current Seeker positions

            // NOTE: maybe splitting the array to make clearing faster would be better?
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                for (int future = 1; future < FUTURE_STEPS + 1; ++future)
                {
                    for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                    {
                        _futurePossiblePositionsSeekers[seekerIndex, future, tileIndex] = 0;
                    }
                }
            }

            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {

                bool[] hasMode = [
                    (seekerIndex >= _detectivesCount || _detectiveTickets[seekerIndex, (int)TicketKinds.Mode1] > 0),
                    (seekerIndex >= _detectivesCount || _detectiveTickets[seekerIndex, (int)TicketKinds.Mode2] > 0),
                    (seekerIndex >= _detectivesCount || _detectiveTickets[seekerIndex, (int)TicketKinds.Mode3] > 0)
                ];

                // uses _futurePossiblePositionsSeekers[future-1] to determine _futurePossiblePositionsSeekers[future]
                for (int future = 1; future < FUTURE_STEPS + 1; ++future)
                {
                    foreach (var travelMode in TRAVEL_MODES_AS_ARRAY)
                    {
                        // Assume that the seeker can afford this move
                        // NOTE: since we aren't updating the counts between future levels, this prediction is not entirely accurate
                        if (hasMode[(int)travelMode])
                        {
                            for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                            {
                                if (IsSeekerReachable(seekerIndex, future - 1, tileIndex))
                                {
                                    var neighborCount = _fantomBoard.CountNeighbors(travelMode, tileIndex);
                                    for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                                    {
                                        // just set to 1
                                        _futurePossiblePositionsSeekers[
                                            seekerIndex,
                                            future,
                                            _fantomBoard.GetNeighbor(travelMode, tileIndex, neighborIndex)] = 1;
                                    }
                                }
                            }
                        }
                    }

                    // Count how many total different tiles can each Seeker cover
                    var sum = 0;
                    for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                    {
                        if (IsSeekerReachable(seekerIndex, future, tileIndex))
                            sum++;
                    }
                    if (sum > 0)
                        _futurePossiblePositionsSeekersCountsInverse[seekerIndex, future] = 1f / sum;

                }



            }

            RecalculateEvaluation();
        }

        private void RecalculateEvaluation()
        {
            Array.Clear(_tilesEvaluation);

            // determine likeliness of a tile being occupied by a seeker           

            var cost = CURRENT_COST;

            for (int future = _currentLevel; future <= FUTURE_STEPS; ++future)
            {

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    _tilesEvaluation[tileIndex] += IsAnySeekerReachable(future, tileIndex) ? cost : 0;
                }

                cost *= FUTURE_COST_FALLOFF;
            }

            // normalize values to be in range (0, 1)
            /*
            float total = 0;
            for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
            {
                total += _tilesEvaluation[tileIndex];
            }

            if (total > 0)
            {
                var f = MAX_EVAL_VALUE / total;

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    // Flip 
                    _tilesEvaluation[tileIndex] = MAX_EVAL_VALUE - _tilesEvaluation[tileIndex] * f;
                }
            }
            */

            // flip no normalization 
            for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
            {
                // Flip 
                _tilesEvaluation[tileIndex] = MAX_EVAL_VALUE - _tilesEvaluation[tileIndex];
            }

        }


        private void RecalculateSeekerCatchPotentials()
        {
            // Implement future pred.

            Array.Clear(_seekerReachableFromRevealCount);
            Array.Clear(_seekerCatchPotentialByMode);
            Array.Clear(_tempPositionValues1);

            Array.Clear(_tempPositions1);

            var neighborCount = 0;

            // For current level 
            // Determine how many positions for each tile and mode will result in being reachable by a Seeker
            // if they are valid positions (not leading to position with Seeker now)           
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    // For the 3 main Modes
                    for (int ticketIndex = 0; ticketIndex < 3; ++ticketIndex)
                    {

                        neighborCount = _fantomBoard.CountNeighbors((TravelModes)ticketIndex, tileIndex);
                        for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                        {
                            var neighborTile = _fantomBoard.GetNeighbor((TravelModes)ticketIndex, tileIndex, neighborIndex);

                            // Isn't there now, but could be next round
                            if (!IsAnySeekerReachable(0, neighborTile) &&
                                IsSeekerReachable(seekerIndex, 1, neighborTile))
                            {
                                _seekerReachableFromRevealCount[seekerIndex, ticketIndex, tileIndex]++;
                                _tempPositions1[neighborTile] = 1;
                            }
                        }
                    }

                    // Black by above union of others
                    _seekerReachableFromRevealCount[seekerIndex, 3, tileIndex] = _tempPositions1.Sum();

                    // And by River
                    neighborCount = _fantomBoard.CountNeighbors(TravelModes.River, tileIndex);
                    for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                    {
                        var neighborTile = _fantomBoard.GetNeighbor(TravelModes.River, tileIndex, neighborIndex);

                        // Isn't there now, but could be next round
                        if (!IsAnySeekerReachable(0, neighborTile) &&
                            IsSeekerReachable(seekerIndex, 1, neighborTile))
                        {
                            _seekerReachableFromRevealCount[seekerIndex, 3, tileIndex]++;                            
                        }
                    }

                    for (int ticketIndex = 0; ticketIndex < 4; ++ticketIndex)
                    {
                        if (IsSeekerReachable(seekerIndex, 1, tileIndex) &&
                            _seekerReachableFromRevealCount[seekerIndex, ticketIndex, tileIndex] > 0)
                            
                            // Take the biggest value across all seekers
                            _seekerCatchPotentialByMode[0, ticketIndex, tileIndex] = 
                                Math.Max(_seekerCatchPotentialByMode[0, ticketIndex, tileIndex], 
                                1f / _seekerReachableFromRevealCount[seekerIndex, ticketIndex, tileIndex]);
                    }
                    Array.Clear(_tempPositions1);
                }


            }


            // For the future ones, only approximately
            for (int future = 1; future < FUTURE_STEPS; ++future)
            {

                // Determine how many positions for each tile and mode will result in being reachable by a Seeker
                // if they are valid positions (not leading to position with Seeker now)           
                for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
                {
                    for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                    {
                        // For the 3 main Modes
                        for (int ticketIndex = 0; ticketIndex < 3; ++ticketIndex)
                        {

                            Array.Clear(_tempPositions1);

                            neighborCount = _fantomBoard.CountNeighbors((TravelModes)ticketIndex, tileIndex);
                            for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                            {
                                var neighborTile = _fantomBoard.GetNeighbor((TravelModes)ticketIndex, tileIndex, neighborIndex);


                                // REDO: to be more like before -> harsher with the close by tiles, 


                                // Isn't there now, but could be next round -> could make this Move
                                var moveProb =
                                    // what is the 'prob' of this Seeker not being there this turn
                                    (1 - _futurePossiblePositionsSeekers[seekerIndex, future, neighborTile] *
                                    _futurePossiblePositionsSeekersCountsInverse[seekerIndex, future]) *

                                    // what is the 'prob' of this Seeker being there next turn
                                    (_futurePossiblePositionsSeekers[seekerIndex, future + 1, neighborTile] *
                                    _futurePossiblePositionsSeekersCountsInverse[seekerIndex, future + 1]);

                                _seekerCatchPotentialByMode[future, ticketIndex, tileIndex] = Math.Max(moveProb, _seekerCatchPotentialByMode[future, ticketIndex, tileIndex]);

                                if (moveProb > 0)
                                {
                                    _tempPositionValues1[neighborTile] = Math.Max(moveProb, _tempPositionValues1[neighborTile]);
                                }

                                
                            }
                        }

                        // Black by above union of others (ignore River for simplicity)
                        _seekerCatchPotentialByMode[future, 3, tileIndex] = Math.Max(_seekerCatchPotentialByMode[future, 3, tileIndex], _tempPositionValues1.Sum());
                        
                        Array.Clear(_tempPositionValues1);
                    }
                }


            }
        }

        
        private void RecalculateCornered()
        {

            Array.Clear(_tempPositions1);
            Array.Clear(_tempPositionValues1);

            for (int future = 0; future < FUTURE_STEPS; ++future)
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    // Separately for all Seekers for their specific values
                    for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
                    {
                        // For the 3 main Modes
                        for (int ticketIndex = 0; ticketIndex < 3; ++ticketIndex)
                        {

                            var neighborCount = _fantomBoard.CountNeighbors((TravelModes)ticketIndex, tileIndex);
                            for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                            {
                                var neighborTile = _fantomBoard.GetNeighbor((TravelModes)ticketIndex, tileIndex, neighborIndex);

                                // Is there now or could be next Turn                        
                                if (IsSeekerReachable(seekerIndex, future, neighborTile))
                                {
                                    _tempPositionValues1[neighborTile] = Math.Max(_tempPositionValues1[neighborTile], _futurePossiblePositionsSeekersCountsInverse[seekerIndex, future]);
                                }
                                if (IsSeekerReachable(seekerIndex, future + 1, neighborTile))
                                {
                                    _tempPositionValues1[neighborTile] = Math.Max(_tempPositionValues1[neighborTile], _futurePossiblePositionsSeekersCountsInverse[seekerIndex, future + 1]);
                                }

                                // Count how many positions are Reachable by Fantom
                                _tempPositions1[neighborTile] = 1;
                            }
                        }
                    }

                    float reachableTiles = _tempPositionValues1.Sum();
                    int totalTiles = _tempPositions1.Sum();

                    // overestimate cornered a bit
                    _cornered[future, tileIndex] = 1.5f * (float)reachableTiles / totalTiles;

                    Array.Clear(_tempPositions1);
                    Array.Clear(_tempPositionValues1);
                }

            }

        }


        private void RecalculateReachableCounts()
        {
            // For the 3 main Modes

            for (int ticketIndex = 0; ticketIndex < 3; ++ticketIndex)
            {

                Array.Clear(_tempPositions1);

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    if (IsFantomReachable(0, tileIndex))
                    {
                        var neighborCount = _fantomBoard.CountNeighbors((TravelModes)ticketIndex, tileIndex);
                        for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                        {
                            var neighborTile = _fantomBoard.GetNeighbor((TravelModes)ticketIndex, tileIndex, neighborIndex);

                            // Only tiles where seekers aren't
                            if (!IsAnySeekerReachable(0, neighborTile))
                                _tempPositions1[neighborTile] = 1;

                        }
                    }
                }

                _reachableTilesByMode[ticketIndex] = _tempPositions1.Sum();
            }
        }

        
    }
}
