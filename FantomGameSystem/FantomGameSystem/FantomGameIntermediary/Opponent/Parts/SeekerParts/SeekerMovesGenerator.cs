using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent.Parts.SeekerParts
{

    /// <summary>
    /// Generates all possible 'legal' Moves from the given 
    /// </summary>
    /// <param name="_order"></param>
    internal struct SeekerMovesGenerator(
        IReadOnlyFantomBoard _fantomBoard,
        int _seekersCount,
        int _detectivesCount,
        bool _order) : ILegalMovesGenerator<SeekersMove, SeekersState>
    {

        // NOTE: this one will need to be reduced, ideally only generating potentially good moves, probably integrate the evaluator here - generate based on best 'guess'


        public readonly bool IsLegal(SeekersState state, SeekersMove move)
        {
            // TODO: _order check if enabled, even no move needs to be updated

            // for now, assume _order always true
            // Try to find conflicts
            // Invariant (_order true) - all Seekers with index smaller than seekerIndex1 have done a valid Move
            for (int seekerIndex1 = 0; seekerIndex1 < _seekersCount; ++seekerIndex1)
            {
                var seekerDestination = move.GetDestination(seekerIndex1);

                if (move.Moved(seekerIndex1))
                {

                    // 1) Seekers have the appropriate Tickets                   
                    // Bobbies don't have Tickets, so ignore
                    // NOTE: abusing enum numbering same for Modes
                    if (seekerIndex1 < _detectivesCount &&
                        state.GetDetectiveTickets(seekerIndex1, move.GetTicketUsed(seekerIndex1)) <= 0)
                    {
                        // Cannot afford
                        return false;
                    }

                    // 2)   Seekers aren't trying to go to positions with other Seekers on them
                    // 2.1) Seeker s cannot go to any tile that Seekers > s are now (ordering);
                    // 2.2) and cannot go to spots that Seekers < s are going to be

                    for (int seekerIndex2 = 0; seekerIndex2 < _seekersCount; ++seekerIndex2)
                    {
                        // no check against self
                        if (seekerIndex1 == seekerIndex2)
                            continue;

                        // Wants to move to a spot with a Seeker on it, that hasn't Moved yet.
                        // 2.1)
                        if (seekerIndex1 < seekerIndex2 &&
                            seekerDestination == state.GetSeekerPosition(seekerIndex2))
                        {
                            return false;
                        }

                        // Wants to move to a spot where another seeker has already moved to
                        // 2.2)
                        if (seekerIndex1 > seekerIndex2 &&
                            seekerDestination == move.GetDestination(seekerIndex2)
                            )
                        {
                            // Trying to Move to a spot where another Seeker is going to Move
                            return false;
                        }

                    }
                }
                // this Seeker is Not Moving
                else
                {
                    // Invariant says the Seekers before have Moved fine.

                    // assume ordering true so this is only fine, if cannot move to free spots
                    // so check if can afford a destination w. Ticket that isn't blocked right now,
                    // so for seekers who have played that would be move.destination and for others
                    // that would be current position

                    for (int modeIndex = 0; modeIndex < 3; ++modeIndex)
                    {
                        // is a bobby or can afford the mode in question
                        if (seekerIndex1 >= _detectivesCount ||
                            state.GetDetectiveTickets(seekerIndex1, (TicketKinds)modeIndex) > 0)
                        {

                            int neighborCount = _fantomBoard.CountNeighbors((TravelModes)modeIndex,
                                                                            seekerDestination);
                            for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                            {
                                var neighborTile = 
                                    _fantomBoard.GetNeighbor((TravelModes)modeIndex,
                                                              seekerDestination, 
                                                              neighborIndex);
                                bool isFree = true;

                                // check that this neighbor tile is not occupied by:                                
                                for (int seekerIndex2 = 0; seekerIndex2 < _seekersCount; ++seekerIndex2)
                                {
                                    // no check against self
                                    if (seekerIndex1 == seekerIndex2)
                                        continue;

                                    // a) a Seeker that hasn't Moved yet
                                    if (seekerIndex1 < seekerIndex2 &&
                                        neighborTile == state.GetSeekerPosition(seekerIndex2))
                                    {
                                        isFree = false;
                                    }

                                    // b) a Seeker that has Moved already
                                    if (seekerIndex1 > seekerIndex2 &&
                                        neighborTile == move.GetDestination(seekerIndex2)
                                        )
                                    {
                                        // Trying to Move to a spot where another Seeker is going to Move
                                        isFree = false;
                                    }

                                }

                                // Found a neighborTile, that is next to this Seeker seekerIndex1,
                                // that can be reached via modeIndex, and isn't currently occupied by another Seeker
                                if (isFree)
                                    return false;
                            }
                        }

                    }

                }
            }

            // Default - no conflicts
            return true;
        }


        // TODO: when run out of Tickets, no Move is possible
        // TODO: no move, when surrounded by other Seekers possible if order +
        // TODO: order on Seekers, allows moving onto now occupied tiles if no cycle
        // Then TODO: redo brain, to allow no move for stationary Moves
        // NOTE: when generating the moves for no move, they should be destinations, so same as now
        public IEnumerable<SeekersMove> PossibleMoves(SeekersState state)
        {
            // TODO: implement _order check inside the validity

            int[] proposedDestination = new int[_seekersCount];
            TicketKinds[] proposedTicket = new TicketKinds[_seekersCount];
            bool[] moved = new bool[_seekersCount];


            // helper has sums for each seeker - [1, how many mode1 + 1, how many 1+mode1+mode2 and 1+mode1+mode2+mode3]
            // so always prev + current
            int[,] helperIndices = new int[_seekersCount, 4];
            int[] seekerHelper = new int[_seekersCount];
            int totalDestinations = 0;
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                helperIndices[seekerIndex, 0] = 1;
                for (int modeIndex = 0; modeIndex < 3; modeIndex++)
                    helperIndices[seekerIndex, 1 + modeIndex] =
                        helperIndices[seekerIndex, modeIndex] +
                        _fantomBoard.CountNeighbors((TravelModes)(modeIndex),
                                                     state.GetSeekerPosition(seekerIndex));

                seekerHelper[seekerIndex] = totalDestinations;
                totalDestinations += helperIndices[seekerIndex, 3];
            }

            // NOTE: this is in preparation for if State Evaluator is added here, to sort them first
            // create an array of destination-ticket(same as mode) for all Seekers, including staying put
            // keep just as one array, but for all seekers
            int[] seekerPossibilities = new int[totalDestinations];

            int possibilityIndex = 0;
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                var seekerPosition = state.GetSeekerPosition(seekerIndex);

                seekerPossibilities[possibilityIndex] = seekerPosition;
                possibilityIndex++;

                for (int modeIndex = 0; modeIndex < 3; ++modeIndex)
                {
                    int neighborCount = _fantomBoard.CountNeighbors(
                        (TravelModes)modeIndex,
                        seekerPosition);
                    for (int neighborIndex = 0; neighborIndex < neighborCount; ++neighborIndex)
                    {
                        seekerPossibilities[possibilityIndex] = _fantomBoard.GetNeighbor(
                            (TravelModes)modeIndex,
                            seekerPosition,
                            neighborIndex);
                        possibilityIndex++;
                    }
                }
            }

            // The whole array has to be filled now 
            Debug.Assert(possibilityIndex == totalDestinations);

            // setup the arrays initially to have begin pos etc. indices to 0 all
            for (int seekerIndex = 0; seekerIndex < _seekersCount; ++seekerIndex)
            {
                proposedDestination[seekerIndex] = GetSeekerPossibility(0, seekerIndex);
                proposedTicket[seekerIndex] = 0;
                moved[seekerIndex] = false;
            }

            // go over all combinations 
            // using sliders over the seekerPossibilities
            // NOTE: if the State Evaluator is added, go over them in a way to maximize the sum 
            int[] indices = new int[_seekersCount];
            bool done = false;
            while (!done)
            {
                // see if the proposed Moved is legal under settings and circumstances                
                var proposedMoved = new SeekersMove(proposedDestination, proposedTicket, moved);
                if (IsLegal(state, proposedMoved))
                {
                    yield return proposedMoved;
                }

                // increase indices, when overflow, done true
                for (int index = 0; index < _seekersCount; ++index)
                {
                    indices[index]++;
                    // Gone over, so increase the next one and reset this one
                    if (indices[index] >= helperIndices[index, 3])
                    {
                        indices[index] = 0;

                        // update the move
                        proposedDestination[index] = GetSeekerPossibility(0, index);
                        // NOTE: the first possibility is always stay at own spot; no ticket
                        proposedTicket[index] = 0;
                        moved[index] = false;
                    }
                    // this one was increased and not over the limit yet; so stop increasing
                    else
                    {
                        // update the move
                        proposedDestination[index] = GetSeekerPossibility(indices[index], index);
                        proposedTicket[index] = (TicketKinds)GetModeFrom(indices[index], index);
                        // NOTE: any but the first are actual Moves
                        moved[index] = true;
                        break;
                    }
                }

                // if all are 0 at this point, means we have done them all
                done = true;
                for (int index = 0; index < _seekersCount; ++index)
                {
                    if (indices[index] != 0)
                    {
                        done = false;
                        break;
                    }
                }
            }

            int GetModeFrom(int index, int seekerIndex)
            {
                // helper indices start w 0 and have sums of modes
                // index 0 is always staying so invalid mode, so return 0 
                // others between ranges
                return index < helperIndices[seekerIndex, 1] ? 0 :
                       index < helperIndices[seekerIndex, 2] ? 1 :
                       2;
            }

            int GetSeekerPossibility(int index, int seekerIndex)
            {
                return seekerPossibilities[seekerHelper[seekerIndex] + index];
            }

        }


    }
}
