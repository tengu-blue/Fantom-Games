using System.Runtime.CompilerServices;

namespace FantomGamesIntermediary.Opponent.Interfaces
{

    /// <summary>
    /// Stores a sequence of MoveType's whilst also keeping track of each Moves' value.
    /// </summary>
    /// <typeparam name="MoveType"></typeparam>
    public class MovesSequence<MoveType>
        where MoveType : struct
    {
        const int MAX_MOVES_SEQUENCE = 6;

        readonly MoveType[] moves = new MoveType[MAX_MOVES_SEQUENCE];
        readonly float[] moveValues = new float[MAX_MOVES_SEQUENCE];

        public int CurrentMoves { get; private set; } = 0;

        public MovesSequence()
        {
            Array.Fill(moveValues, -1);
        }

        public void Reset()
        {
            Array.Clear(moves);
            Array.Fill(moveValues, -1);
            CurrentMoves = 0;
        }


        /// <summary>
        /// Sets the specified level's Move and value, all Moves in Sequence after the given level are discarded.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="value"></param>
        /// <param name="level"></param>
        public void SetMove(MoveType move, float value, int level)
        {
            moves[level] = move;
            moveValues[level] = value;

            // the other moves that might have been in this sequence are no longer valid
            CurrentMoves = level + 1;
        }

        /// <summary>
        /// Copies the Moves and values from the given sequence to this one.
        /// </summary>
        /// <param name="otherSequence"></param>
        public void SetMoves(MovesSequence<MoveType> otherSequence)
        {

            for (int i = 0; i < otherSequence.CurrentMoves; ++i)
            {
                moves[i] = otherSequence.moves[i];
                moveValues[i] = otherSequence.moveValues[i];
            }

            // the other moves that might have been in this sequence are no longer valid
            CurrentMoves = otherSequence.CurrentMoves;
        }

        /// <summary>
        /// Gives the move from the specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public MoveType GetMove(int level)
        {
            return moves[level];
        }

        /// <summary>
        /// Gives the value of the move at the specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public float GetMoveValue(int level)
        {
            return moveValues[level];
        }

        /// <summary>
        /// Removes the move at the specified level and shifts the sequence.
        /// </summary>
        /// <param name="level"></param>
        public void RemoveMove(int level)
        {
            // want to remove a Move that is actually stored in
            if (level < CurrentMoves)
            {
                // shift all Moves to the left
                for (int i = level; i < moves.Length - 1; ++i)
                {
                    moves[i] = moves[i + 1];
                    moveValues[i] = moveValues[i + 1];
                }

                // last one delete (not necessary to remove actually but...)
                moves[^1] = new();
                moveValues[^1] = -1;

                CurrentMoves = Math.Max(0, --CurrentMoves);
            }
        }

        public override string ToString()
        {
            return moves[0].ToString()+" w. val: "+moveValues[0];
        }
    }
}
