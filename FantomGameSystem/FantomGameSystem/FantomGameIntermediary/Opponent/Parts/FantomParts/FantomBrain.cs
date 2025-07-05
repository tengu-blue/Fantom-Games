using FantomGamesCore;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesSystemUtils;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent.Parts.FantomParts
{
    internal struct FantomBrain(
        IFantomCommander _commander)

        : IOpponentBrain<FantomMove>
    {
        private object _brainLock = new();
        int _entryCounter = 0;

        ISafeBestMoveKeeper<FantomMove> _bestMovesSequence = new();

        // NOTE: done in this way to maximize code reuse but keep struct generic paste for performance        
        public void UpdateMoveSequence(float updatedWorstValue, float updatedBestValue, MovesSequence<FantomMove> proposedSequence) => _bestMovesSequence.UpdateMoveSequence(updatedWorstValue, updatedBestValue, proposedSequence);

        public bool CanSetWith(float newWorst, float proposedBest) => _bestMovesSequence.CanSetWith(newWorst, proposedBest);

        public bool CanSetWith(MovesSequence<FantomMove> proposedSequence) => _bestMovesSequence.CanSetWith(proposedSequence);

        public void SetBestMoveSequence(float newWorstValue, float newBestValue, MovesSequence<FantomMove> proposedSequence) => _bestMovesSequence.SetBestMoveSequence(newWorstValue, newBestValue, proposedSequence);

        public readonly bool HasMoved => _bestMovesSequence.HasMoved;

        public readonly MovesSequence<FantomMove> GetMovesSequence() => _bestMovesSequence.GetSequence();

        public void Reset() => _bestMovesSequence.Reset();

        // --------------------------------------------------------------------------------

        public void NewSearchSetup()
        {
            _bestMovesSequence.RemoveFirstMove();
        }

        public void MakeBestMove()
        {
            var code = _entryCounter;
            
            lock (_brainLock)
            {
                if (code != _entryCounter)
                    return;
                _entryCounter++;

                // Fantom has already played (called again accidentally probably)
                if (!_commander.IsFantomTurn())
                    return;

                FantomMove? move = null;
                // this locks the first move inside the sequence so that others cannot change it
                _bestMovesSequence.Moved();
                // if the best is better than -1, then must have made some Move next
                if (_bestMovesSequence.GetBestMoveValue() > 0)
                {
                    move = _bestMovesSequence.GetNextMove();
                }
                else
                {
                    Log("No Best Move for Fantom given!");
                }

                if (move is not null)
                {                    
                    Log($"Making a move as the Fantom - {move}");

                    var destination1 = move.Value.GetDestination(0);
                    Debug.Assert(destination1 != null);

                    if (move.Value.IsDouble)
                    {                        
                        var destination2 = move.Value.GetDestination(1);    
                        Debug.Assert(destination2 != null);

                        _commander.UseDouble();
                        _commander.Move(destination1.Value, move.Value.GetTicketUsed(0));
                        _commander.Move(destination2.Value, move.Value.GetTicketUsed(1));
                    } 
                    else
                    {
                        _commander.Move(destination1.Value, move.Value.GetTicketUsed(0));
                    }

                    // whatever move tried to make, it failed
                    if (!_commander.ConfirmTurnOver())
                        MakeDefaultMove();
                }
                // No best given, so default
                else
                {
                    MakeDefaultMove();
                }
            }
        }

        private static TicketKinds[] FANTOM_TICKET_TRIALS = [TicketKinds.Mode1, TicketKinds.Mode2, TicketKinds.Mode3, TicketKinds.River, TicketKinds.Black];

        private void MakeDefaultMove()
        {
            Log("Defaulting Fantom");

            int? possiblePosition = _commander.CannotMove();
            if(possiblePosition is not null)
            {
                // just brute force try to Move to that tile somehow (preferring cheaper Tickets)
                foreach(var ticket in FANTOM_TICKET_TRIALS)
                    // try to move there via this mode
                    if (_commander.Move(possiblePosition.Value, ticket))
                    {                        
                        break;
                    }
                
            }

            if (!_commander.ConfirmTurnOver())
                Log("Cannot Move or NotMove as Fantom!");
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
