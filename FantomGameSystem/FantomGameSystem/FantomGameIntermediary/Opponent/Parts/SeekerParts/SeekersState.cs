using FantomGamesCore;
using FantomGamesIntermediary.Opponent.Interfaces;

namespace FantomGamesIntermediary.Opponent.Parts.SeekerParts
{
    internal unsafe struct SeekersState : IActorState<SeekersState, SeekersMove>
    {

        public SeekersState()
        {
            // Default just invalidate
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                _seekerPositions[i] = -1;
                _detectiveTickets[3 * i + 0] = -1;
                _detectiveTickets[3 * i + 1] = -1;
                _detectiveTickets[3 * i + 2] = -1;
            }
        }

        private fixed int _seekerPositions[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];
        private fixed int _detectiveTickets[FantomGameSettings.MAXIMUM_SEEKERS_COUNT * 3];

        public static SeekersState FromState(FantomGameState state)
        {
            SeekersState result = new();
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                if (i < state.SeekersCount)
                {
                    result._seekerPositions[i] = state.GetSeekerPosition(i);
                    if (i < state.DetectivesCount)
                    {
                        result._detectiveTickets[3 * i + 0] = state.GetDetectiveTickets(i, TicketKinds.Mode1);
                        result._detectiveTickets[3 * i + 1] = state.GetDetectiveTickets(i, TicketKinds.Mode2);
                        result._detectiveTickets[3 * i + 2] = state.GetDetectiveTickets(i, TicketKinds.Mode3);
                    }
                }
            }
            return result;
        }

        static SeekersState IActorState<SeekersState, SeekersMove>.operator +(SeekersState state, SeekersMove move)
        {
            SeekersState result = new();
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                // this seeker index is valid
                if (state.GetSeekerPosition(i) != -1)
                {
                    result._seekerPositions[i] = move.GetDestination(i);
                    if (state.GetDetectiveTickets(i, TicketKinds.Mode1) != -1)
                    {
                        result._detectiveTickets[3 * i + 0] = state.GetDetectiveTickets(i, TicketKinds.Mode1);
                        result._detectiveTickets[3 * i + 1] = state.GetDetectiveTickets(i, TicketKinds.Mode2);
                        result._detectiveTickets[3 * i + 2] = state.GetDetectiveTickets(i, TicketKinds.Mode3);

                        // Wasn't a bobby -> use the Ticket
                        // Did actually move (didn't just stay)
                        if (move.Moved(i))
                            result._detectiveTickets[3 * i + move.GetTicketAsInt(i)]--;
                    }
                }
                // done setting useful data
                else
                {
                    break;
                }
            }
            return result;
        }

        // -----------------------------------

        // no index checks for speed

        public readonly int GetDetectiveTickets(int detectiveIndex, TicketKinds ticketKinds)
        {
            return _detectiveTickets[detectiveIndex * 3 + (int)ticketKinds];
        }

        public readonly int GetSeekerPosition(int seekerIndex)
        {
            return _seekerPositions[seekerIndex];
        }
    }
}
