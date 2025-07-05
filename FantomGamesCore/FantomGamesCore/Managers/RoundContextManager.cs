using FantomGamesCore.Interfaces;

namespace FantomGamesCore.Managers
{

    /// <summary>
    /// Context kept for each round. Keeps track of which Actors have moved. Manages the movement and placement of Actors.
    /// </summary>

    internal class RoundContextManager(
        int SeekersCount,
        int DetectivesCount,
        FantomBoard BoardManager)
    {
        public FantomBoard BoardManager { get; private set; } = BoardManager;
        public required ActorsManager ActorsManager { get; init; }
        public required TicketsManager TicketsManager { get; init; }

        /// <summary>
        /// The total number of Detectives and Bobbies. CurrentSeekerIndex is between 0 and this number.
        /// </summary>
        public int SeekersCount { get; private set; } = SeekersCount;

        /// <summary>
        /// The number of Detectives.
        /// </summary>
        public int DetectivesCount { get; private set; } = DetectivesCount;

        bool[] _seekerHasMoved = new bool[SeekersCount];

        public void Reset()
        {
            for (int i = 0; i < _seekerHasMoved.Length; ++i)
            {
                _seekerHasMoved[i] = false;
            }
        }

        public void Restart(
            int SeekersCount,
            int DetectivesCount,
            FantomBoard BoardManager
            )
        {
            _seekerHasMoved = new bool[SeekersCount];
            this.BoardManager = BoardManager;
            this.SeekersCount = SeekersCount;
            this.DetectivesCount = DetectivesCount;
        }

        // ------------------------------------------------------------------------------

        public void Moved(int seekerIndex)
        {
            _seekerHasMoved[seekerIndex] = true;
        }

        public bool HasMoved(int seekerIndex)
        {
            return _seekerHasMoved[seekerIndex];
        }

        public bool HaveAllMoved()
        {
            for (int i = 0; i < _seekerHasMoved.Length; ++i)
            {
                if (!_seekerHasMoved[i])
                    return false;
            }

            return true;
        }



        // ------------------------------------------------------------------------------

        public bool IsActorBobby(int actorIndex)
        {
            return actorIndex - 1 >= DetectivesCount;
        }

        public bool IsActorSeeker(int actorIndex)
        {
            return actorIndex != 0;
        }

        public bool IsActorFantom(int actorIndex)
        {
            return actorIndex == 0;
        }

        public bool CheckBoardTileIndex(int tileIndex, out ReturnCode<bool> result)
        {
            if (!BoardManager.IsValidIndex(tileIndex))
            {
                result = new(false, ReturnCodes.BadIndex,
                    $"Tile index out of range {tileIndex} not in 1-{BoardManager.TileCount}.");
                return false;
            }

            result = new(true);
            return true;
        }

        public bool CheckSeekerIndex(int seekerIndex, out ReturnCode<bool> result)
        {
            if (seekerIndex < 0 || seekerIndex >= ActorsManager.ActorsCount - 1)
            {
                result = new(false, ReturnCodes.BadIndex,
                    $"Seeker index out of range {seekerIndex} not in 1-{ActorsManager.ActorsCount - 2}.");
                return false;
            }

            result = new(true);
            return true;
        }

        public bool ActorHasEnoughTickets(int actorIndex, TicketKinds ticketKind, int amount, out ReturnCode<bool> hasResult)
        {
            if (TicketsManager.GetActorTickets(actorIndex, ticketKind) < amount)
            {
                hasResult = new(false, ReturnCodes.NotEnoughTickets,
                    (actorIndex == 0 ?
                    $"Fantom doesn't have enough Tickets {ticketKind} " :
                    $"Seeker {actorIndex - 1} doesn't have enough Tickets {ticketKind}")
                    + $" has {TicketsManager.GetActorTickets(actorIndex, ticketKind)}, needs {amount}.");
                return false;
            }

            hasResult = new(true);
            return true;
        }



        // ------------------------------------------------------------------------------

        /// <summary>
        /// Tries to place the specified Seeker at the given position.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <param name="tileIndex"></param>
        /// <returns>True if the placement was a done. False otherwise.</returns>
        public bool PlaceSeekerAt(int seekerIndex, int tileIndex, out ReturnCode<bool> placeResult)
        {

            // Check indices
            if (!CheckSeekerIndex(seekerIndex, out ReturnCode<bool> seekerResult))
            {
                placeResult = seekerResult;
                return false;
            }

            if (!CheckBoardTileIndex(tileIndex, out ReturnCode<bool> indexResult))
            {
                placeResult = indexResult;
                return false;
            }

            // If position not free -> can't move
            if (ActorsManager.IsActorAt(tileIndex))
            {
                placeResult = new(false, ReturnCodes.PositionOccupied,
                    $"Tile position {tileIndex} occupied by another Actor {ActorsManager.GetActorAt(tileIndex)}, cannot place Seeker {seekerIndex}.");
                return false;
            }


            // Place the detective at that position only if it is free
            ActorsManager.Move(1 + seekerIndex, tileIndex);

            placeResult = new(true);
            return true;
        }

        /// <summary>
        /// Tries to place the Fantom at the given position.
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns>True if the placement was a done. False otherwise.</returns>
        public bool PlaceFantomAt(int tileIndex, out ReturnCode<bool> placeResult)
        {
            // Check indices
            if (!CheckBoardTileIndex(tileIndex, out ReturnCode<bool> tileResult))
            {
                placeResult = tileResult;
                return false;
            }
            // Fantom can be placed only after all the Detectives
            for (int i = 1; i < ActorsManager.ActorsCount; ++i)
                if (!ActorsManager.IsActorOnBoard(i))
                {
                    placeResult = new(false, ReturnCodes.InvalidOperation,
                        $"The Fantom cannot be placed until all Seekers have been. Seeker {i - 1} isn't placed yet.");
                    return false;
                }


            // If position not free -> can't move
            if (ActorsManager.IsActorAt(tileIndex))
            {
                placeResult = new(false, ReturnCodes.PositionOccupied,
                    $"Tile position {tileIndex} occupied by another Actor {ActorsManager.GetActorAt(tileIndex)}, cannot place Fantom.");
                return false;
            }

            // Place Fantom at that position only if it is free
            ActorsManager.Move(0, tileIndex);

            placeResult = new(true);
            return true;
        }


        // ------------------------------------------------------------------------------


        // Fantom tools

        public bool FantomUseDouble(out ReturnCode<bool> useResult)
        {
            // He has to have at least one Double Ticket available
            if (!ActorHasEnoughTickets(0, TicketKinds.Double, 1, out ReturnCode<bool> hasResult))
            {
                useResult = hasResult;
                return false;
            }

            // Remove the Ticket
            TicketsManager.RemoveActorTickets(0, TicketKinds.Double, 1);

            useResult = new(true);
            return true;
        }

        public bool FantomMove(int tileIndex_Where, TicketKinds ticketKind,
            out ReturnCode<bool> moveResult)
        {

            if (ticketKind == TicketKinds.Double || ticketKind == TicketKinds.Balloon)
            {
                moveResult = new(false, ReturnCodes.Fail,
                    $"The given Ticket kind cannot be used currently.");
                return false;
            }

            // Index checking
            if (!CheckBoardTileIndex(tileIndex_Where, out ReturnCode<bool> indexResult))
            {
                moveResult = indexResult;
                return false;
            }

            if (!IsMoveValid(0, tileIndex_Where, ticketKind, out ReturnCode<bool> validResult))
            {
                moveResult = validResult;
                return false;
            }


            // Take the Ticket away
            TicketsManager.RemoveActorTickets(0, ticketKind, 1);

            // Move the Fantom 
            ActorsManager.Move(0, tileIndex_Where);

            moveResult = new(true);
            return true;

        }

        // Seekers tools ----------------------------------------------------------------

        public bool SeekerMove(int seekerIndex, int tileIndex_Where, TicketKinds ticketKind,
            out ReturnCode<bool> moveResult)
        {
            if (ticketKind != TicketKinds.Mode1 && ticketKind != TicketKinds.Mode2 && ticketKind != TicketKinds.Mode3)
            {
                moveResult = new(false, ReturnCodes.BadArgument,
                    $"Seekers can only use the basic 3 Ticket kinds, not {ticketKind}.");
                return false;
            }

            if (HasMoved(seekerIndex))
            {
                moveResult = new(false, ReturnCodes.BadArgument,
                    $"Seeker {seekerIndex} has already moved this Round.");
                return false;
            }

            // Check indices
            if (!CheckSeekerIndex(seekerIndex, out ReturnCode<bool> seekerResult))
            {
                moveResult = seekerResult;
                return false;
            }

            if (!CheckBoardTileIndex(tileIndex_Where, out ReturnCode<bool> indexResult))
            {
                moveResult = indexResult;
                return false;
            }


            if (!IsMoveValid(1 + seekerIndex, tileIndex_Where, ticketKind, out ReturnCode<bool> validResult))
            {
                moveResult = validResult;
                return false;
            }

            // Move to the spot
            ActorsManager.Move(1 + seekerIndex, tileIndex_Where);
            Moved(seekerIndex);


            // Detective Ticket handling
            if (seekerIndex < DetectivesCount)
            {
                // Remove own Ticket
                TicketsManager.RemoveActorTickets(1 + seekerIndex, ticketKind, 1);
                // Give Ticket to Fantom if Detective
                TicketsManager.AddActorTickets(0, ticketKind, 1);
            }

            moveResult = new(true);
            return true;
        }


        // Actor Movement Utils ------------------------------------------------

        public bool IsMoveValid(int actorIndex, int tileIndex_Where, TicketKinds ticketKind,
            out ReturnCode<bool> moveResult)
        {

            if (actorIndex == 0)
            {
                // Fantom can only move to a free tile
                if (ActorsManager.IsActorAt(tileIndex_Where))
                {
                    moveResult = new(false, ReturnCodes.BadArgument,
                        $"Cannot move Fantom to a position {tileIndex_Where} with a Seeker on it.");
                    return false;
                }
            }
            else
            {
                // Seekers can move to free tiles or the tile with Fantom
                if (ActorsManager.IsActorAt(tileIndex_Where) && ActorsManager.GetActorAt(tileIndex_Where) != 0)
                {
                    moveResult = new(false, ReturnCodes.BadArgument,
                        $"Cannot move Seeker {actorIndex - 1} to a position {tileIndex_Where} with another Seeker {ActorsManager.GetActorAt(tileIndex_Where) - 1} on it.");
                    return false;
                }
            }

            // If not Bobby, has to have that Ticket available
            if (actorIndex - 1 < DetectivesCount)
            {
                if (!ActorHasEnoughTickets(actorIndex, ticketKind, 1, out ReturnCode<bool> hasResult))
                {
                    moveResult = hasResult;
                    return false;
                }
            }

            // Always have to have a connection via the given Kind
            if (ticketKind == TicketKinds.Black)
            {
                // Black Ticket -> Can use any transport
                if (!BoardManager.IsNeighbor(ActorsManager.GetTileOf(actorIndex), tileIndex_Where))
                {
                    moveResult = new(false, ReturnCodes.BoardTilesNotConnected,
                        $"Black Ticket cannot be used as tiles {ActorsManager.GetTileOf(actorIndex)} and {tileIndex_Where} are not connected.");
                    return false;
                }
            }
            else
            {
                // Mode Tickets -> Can use that transport
                if (!BoardManager.IsNeighbor(TicketKindToTravelMode(ticketKind), ActorsManager.GetTileOf(actorIndex), tileIndex_Where))
                {
                    moveResult = new(false, ReturnCodes.BoardTilesNotConnected,
                        $"Ticket {ticketKind} cannot be used as tiles {ActorsManager.GetTileOf(actorIndex)} and {tileIndex_Where} are not connected via this Mode.");
                    return false;
                }
            }

            moveResult = new(true);
            return true;

        }

        public static TravelModes TicketKindToTravelMode(TicketKinds ticketKind)
        {
            return ticketKind switch
            {
                TicketKinds.Mode1 => TravelModes.Mode1,
                TicketKinds.Mode2 => TravelModes.Mode2,
                TicketKinds.Mode3 => TravelModes.Mode3,
                TicketKinds.River => TravelModes.River,
                _ => TravelModes.Mode1,
            };
        }

    }
}
