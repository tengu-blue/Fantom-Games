using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesIntermediary.Opponent.Parts.FantomParts;

namespace FantomGamesIntermediary.Opponent.Parts.SeekerParts
{
    /// <summary>
    /// Keeps track of possible positions for the Fantom in the current game.
    /// 
    /// Is updated with every Move the Fantom makes. 
    /// 
    /// A Mask might be applied that blocks certain positions, and includes which Tickets would be added to the 'budget'
    /// 
    /// Returns a value for a given 'move' from the held state - value is in a range; if the value is MAX, that Move is guaranteed to win the game.
    /// 
    /// </summary>
    internal class SeekersStateEvaluator(
        int _seekersCount,
        int _detectivesCount,
        IReadOnlyFantomBoard _fantomBoard,
        IEnumerable<int> _startingPositions)
        : IStateEvaluator<SeekersStateEvaluator, SeekersMove, FantomMove>
    {
        /// <summary>
        /// The number of Moves we try to predict in the Future.
        /// </summary>
        // Keep high enough (at least 2 definitely)
        const int FUTURE_STEPS = 6;
        // 100 for fun, instead of boring 1 :)
        const float MAX_EVAL_VALUE = 100f;

        // TODO: experiment
        const float CURRENT_COST = 100f;
        const float FUTURE_COST_FALLOFF = 0.5f;


        // + 1 for easier indexing, zero is unused !! remember to skip
        // int[] _currentPossiblePositions; // = new int[tileCount + 1];
        // TODO : fixed sizes -> probably not worth the trouble
        int[] _fantomTickets = new int[FantomGameSettings.TICKET_KINDS_COUNT];


        // Helper temp arrays
        int[] _tempPositions1 = new int[_fantomBoard.TileCount + 1];
        int[] _tempPositions2 = new int[_fantomBoard.TileCount + 1];

        // _futurePossiblePositions[0, x] is the _currentPossiblePositions
        int currentFantomPositions = 0;
        bool fantomCornered = false;
        int[,] _futurePossiblePositions = new int[FUTURE_STEPS + 2, _fantomBoard.TileCount + 1];
        // helper arrays for determining the Next Move as accurately as possible
        // the first dim is the travel mode, 
        int[,] _nextPossiblePositionsByMode = new int[TRAVEL_MODES_AS_ARRAY.Length, _fantomBoard.TileCount + 1];


        int[] _seekerPositions = new int[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];
        // Masks for future evaluations blocking out Seeker Positions, only the 'future ones'
        SeekersMask[] _masks = new SeekersMask[FUTURE_STEPS];

        // Which level is now (default 0, but when assuming moves, will increase)
        int _currentLevel = 0;

        // Cached evaluation for fast retrieval for thousands of states 
        float[] _tilesEvaluation = new float[_fantomBoard.TileCount + 1];

        int TileCount { get => _futurePossiblePositions.GetLength(1) - 1; }

        // ------------------------------------------------------------------------------------

        // Works with the same game configuration, same settings -> starting positions and tile count
        public void Reset(FantomGameState initState)
        {
            // Array.Clear(_currentPossiblePositions);
            Array.Clear(_futurePossiblePositions);
            Array.Clear(_seekerPositions);
            Array.Clear(_tilesEvaluation);

            _currentLevel = 0;
            for (int i = 0; i < _masks.Length; ++i)
                _masks[i].Clear();

            initState.CopyFantomTicketsTo(_fantomTickets);

            // _currentPossiblePositions has 1 in spots that he might be at when the game starts, 0 elsewhere
            foreach (var pos in _startingPositions)
            {
                //_currentPossiblePositions[pos] = 1;
                _futurePossiblePositions[0, pos] = 1;
            }

            // remove Seeker positions out of the startingPositions
            for (int seekerIndex = 0; seekerIndex < initState.SeekersCount; ++seekerIndex)
            {
                //_currentPossiblePositions[currentState.GetSeekerPosition(seekerIndex)] = 0;
                _futurePossiblePositions[0, initState.GetSeekerPosition(seekerIndex)] = 0;
                _seekerPositions[seekerIndex] = initState.GetSeekerPosition(seekerIndex);
            }

            // RecalculateNextModePositions();
        }



        static float[] mode_mods = { 10, 5, 2 };

        public float Evaluate(int tileIndex)
        {
            return _futurePossiblePositions[0, tileIndex] > 0 ? 1 : 0;
        }

        public float Evaluate(SeekersMove move)
        {
            float value = 0f;

            // if the move covers all current possible positions for the Fantom, then the value is MAX automatically
            int currentFantomTilesCovered = 0;

            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {


                // seeker index is valid
                // if seeker doesn't move ?? technically zero for it so..
                if (move.GetDestination(seekerIndex) > 0)
                {
                    currentFantomTilesCovered += _futurePossiblePositions[_currentLevel, move.GetDestination(seekerIndex)] > 0 ? 1 : 0;


                    value += _tilesEvaluation[move.GetDestination(seekerIndex)];

                    // TODO: change based on Ticket type and game phase
                    // value += mode_mods[(int)move.GetMode(seekerIndex)];

                }
            }

            //if (fantomCornered)
            //    return MAX_EVAL_VALUE;

            if (currentFantomPositions == 0 || currentFantomTilesCovered == currentFantomPositions)
            {
                return MAX_EVAL_VALUE;
            }
            return value;
        }


        public void ResetMask(int level)
        {
            // Take the mask from the specified level, set it back to the original values
            // and update the cascade, essentially in reverse of the ApplyMask

            for (int i = 0; i < _tempPositions1.Length; ++i)
            {
                _tempPositions1[i] = _futurePossiblePositions[level, i];
            }

            // Go back one level essentially
            _currentLevel = level;

            // maskApply has 2 * SeekersCount spaces, the first SeekersCount represent the current mask, that might need to be
            // removed, but some might be kept; the other represent the new Mask whose positions might be already applied, but
            // some might need applying yet
            if (_masks[level].Valid())
            {
                for (int i = 0; i < _seekersCount; ++i)
                {
                    // Restore original values
                    _tempPositions1[_masks[level].GetSeekerPosition(i)] = _masks[level].GetSeekerMaskValue(i);
                }

                RecalculateFuturePositionsFromTemp(level);
                RecalculateEvaluation();
            }

            _masks[level].Clear();
        }


        private void RemoveMask(int level)
        {
            if (_masks[level].Valid())
            {
                for (int i = 0; i < _seekersCount; ++i)
                {
                    // Restore original values
                    _futurePossiblePositions[level, _masks[level].GetSeekerPosition(i)] =
                        _masks[level].GetSeekerMaskValue(i);
                }

                _masks[level].Clear();
            }
        }

        private void UnsetMaskInTemp(int level)
        {
            // But since the new might and the old might have some overlap, use temp arrays
            // and only recursively update if needed.
            for (int i = 0; i < _tempPositions1.Length; ++i)
            {
                _tempPositions1[i] = _futurePossiblePositions[level, i];
            }

            // If there is a mask at this level -> reset it in the temp !! 
            if (_masks[level].Valid())
            {
                for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
                {
                    // Restore original values for all future masks
                    _tempPositions1[_masks[level].GetSeekerPosition(seekerIndex)] = _masks[level].GetSeekerMaskValue(seekerIndex);

                    // Actually Moved -> modify Tickets
                    if (_masks[level].Moved(seekerIndex) && seekerIndex < _detectivesCount)
                        _fantomTickets[_masks[level].GetSeekerTicketUsed(seekerIndex)]--;
                }
            }
        }

        private void ClearMasks()
        {
            for (int maskIndex = 0; maskIndex < _masks.Length; ++maskIndex)
                _masks[maskIndex].Clear();
        }

        private bool FantomRevealAt(int? revealedAt)
        {
            if (revealedAt is not null)
            {
                // clear the current possible positions
                Array.Clear(_futurePossiblePositions, 0, TileCount + 1);
                // set the revealed position as the only one
                _futurePossiblePositions[0, revealedAt.Value] = 1;

                return true;
            }

            return false;
        }

        // After this is called, the _futurePossiblePositions[0, ...] contains accurate representation of Fantom's current whereabouts
        private void FantomPlay(TicketKinds ticket, int? revealedAt)
        {
            // remove the used Ticket
            _fantomTickets[(int)ticket]--;

            if (!FantomRevealAt(revealedAt))
            {
                RecalculateNextMovePositionsByMode();

                // set the possible positions as 1 else 0 by the used Ticket
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    // Black Ticket used means can be at any of the Travel Modes locations
                    if (ticket == TicketKinds.Black)
                    {
                        _futurePossiblePositions[0, tileIndex] =
                            _nextPossiblePositionsByMode[(int)TravelModes.Mode1, tileIndex] +
                             _nextPossiblePositionsByMode[(int)TravelModes.Mode2, tileIndex] +
                             _nextPossiblePositionsByMode[(int)TravelModes.Mode3, tileIndex] +
                             _nextPossiblePositionsByMode[(int)TravelModes.River, tileIndex] > 0
                             ? 1 : 0;
                    }
                    else
                    // other Tickets -> convert to the appropriate travel mode, then take only those tiles
                    {
                        _futurePossiblePositions[0, tileIndex] =
                            _nextPossiblePositionsByMode[(int)TICKET_TO_TRAVEL_MODE[(int)ticket], tileIndex] > 0
                            ? 1 : 0;
                    }
                }
            }

            // Fantom cannot be at the Seekers' positions 
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                // NOTE: for Seekers that aren't playing, the position will be 0, which won't break anything
                _futurePossiblePositions[0, _seekerPositions[seekerIndex]] = 0;
            }
        }

        public void OpponentPlay(FantomMove opMove)
        {

            for (int i = 0; i < opMove.MovesCount; ++i)
            {
                // actually Moved 
                if (opMove.Moved(i))
                    FantomPlay(opMove.GetTicketUsed(i), opMove.GetDestination(i));
                // didn't Move, but it was a revealing turn, so the position is known
                else if (opMove.WasRevealing(i))
                    FantomRevealAt(opMove.GetDestination(i));

            }
            // remove the Double Ticket if used            
            if (opMove.IsDouble)
                _fantomTickets[(int)TicketKinds.Double]--;

        }

        public void OwnPlay(SeekersMove move)
        {
            // Apply all the seekers move one by one
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                var tileIndex = move.GetDestination(seekerIndex);

                // The Seeker actually Moved
                if (move.Moved(seekerIndex))
                {

                    _seekerPositions[seekerIndex] = tileIndex;
                    // this position cannot be occupied by the Fantom now (else game over and win)
                    // NOTE: neither in the next Move since the Fantom's Turn is next

                    _futurePossiblePositions[0, tileIndex] = 0;
                    // NOTE: at this point the future predictions are wrong but they will be recalculated once the Fantom plays

                    // if not bobby - add the ticket
                    if (seekerIndex < _detectivesCount)
                        _fantomTickets[move.GetTicketAsInt(seekerIndex)]++;

                }
                // NOTE: for not playing seekers the _seekerPositions need to be 0
            }
        }

        public void PrepareForSearch()
        {
            // Recalculate the possible positions only now - avoid repeat calculations, as Fantom might have played two Tickets
            // ForgetMove(0);
            // Clear Masks
            ClearMasks();
            _currentLevel = 0;
            RecalculateFuturePositions();

        }


        public void AssumeMove(SeekersMove move, int level)
        {
            // If there was already an assumption, we need to clear it first
            // Also clear any future ones
            ForgetMove(level + 1);

            UnsetMaskInTemp(level);

            // Store the mask
            _masks[level].SetFrom(move);
            _currentLevel = level + 1;

            // Apply the new mask to temp
            // NOTE: _futurePositions potentially still contains the previous masked information, this is used in
            // the RecalculateFuturePositionsFromTemp() 
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                // Remember original values and replace them with 0
                _masks[level].SetSeekerMaskValue(
                    seekerIndex,
                    _futurePossiblePositions[level, _masks[level].GetSeekerPosition(seekerIndex)]);

                _tempPositions1[_masks[level].GetSeekerPosition(seekerIndex)] = 0;
                // Actually Moved -> modify Tickets
                if (_masks[level].Moved(seekerIndex) && seekerIndex < _detectivesCount)
                    _fantomTickets[_masks[level].GetSeekerTicketUsed(seekerIndex)]++;
            }

            // Recursively update 
            RecalculateFuturePositionsFromTemp(level);
            RecalculateEvaluation();
        }

        public void ForgetMove(int level)
        {
            // Remove masks from last to the level, and update recursively all temp positions
            for (int maskLevel = _masks.Length - 1; maskLevel >= level; --maskLevel)
            {
                UnsetMaskInTemp(maskLevel);

                if (_masks[level].Valid())
                {
                    // Recursively update 
                    RecalculateFuturePositionsFromTemp(level);

                    _masks[level].Clear();
                }
            }

            _currentLevel = level;
            RecalculateEvaluation();
        }


        public void UpdateFrom(SeekersStateEvaluator otherEvaluator)
        {
            for (int i = 0; i < FantomGameSettings.TICKET_KINDS_COUNT; ++i)
            {
                _fantomTickets[i] = otherEvaluator._fantomTickets[i];
            }

            for (int future = 0; future < FUTURE_STEPS + 2; ++future)
            {
                for (int tileIndex = 0; tileIndex < _fantomBoard.TileCount + 1; ++tileIndex)
                {
                    _futurePossiblePositions[future, tileIndex] = otherEvaluator._futurePossiblePositions[future, tileIndex];
                }
            }

            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                _seekerPositions[seekerIndex] = otherEvaluator._seekerPositions[seekerIndex];
            }

            for (int maskIndex = 0; maskIndex < FUTURE_STEPS; ++maskIndex)
            {
                _masks[maskIndex] = otherEvaluator._masks[maskIndex];
            }

            _currentLevel = otherEvaluator._currentLevel;

            for (int tileIndex = 0; tileIndex < _fantomBoard.TileCount + 1; ++tileIndex)
            {
                _tilesEvaluation[tileIndex] = otherEvaluator._tilesEvaluation[tileIndex];
            }

        }



        // ------------------------------------------------------------------------------------------------

        static readonly TravelModes[] TRAVEL_MODES_AS_ARRAY = {
            TravelModes.Mode1,
            TravelModes.Mode2,
            TravelModes.Mode3,
            TravelModes.River
        };

        static readonly TravelModes[] TICKET_TO_TRAVEL_MODE =
        {
            TravelModes.Mode1,
            TravelModes.Mode2,
            TravelModes.Mode3,
            TravelModes.Mode1, // This one is nonsense
            TravelModes.River,
        };

        private bool IsReachable(int futureLevel, int tileIndex)
        {
            return _futurePossiblePositions[futureLevel, tileIndex] > 0;
        }

        private void RecalculateNextMovePositionsByMode()
        {
            Array.Clear(_nextPossiblePositionsByMode);

            RemoveMask(0);

            foreach (var travelMode in TRAVEL_MODES_AS_ARRAY)
            {
                // go over all tiles that are currently reachable, and mark their neighbors via this travel mode as reachable
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    if (IsReachable(0, tileIndex))
                    {
                        var neighborCount = _fantomBoard.CountNeighbors(travelMode, tileIndex);
                        for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                        {
                            // increase to know 'number of paths'
                            _nextPossiblePositionsByMode[(int)travelMode, _fantomBoard.GetNeighbor(travelMode, tileIndex, neighborIndex)]++;
                        }
                    }
                }
            }

        }
        /*
        private void RecalculateNextMoveAccurately()
        {
            RecalculateNextModePositions();

            bool hasBlack = _fantomTickets[(int)TicketKinds.Black] > 0;            
            int[] modeMask = [
                _fantomTickets[(int)TicketKinds.Mode1] > 0 ? 1 : 0,
                _fantomTickets[(int)TicketKinds.Mode2] > 0 ? 1 : 0,
                _fantomTickets[(int)TicketKinds.Mode3] > 0 ? 1 : 0,
                _fantomTickets[(int)TicketKinds.River] > 0 ? 1 : 0  
            ];

            // set the possible positions as 1 else 0 by the used Ticket
            for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
            {
                if (hasBlack)
                {
                    _futurePossiblePositions[1, tileIndex] =
                        (_nextPossiblePositionsByMode[(int)TravelModes.Mode1, tileIndex] +
                         _nextPossiblePositionsByMode[(int)TravelModes.Mode2, tileIndex] +
                         _nextPossiblePositionsByMode[(int)TravelModes.Mode3, tileIndex] +
                         _nextPossiblePositionsByMode[(int)TravelModes.River, tileIndex]) > 0
                         ? 1 : 0;
                }
                else
                {
                    _futurePossiblePositions[1, tileIndex] =
                        (_nextPossiblePositionsByMode[(int)TravelModes.Mode1, tileIndex] * modeMask[0] +
                         _nextPossiblePositionsByMode[(int)TravelModes.Mode2, tileIndex] * modeMask[1] +
                         _nextPossiblePositionsByMode[(int)TravelModes.Mode3, tileIndex] * modeMask[2] +
                         _nextPossiblePositionsByMode[(int)TravelModes.River, tileIndex] * modeMask[3]) > 0
                         ? 1 : 0;
                }
            }
        }

        */

        private void RecalculateFuturePositionsFromTemp(int fromLevel)
        {

            bool hasBlack = _fantomTickets[(int)TicketKinds.Black] > 0;
            // fixed bool hasMode[] = 
            bool[] hasMode = [
                (_fantomTickets[(int)TicketKinds.Mode1] > 0),
                (_fantomTickets[(int)TicketKinds.Mode2] > 0),
                (_fantomTickets[(int)TicketKinds.Mode3] > 0),
                (_fantomTickets[(int)TicketKinds.River] > 0), // or Black, but that is already included
            ];

            for (int future = fromLevel + 1; future < FUTURE_STEPS + 2; ++future)
            {

                // Compare temp with the specified level and when it finds a difference, it will recompute the future levels
                // If it does find a difference, it will cause a cascade into the future levels as well
                bool update = false;
                for (int i = 0; i < _tempPositions2.Length; ++i)
                {
                    _tempPositions2[i] = _futurePossiblePositions[future, i];
                }

                for (int tileIndex = 1; tileIndex < _tempPositions2.Length; ++tileIndex)
                {
                    int mod =
                        // temp wants this position removed, but originally it was active
                        _tempPositions1[tileIndex] == 0 && _futurePossiblePositions[future - 1, tileIndex] > 0 ? -1 :
                        // temp wants this position set, but originally it was impossible
                        _tempPositions1[tileIndex] > 0 && _futurePossiblePositions[future - 1, tileIndex] == 0 ? 1 :
                        0;

                    if (mod != 0)
                    {
                        update = true;

                        // working on _futurePossiblePositions[future] from future-1; through temp1 and temp2
                        foreach (var travelMode in TRAVEL_MODES_AS_ARRAY)
                        {
                            // can make this kind of a Move -> black can get anywhere
                            if (hasBlack || hasMode[(int)travelMode])
                            {
                                var neighborCount = _fantomBoard.CountNeighbors(travelMode, tileIndex);
                                for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                                {
                                    // increase / decrease to know 'number of paths'
                                    _tempPositions2[_fantomBoard.GetNeighbor(travelMode, tileIndex, neighborIndex)] += mod;
                                }
                            }
                        }
                    }

                }

                // copy from temp1 to the actual future positions
                for (int i = 0; i < _tempPositions1.Length; ++i)
                {
                    _futurePossiblePositions[future - 1, i] = _tempPositions1[i];
                }

                // swap temps and continue (essentially recursion until no meaningful changes
                (_tempPositions1, _tempPositions2) = (_tempPositions2, _tempPositions1);

                if (!update)
                    break;
            }

            // RecalculateEvaluation();
        }


        // NOTE: this part will require a lot of efficiency improvements via caching etc.
        // the more accurate this is, the better the Evaluator will be
        private void RecalculateFuturePositions()
        {
            // The current held Tickets determine the potential of where the Fantom might be next 'Turn'

            // We care about certain scenarios:
            // 1) If he has a Black Ticket or at least one of each of the main 3 Modes
            // 2) If he has a Double Ticket - if he has at least two Black / enough Tickets to play as if Black twice
            // 3) If he is missing a certain type of transport completely

            // we always have to consider the worst case scenario - all Moves possible

            // It would be extremely computationally difficult to consider 'all paths' from all possible starting positions
            // with all possible Tickets to get the most accurate prediction and the final result probably wouldn't even matter that much
            // instead go for a simplified - worst-case prediction. Have a very accurate next Move prediction (easy). The future ones, can be a little off.
            // ignore double with calculation - if he has a double Ticket, possible moves are from 'future' and the one over

            bool hasBlack = _fantomTickets[(int)TicketKinds.Black] > 0;
            // fixed bool hasMode[] = 
            bool[] hasMode = [
                (_fantomTickets[(int)TicketKinds.Mode1] > 0),
                (_fantomTickets[(int)TicketKinds.Mode2] > 0),
                (_fantomTickets[(int)TicketKinds.Mode3] > 0),
                (_fantomTickets[(int)TicketKinds.River] > 0), // or Black, but that is already included
            ];
            // bool hasDouble = false;

            // TODO: improve by keeping the moves separate then for double we know if he has at least 2 Tickets for each -> more accurate !next! move only, the rest, assume double worst case but also might use multiple doubles so...
            // the main issue is only when the Ticket numbers are very low - like has only 1 of some ticket type... this won't happen too often since, the number of Tickets for the Fantom goes up quite quickly.

            // RecalculateNextMoveAccurately();
            // NOPE: starting from 2, because ^^

            // reset futures (not now)
            Array.Clear(_futurePossiblePositions, TileCount + 1, TileCount * (FUTURE_STEPS + 1));

            // RecalculateNextModePositions();
            for (int future = 1; future < FUTURE_STEPS + 2; ++future)
            {
                // working on _futurePossiblePositions[future] from future-1
                foreach (var travelMode in TRAVEL_MODES_AS_ARRAY)
                {
                    // can make this kind of a Move -> black can get anywhere
                    if (hasBlack || hasMode[(int)travelMode])
                    {
                        // go over all tiles that are currently reachable, and mark their neighbors via this travel mode as reachable

                        for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                        {
                            if (IsReachable(future - 1, tileIndex))
                            {
                                var neighborCount = _fantomBoard.CountNeighbors(travelMode, tileIndex);
                                for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                                {
                                    // increase to know 'number of paths'
                                    _futurePossiblePositions[future, _fantomBoard.GetNeighbor(travelMode, tileIndex, neighborIndex)]++;
                                }
                            }
                        }
                    }
                }
            }

            RecalculateEvaluation();
        }

        private void RecalculateEvaluation()
        {
            // now form the evaluation
            Array.Clear(_tilesEvaluation);

            var cost = CURRENT_COST;

            bool hasDouble = _fantomTickets[(int)TicketKinds.Double] > 0;

            // mark now and future tiles
            // NOTE: FUTURE_STEPS + 1 is for if has double Tickets
            for (int future = _currentLevel; future <= FUTURE_STEPS; ++future)
            {

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    _tilesEvaluation[tileIndex] += IsReachable(future, tileIndex) ? cost : 0;

                    // experiment with this

                    if (future != _currentLevel && hasDouble)
                    {
                        _tilesEvaluation[tileIndex] += IsReachable(future + 1, tileIndex) ? cost : 0;
                    }

                }
                cost *= FUTURE_COST_FALLOFF;
            }

            // NOTE: this is an approximation because double Tickets complicate how many tiles the Fantom might be at
            currentFantomPositions = 0;
            // The Fantom's possible positions now, are accurate
            if (_currentLevel == 0)
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    currentFantomPositions += IsReachable(_currentLevel, tileIndex) ? 1 : 0;
                }
            }
            // Future levels might be less accurate
            else
            {
                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    currentFantomPositions += IsReachable(_currentLevel, tileIndex) || 
                        (hasDouble && IsReachable(_currentLevel + 1, tileIndex)) ? 1 : 0;
                }
            }

            // normalize values to be in range (0, MAX)
            float total = 0;
            for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
            {
                total += _tilesEvaluation[tileIndex];
            }

            fantomCornered = false;

            if (total > 0)
            {
                var f = MAX_EVAL_VALUE / total;

                for (int tileIndex = 1; tileIndex <= TileCount; ++tileIndex)
                {
                    _tilesEvaluation[tileIndex] *= f;
                }
            }
            // total is 0
            else
            {
                fantomCornered = true;
            }

        }


    }


    /// <summary>
    /// Represents the possible state a move will lead to made by the Seekers.
    /// </summary>
    unsafe struct SeekersMask
    {
        private bool _used;

        private fixed int _maskValues[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];
        private SeekersMove _move;

        // Use to build the Mask
        public void SetFrom(SeekersMove move)
        {
            _move = move;
            _used = true;
        }

        public void SetSeekerMaskValue(int seekerIndex, int value)
        {
            _maskValues[seekerIndex] = value;
        }

        // -----------------------------------------------------------------------------

        // no index checks for speed

        public readonly bool Moved(int seekerIndex) => _move.Moved(seekerIndex);
        public readonly int GetSeekerTicketUsed(int detectiveIndex) => _move.GetTicketAsInt(detectiveIndex);
        public readonly int GetSeekerPosition(int seekerIndex) => _move.GetDestination(seekerIndex);

        public int GetSeekerMaskValue(int seekerIndex)
        {
            return _maskValues[seekerIndex];
        }


        // invalidate the Mask
        public void Clear()
        {
            _used = false;
        }

        public readonly bool Valid() => _used;
    }
}
