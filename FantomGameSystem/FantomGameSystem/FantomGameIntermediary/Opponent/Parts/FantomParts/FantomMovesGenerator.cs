using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent.Parts.FantomParts
{
    internal struct FantomMovesGenerator(
        IReadOnlyFantomBoard _fantomBoard,
        IEnumerable<uint> revealingMoves) : ILegalMovesGenerator<FantomMove, FantomState>
    {

        // 
        readonly uint[] _revealingMoves = revealingMoves.ToArray();
        static readonly (TravelModes, TicketKinds)[] FANTOM_MOVES =
            {
              (TravelModes.Mode1, TicketKinds.Mode1), (TravelModes.Mode1, TicketKinds.Black),
              (TravelModes.Mode2, TicketKinds.Mode2), (TravelModes.Mode2, TicketKinds.Black),
              (TravelModes.Mode3, TicketKinds.Mode3), (TravelModes.Mode3, TicketKinds.Black),
              (TravelModes.River, TicketKinds.River), (TravelModes.River, TicketKinds.Black)
            };

        public readonly bool IsLegal(FantomState state, FantomMove move)
        {
            // confirm that the move leads to positions that are Seeker-free if the state knows them for sure

            var destination1 = move.GetDestination(0);
            // Generating moves for the Fantom, always will know the destination
            Debug.Assert(destination1 != null);

            if (move.IsDouble)
            {
                var destination2 = move.GetDestination(0);
                Debug.Assert(destination2 != null);
                
                return state.IsSafe(destination1.Value) && state.IsSafe(destination2.Value);
            } 
            else
            {
                return state.IsSafe(destination1.Value);
            }
        }

        // TODO: possibly no Move (theoretically game over, or brain default will take care of it) 
        // NOTE: ^^ not as bad as with Seekers

        public readonly IEnumerable<FantomMove> PossibleMoves(FantomState state)
        {
            // State knows which positions are blocked by the Seekers now, Fantom cannot even try to move to those
            // But in later rounds, the positions won't necessarily be blocked, so ignore them when generating

            // Go over all Modes of travel, If he has that Ticket type or a Black Ticket too 

            // If has Double, do the same again            
            
            
            bool[] willReveal = {
                _revealingMoves.Contains(state.MoveNumber + 1),
                _revealingMoves.Contains(state.MoveNumber + 2) 
            };

            bool[] moved = new bool[2];

            int[] tickets =
            {
                    state.GetTickets(TicketKinds.Mode1),
                    state.GetTickets(TicketKinds.Mode2),
                    state.GetTickets(TicketKinds.Mode3),
                    state.GetTickets(TicketKinds.Black),
                    state.GetTickets(TicketKinds.Double),
                    state.GetTickets(TicketKinds.River),                    
                };

            // First move generation
            foreach (var (mode1, ticket1) in FANTOM_MOVES)
            {
                // Can afford this mode via this ticket
                if (tickets[(int)ticket1] > 0)
                {

                    // Go over all tiles connected via this mode to current fantom's position
                    var neighborsCount = _fantomBoard.CountNeighbors(mode1, state.GetPosition());
                    for (int neighborIndex = 0; neighborIndex < neighborsCount; ++neighborIndex)
                    {
                        var first_destination = _fantomBoard.GetNeighbor(mode1, state.GetPosition(), neighborIndex);

                        // This position is 'currently' occupied by a Seeker, so definitely cannot Move to it
                        if (!state.IsSafe(first_destination))
                            continue;

                        // yield this single move
                        yield return new FantomMove(
                                first_destination,
                                true,
                                ticket1,                                
                                willReveal[0]);


                        // Try to add second Moves to this one
                        // If has Double tickets
                        if (tickets[(int)TicketKinds.Double] > 0)
                        {
                            // simulate using this Ticket for the Double Move
                            tickets[(int)ticket1]--;

                            // Second move generation
                            foreach (var (mode2, ticket2) in FANTOM_MOVES)
                            {
                                // Can afford the second move
                                if (tickets[(int)ticket2] > 0)
                                {

                                    // Go over all tiles connected via this mode to fantom's moved position
                                    var neighborsCount2 = _fantomBoard.CountNeighbors(mode2, first_destination);
                                    for (int neighborIndex2 = 0; neighborIndex2 < neighborsCount2; ++neighborIndex2)
                                    {
                                        var second_destination = _fantomBoard.GetNeighbor(mode2, first_destination, neighborIndex2);

                                        // The destination is seeker-free
                                        if (!state.IsSafe(second_destination))
                                            continue;

                                        // yield double move 
                                        yield return new FantomMove(
                                                first_destination,
                                                true,
                                                ticket1,
                                                willReveal[0],
                                                second_destination,
                                                true,
                                                ticket2,
                                                willReveal[1]);
                                    }
                                }

                                // revert the Ticket use
                                tickets[(int)ticket1]++;
                            }
                        }
                    }
                }
            }
        }
    }
}
