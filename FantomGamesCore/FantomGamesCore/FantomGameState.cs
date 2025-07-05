namespace FantomGamesCore
{
    /// <summary>
    /// An unsafe struct representing the current game's state. Contains information about the number of seekers,
    /// their tickets, the round number etc.
    /// </summary>
    public unsafe struct FantomGameState
    {
        /// <summary>
        /// The number of detectives in play right now.
        /// </summary>
        public int DetectivesCount { get; init; }
        /// <summary>
        /// The number of bobbies in play right now.
        /// </summary>
        public int BobbiesCount { get; init; }
        /// <summary>
        /// The number of Detectives and Bobbies in play right now.
        /// </summary>
        public readonly int SeekersCount => DetectivesCount + BobbiesCount;


        // There are at Most 5 enemy Seekers
        private fixed int SeekerPositions[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];

        /// <summary>
        /// Fantom's position if known will be a positive integer. If not known, will most likely be -1.
        /// </summary>
        public readonly int FantomPosition { get; init; }
        /// <summary>
        /// Fantom's last revealed position if known will be a positive integer. If not known, will most likely be -1.
        /// </summary>
        public readonly int FantomLastKnownPosition { get; init; }
        
        /// <summary>
        /// The number of the current round.
        /// </summary>
        public required uint CurrentRound { get; init; }
        /// <summary>
        /// The number of done Fantom's moves.
        /// </summary>
        public required uint CurrentMove { get; init; }


        // The Detectives can only have the three main Mode Tickets
        // Indexing via detectiveIndex*3 + kind
        private fixed int DetectiveTickets[FantomGameSettings.MAXIMUM_SEEKERS_COUNT * 3];
        private fixed int FantomTickets[FantomGameSettings.TICKET_KINDS_COUNT];

        //public required int[] SupplyTickets { get; init; }
        private fixed int SupplyTickets[FantomGameSettings.TICKET_KINDS_COUNT];

        /// <summary>
        /// Get the number of tickets the Fantom is currently holding.
        /// </summary>
        /// <param name="ticketKind">The kind of the tickets in question.</param>
        /// <returns>The number of the specified tickets the Fantom is holding.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when given an unsupported ticket kinds enum index</exception>
        public readonly int GetFantomTickets(TicketKinds ticketKind)
        {
            var i = (int)ticketKind;
            if(i < 0 || i >= FantomGameSettings.TICKET_KINDS_COUNT)
                throw new IndexOutOfRangeException($"Trying to access Fantom Tickets out of allowed range with {i} / {ticketKind}.");

            return FantomTickets[i];
        }

        /// <summary>
        /// Get the number of tickets for the specified detective. 
        /// </summary>
        /// <param name="detectiveIndex">The specified detective.</param>
        /// <param name="ticketKinds">The kind of the tickets in question.</param>
        /// <returns>The number of specific tickets held by that detective.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly int GetDetectiveTickets(int detectiveIndex, TicketKinds ticketKinds)
        {
            var i = (int)ticketKinds;
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException($"Trying to access Detective Tickets out of allowed range with {i}.");

            if(detectiveIndex < 0 || detectiveIndex >= DetectivesCount)
                throw new IndexOutOfRangeException($"Trying to access Detective Tickets for a non-existent Detective {i}.");


            return DetectiveTickets[detectiveIndex*3 + i];
        }

        /// <summary>
        /// Get the position of the specified Seeker. The indexing starts at 0 and goes up to SeekerIndex exclusive. The first DetectiveCount indices correspond to the detectives in play, the remaining ones are for Bobbies. If the returned value is not positive, that that index is currently invalid.
        /// </summary>
        /// <param name="seekerIndex">Index of the Seeker in question</param>
        /// <returns>The position of the Seeker in question.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly int GetSeekerPosition(int seekerIndex)
        {
            if (seekerIndex < 0 || seekerIndex >= FantomGameSettings.MAXIMUM_SEEKERS_COUNT)
                throw new IndexOutOfRangeException($"Trying to access Seeker Positions out of allowed range with {seekerIndex}.");
            
            return SeekerPositions[seekerIndex];
        }

        /// <summary>
        /// Get the number of tickets in the supply.
        /// </summary>
        /// <param name="ticketKinds">The type of ticket in question.</param>
        /// <returns>The number of tickets of the specified type in the supply currently.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly int GetSupplyTickets(TicketKinds ticketKinds)
        {
            var i = (int)ticketKinds;
            if (i < 0 || i >= FantomGameSettings.TICKET_KINDS_COUNT)
                throw new IndexOutOfRangeException($"Trying to access Supply Tickets out of allowed range with {i}.");

            return SupplyTickets[i];
        }

        /// <summary>
        /// Copy held ticket counts of the Fantom into the passed array. The array's length must be 7. The ticket indices correspond to the TicketKinds enum int values.
        /// </summary> 
        /// <param name="FantomTicketsDestination">The array to be copied into.</param>
        /// <exception cref="ArgumentException"></exception>
        public readonly void CopyFantomTicketsTo(int[] FantomTicketsDestination)
        {
            if (FantomTicketsDestination.Length != FantomGameSettings.TICKET_KINDS_COUNT)
                throw new ArgumentException($"Fantom Array must have enough space for all Ticket kinds. Got {FantomTicketsDestination.Length}.");

            for (int i = 0; i < FantomTicketsDestination.Length; ++i)
                FantomTicketsDestination[i] = FantomTickets[i];
        }

        /// <summary>
        /// Copy held detective tickets into the passed array. The array's dimensions must be DetectivesCount x 3. The indices are for each detective in game as the first, and the ticket kind (0, 1, 2) for transport tickets as the second.
        /// </summary>
        /// <param name="DetectiveTicketsDestination"></param>
        /// <exception cref="ArgumentException"></exception>
        public readonly void CopyDetectiveTicketsTo(int[,] DetectiveTicketsDestination)
        {
            if (DetectiveTicketsDestination.GetLength(0) != DetectivesCount || 
                DetectiveTicketsDestination.GetLength(1) != 3)
                throw new ArgumentException($"Detective Array must have enough space for all Ticket kinds. Got {DetectiveTicketsDestination.GetLength(0)} x {DetectiveTicketsDestination.GetLength(1)}.");

            for (int i = 0; i < DetectivesCount; ++i)
            {
                DetectiveTicketsDestination[i, 0] = DetectiveTickets[i * 3 + 0];
                DetectiveTicketsDestination[i, 1] = DetectiveTickets[i * 3 + 1];
                DetectiveTicketsDestination[i, 2] = DetectiveTickets[i * 3 + 2];
            }

        }

        /// <summary>
        /// Used to fill out appropriate information for the current game state.
        /// </summary>
        /// <param name="DetectivesCount">The number of Detectives in game</param>
        /// <param name="BobbiesCount">The number of Bobbies in game.</param>
        /// <param name="SeekerPositions">The positions of all Seekers.</param>
        /// <param name="FantomPosition">The position of the Fantom - null if not allowed (known)</param>
        /// <param name="FantomLastKnownPosition">Fantom's last known position - null if not yet revealed</param>
        /// <param name="ActorTickets">The tickets for all actors.</param>
        /// <param name="SupplyTickets">The tickets for the supply.</param>
        public FantomGameState(
            int DetectivesCount,
            int BobbiesCount,

            int[] SeekerPositions,            
            int? FantomPosition,
            int? FantomLastKnownPosition,

            int[,] ActorTickets,
            int[] SupplyTickets)
        {
            // Detectives and Bobbies Count are checked via settings when loading, they should be fine
            this.DetectivesCount = DetectivesCount;
            this.BobbiesCount = BobbiesCount;

            // Set currently playing Seekers' Positions (might be fewer than 5)
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                if (i < DetectivesCount+BobbiesCount)
                {
                    this.SeekerPositions[i] = SeekerPositions[i];
                }
                else
                {
                    this.SeekerPositions[i] = -1;
                }
            }

            // Fantom Positions
            this.FantomPosition = FantomPosition ?? -1;
            this.FantomLastKnownPosition = FantomLastKnownPosition ?? -1;

            // Set Fantom Tickets
            for (int i = 0; i < FantomGameSettings.TICKET_KINDS_COUNT; ++i)
            {
                FantomTickets[i] = ActorTickets[0, i];
                this.SupplyTickets[i] = SupplyTickets[i];
            }

            // Set currently playing Detectives' Tickets, Bobbies don't have them!
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            { 
                if (i < DetectivesCount)
                {
                    DetectiveTickets[i * 3 + 0] = ActorTickets[1+i, (int) TicketKinds.Mode1];
                    DetectiveTickets[i * 3 + 1] = ActorTickets[1+i, (int) TicketKinds.Mode2];
                    DetectiveTickets[i * 3 + 2] = ActorTickets[1+i, (int) TicketKinds.Mode3];
                } else
                {
                    DetectiveTickets[i * 3 + 0] = -1;
                    DetectiveTickets[i * 3 + 1] = -1;
                    DetectiveTickets[i * 3 + 2] = -1;
                }
            }
        }

        
    }
}
