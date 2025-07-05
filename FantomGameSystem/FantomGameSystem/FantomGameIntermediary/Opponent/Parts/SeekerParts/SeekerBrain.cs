using FantomGamesCore;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesSystemUtils;

namespace FantomGamesIntermediary.Opponent.Parts.SeekerParts
{
    internal struct SeekerBrain(
        ISeekersCommander _commander,
        int _detectivesCount,
        int _bobbiesCount
        )

        : IOpponentBrain<SeekersMove>
    {

        private object _brainLock = new();
        private int _entryCounter = 0;

        ISafeBestMoveKeeper<SeekersMove> _bestMovesSequence = new();

        // NOTE: done in this way to maximize code reuse but keep struct generic paste for performance        
        public void UpdateMoveSequence(float updatedWorstValue, float updatedBestValue, MovesSequence<SeekersMove> proposedSequence) => _bestMovesSequence.UpdateMoveSequence(updatedWorstValue, updatedBestValue, proposedSequence);

        public bool CanSetWith(float newWorst, float proposedBest) => _bestMovesSequence.CanSetWith(newWorst, proposedBest);

        public bool CanSetWith(MovesSequence<SeekersMove> proposedSequence) => _bestMovesSequence.CanSetWith(proposedSequence);

        public void SetBestMoveSequence(float newWorstValue, float newBestValue, MovesSequence<SeekersMove> proposedSequence) => _bestMovesSequence.SetBestMoveSequence(newWorstValue, newBestValue, proposedSequence);

        public readonly bool HasMoved => _bestMovesSequence.HasMoved;

        public readonly MovesSequence<SeekersMove> GetMovesSequence() => _bestMovesSequence.GetSequence();

        public void Reset() => _bestMovesSequence.Reset();

        // --------------------------------------------------------------------------------

        public void NewSearchSetup()
        {
            _bestMovesSequence.RemoveFirstMove();
        }

        public void MakeBestMove()
        {
            int code = _entryCounter;

            lock (_brainLock)
            {
                if (code != _entryCounter)
                    return;
                _entryCounter++;

                // Seekers have already played 
                if (!_commander.IsSeekersTurn())
                    return;

                SeekersMove? move = null;
                // this locks the first move inside the sequence so that others cannot change it
                _bestMovesSequence.Moved();
                // if the best is better than -1, then must have made some Move next
                if (_bestMovesSequence.GetBestMoveValue() > 0)
                {
                    move = _bestMovesSequence.GetNextMove();
                }
                else
                {
                    Log("No Best Move for Seekers given!");
                }

                if (move is not null)
                {
                    if (!_commander.IsSeekersTurn())
                        return;

                    Log($"Making a move as Seekers - {move}");

                    for (int seekerIndex = 0; seekerIndex < _detectivesCount + _bobbiesCount; ++seekerIndex)
                    {
                        // whatever Moves we have made, the turn ended or the game is over now; stop
                        if (!_commander.IsSeekersTurn())
                        {
                            Console.WriteLine("Done moving ahead of time.");
                            return;
                        }

                        if (move.Value.Moved(seekerIndex))
                        {
                            _commander.Move(
                                seekerIndex,
                                move.Value.GetDestination(seekerIndex),
                                move.Value.GetTicketUsed(seekerIndex));
                        } else
                        {
                            _commander.CannotMove();
                        }
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

        private void MakeDefaultMove()
        {
            Log("Defaulting Seekers");

            // NOTE: position might be null now for the seekerIndex but valid later
            // so need to try again, if have managed to move in the previous one
            // if no Move before and no Move now, yet still not done moving, then error somewhere 
            bool moved = false;
            for (int seekerIndex = 0; seekerIndex < _detectivesCount + _bobbiesCount; ++seekerIndex)
            {
                // Game might be over already
                if (!_commander.IsSeekersTurn())
                    return;

                int? position = _commander.CannotMove();

                // found 
                if (position.HasValue)
                {
                    // just brute force try to Move to that tile somehow (preferring cheaper Tickets)
                    for (int mode = 0; mode < 3; ++mode)
                    {
                        // try to move there via this mode
                        if (_commander.Move(seekerIndex, position.Value, (TicketKinds)mode))
                        {
                            moved = true;
                            break;
                        }
                    }
                }
            }

            // Confirm that the Seeker is done playing
            if (_commander.ConfirmTurnOver() && moved)
            // try again (see note above)
            {
                MakeDefaultMove();
            }
            else
            {
                Log("No way to continue, cannot Move or NotMove");
            }

        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}