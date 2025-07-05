using FantomGamesCore;
using FantomGamesIntermediary.Opponent.Interfaces;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent.Parts.FantomParts
{
    internal unsafe struct FantomState : IActorState<FantomState, FantomMove>
    {

        private int _fantomPosition;
        private fixed int _fantomTickets[FantomGameSettings.TICKET_KINDS_COUNT];

        private bool _knowsSeekerPositions;
        private fixed int _seekerPositions[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];

        public uint MoveNumber { get; private set; }

        // the passed state will be private state, so will have Fantom position
        public static FantomState FromState(FantomGameState state)
        {
            FantomState result = new()
            {
                _fantomPosition = state.FantomPosition,
                _knowsSeekerPositions = true,
                MoveNumber = state.CurrentMove
            };

            for (int ticketKind = 0; ticketKind < FantomGameSettings.TICKET_KINDS_COUNT; ++ticketKind)
                result._fantomTickets[ticketKind] = state.GetFantomTickets((TicketKinds)ticketKind);

            for (int seekerIndex = 0; seekerIndex < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++seekerIndex)
                result._seekerPositions[seekerIndex] = state.GetSeekerPosition(seekerIndex);

            // position has to be known
            Debug.Assert(result._fantomPosition > 0);

            return result;
        }

        public static FantomState operator +(FantomState state, FantomMove move)
        {
            var fantomPosition = move.IsDouble ? move.GetDestination(1) : move.GetDestination(0);
            Debug.Assert(fantomPosition != null);

            FantomState result = new()
            {
                _fantomPosition = fantomPosition.Value,
                _knowsSeekerPositions = false,
                MoveNumber = state.MoveNumber + (move.IsDouble ? 1u : 2u)
            };

            // copy Tickets 
            for (int ticketKind = 0; ticketKind < FantomGameSettings.TICKET_KINDS_COUNT; ++ticketKind)
                result._fantomTickets[ticketKind] = state._fantomTickets[ticketKind];

            // remove the move Tickets
            if(move.Moved(0))
                result._fantomTickets[move.GetTicketAsInt(0)]--;

            if(move.IsDouble)
            {
                result._fantomTickets[(int) TicketKinds.Double]--;

                // NOTE: should be fine, but if double, then on second run out ?
                if (move.Moved(1))
                    result._fantomTickets[move.GetTicketAsInt(1)]--;
            }

            return result;
        }


        public bool IsSafe(int tileIndex)
        {
            // If doesn't know the positions exactly, then might be safe, who knows
            if (!_knowsSeekerPositions)
                return true;

            // If knows the positions and some seeker is at it, definitely not safe
            for (int seekerIndex = 0; seekerIndex < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++seekerIndex)
            {
                if (_seekerPositions[seekerIndex] == tileIndex)
                    return false;
            }

            return true;
        }

        public readonly int GetTickets(TicketKinds ticketKinds)
        {
            return _fantomTickets[(int)ticketKinds];
        }

        public readonly int GetPosition()
        {
            return _fantomPosition;
        }

    }
}
