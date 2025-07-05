using System.Text;

namespace FantomGamesIntermediary.Opponent.Interfaces
{
    internal struct ISafeBestMoveKeeper<MoveType>()
        where MoveType : struct, IActorMove<MoveType>
    {

        MovesSequence<MoveType> _bestSequence = new();
        private readonly object _sequenceLock = new();

        float _bestMoveValue = float.MinValue;
        float _worstMoveValue = float.MaxValue;

        public bool HasMoved { get; private set; }

        public readonly MovesSequence<MoveType> GetSequence()
        {
            return _bestSequence;
        }

        public readonly float GetBestMoveValue()
        {
            return _bestMoveValue;
        }

        public MoveType GetNextMove()
        {
            lock (_sequenceLock)
            {
                return _bestSequence.GetMove(0);
            }
        }

        public bool CanSetWith(float newWorst, float proposedBest)
        {
            // We care about better Moves, or equally good Moves that are safer
            return proposedBest > _bestMoveValue || (proposedBest == _bestMoveValue && newWorst > _worstMoveValue);
        }

        public bool CanSetWith(MovesSequence<MoveType> proposedSequence)
        {
            // most of the time, has Moved will be False,
            // then we require the 
            return !HasMoved || proposedSequence.GetMove(0) == GetNextMove();
        }

        public void UpdateMoveSequence(float updatedWorstValue, float updatedBestValue, MovesSequence<MoveType> proposedSequence)
        {
            lock (_sequenceLock)
            {
                _worstMoveValue = updatedWorstValue;
                _bestMoveValue = updatedBestValue;
                // only update if has not moved or the first move of the sequence matches
                if (CanSetWith(proposedSequence))
                    _bestSequence.SetMoves(proposedSequence);
            }
        }

        public void SetBestMoveSequence(float newWorstValue, float newBestValue, MovesSequence<MoveType> proposedSequence)
        {
            lock (_sequenceLock)
            {
                if (CanSetWith(newWorstValue, newBestValue) && CanSetWith(proposedSequence))
                {
                    _worstMoveValue = newWorstValue;
                    _bestMoveValue = newBestValue;
                    _bestSequence.SetMoves(proposedSequence);
                    Console.WriteLine(_bestSequence);
                }
            }
        }

        public void Moved()
        {
            HasMoved = true;
        }

        public void RemoveFirstMove()
        {
            // NOTE: should only be called from one thread ever, but still lock
            lock (_sequenceLock)
            {
                _bestSequence.RemoveMove(0);
                HasMoved = false;
            }
        }

        public void Reset()
        {
            _bestSequence.Reset();
            HasMoved = false;
            _bestMoveValue = -1;
        }
       
    }
}
