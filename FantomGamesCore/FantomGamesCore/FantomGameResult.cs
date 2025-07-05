using System.Text;

namespace FantomGamesCore
{
    /// <summary>
    /// Contains the result of the game - the winner as well as the actual conditions that led to the game over.
    /// </summary>
    public struct FantomGameResult
    {
        /// <summary>
        /// True if Seekers won the game.
        /// </summary>
        public bool SeekersWon { get; init; }
        /// <summary>
        /// True if the Fantom won the game.
        /// </summary>
        public bool FantomWon { get; init; }
        /// <summary>
        /// True if the game ended in a draw.
        /// </summary>
        public bool GameDraw { get; init; }

        /// <summary>
        /// The actual conditions that led to the game over.
        /// </summary>
        public GameOverConditions[] trueWinningConditions;

        /// <summary>
        /// String representation of the winning conditions.
        /// </summary>
        public readonly string WinningCondition
        {
            get
            {
                StringBuilder sb = new();
                bool a = false;

                foreach (var clause in trueWinningConditions)
                {
                    if (a)
                        sb.Append(" & ");

                    sb.Append('(');


                    bool b = false;
                    foreach (var condition in Enum.GetValues<GameOverConditions>())
                    {
                        if (condition == GameOverConditions.None)
                            continue;

                        if (clause.HasFlag(condition))
                        {
                            if (b)
                                sb.Append(" | ");

                            sb.Append(Enum.GetName(condition));
                            b = true;
                        }
                    }
                    sb.Append(')');
                    a = true;
                }

                return sb.ToString();
            }
        }
    }
}
