using FantomGamesCore.Interfaces;
using FantomGamesCore.Managers;

namespace FantomGamesCore
{

    internal enum GameResolutionStates
    {
        Playing,

        SeekersWon,
        FantomWon,

        Draw
    }

    /// <summary>
    /// Contains things that change during gameplay for organization purposes.
    /// 
    /// </summary>
    internal class FantomGameContext(IReadOnlyFantomBoard BoardManager)
    {

        public IReadOnlyFantomBoard BoardManager { get; private set; } = BoardManager;
        public required ActorsManager ActorsManager { get; init; }
        public required TicketsManager TicketsManager { get; init; }
        public required FantomGameManager GameManager { get; init; }

        public void Restart(
            IReadOnlyFantomBoard BoardManager
            )
        {
            this.BoardManager = BoardManager;
        }


        // Actor indexing ------------------------------------------------

        /// <summary>
        /// Whether order of Seekers is significant.
        /// </summary>
        public required bool KeepOrder { private get; init; }

        /// <summary>
        /// The total number of Detectives and Bobbies. CurrentSeekerIndex is between 0 and this number.
        /// </summary>
        public required int SeekersCount { private get; init; }

        /// <summary>
        /// The number of Detectives.
        /// </summary>
        public required int DetectivesCount { private get; init; }

        /// <summary>
        /// Which Fantom Moves are Revealing
        /// </summary>
        public required uint[] RevealingMoves { private get; init; }

        /// <summary>
        /// The current Detective if that makes sense with current settings.
        /// </summary>
        public int CurrentSeekerIndex { get; private set; } = 0;


        /// <summary>
        /// Checks if the given Seeker is currently playing. Returns true if ordering is disabled.
        /// </summary>
        /// <param name="seekerIndex"></param>
        /// <returns></returns>
        public bool IsThisSeekersTurn(int seekerIndex)
        {
            return !KeepOrder || CurrentSeekerIndex == seekerIndex;
        }

        public bool IsActorBobby(int actorIndex)
        {
            return (actorIndex - 1) >= DetectivesCount;
        }

        public bool IsActorSeeker(int actorIndex)
        {
            return actorIndex != 0;
        }

        public bool IsActorFantom(int actorIndex)
        {
            return actorIndex == 0;
        }

        public bool IsValidSeekerIndex(int seekerIndex)
        {
            return seekerIndex >= 0 && seekerIndex < SeekersCount;
        }

        private readonly TicketKinds[] ModeTickets = [TicketKinds.Mode1, TicketKinds.Mode2, TicketKinds.Mode3, TicketKinds.River];

        public bool CanActorMove(int actorIndex, TicketKinds ticketKind, out ReturnCode<int> possibleMove)
        {
            var m = RoundContextManager.TicketKindToTravelMode(ticketKind);
            var t = ActorsManager.GetTileOf(actorIndex);

            // If the specified Actor isn't on the board, return false
            if (ActorsManager.IsActorOnBoard(actorIndex))
            {
                var c = BoardManager.CountNeighbors(m, t);
                for (int i = 0; i < c; ++i)
                {
                    // Fantom can move to empty only; or
                    // Seekers can move to empty and to Fantom
                    if (!ActorsManager.IsActorAt(BoardManager.GetNeighbor(m, t, i)) ||
                        (IsActorSeeker(actorIndex) &&
                         IsActorFantom(ActorsManager.GetActorAt(BoardManager.GetNeighbor(m, t, i)))))
                    {
                        possibleMove = new(BoardManager.GetNeighbor(m, t, i));
                        return true;
                    }
                }
            }

            possibleMove = new(-1, ReturnCodes.Fail);
            return false;
        }

        public bool CanActorMove(int actorIndex, out ReturnCode<int> possibleMove)
        {

            bool hasBlack = TicketsManager.GetActorTickets(actorIndex, TicketKinds.Black) > 0;

            // Take the Tickets this actor has
            foreach (TicketKinds ticketKind in ModeTickets)
            {

                // River can only be used by the Fantom
                if (IsActorSeeker(actorIndex) && ticketKind == TicketKinds.River)
                    continue;

                // Is Fantom and has the Black Ticket -> must check all possible Tickets
                // Is Bobby -> must check all Modes without having them 
                // Is Detective -> must check all Modes for which he has Tickets
                if (hasBlack ||
                    IsActorBobby(actorIndex) ||
                    TicketsManager.GetActorTickets(actorIndex, ticketKind) > 0)
                {
                    if (CanActorMove(actorIndex, ticketKind, out possibleMove))
                    {
                        return true;
                    }
                }
            }

            // TODO: possibly abilities, Balloon Tickets

            possibleMove = new(-1, ReturnCodes.Fail);
            return false;
        }


        // Round and Move counting ---------------------------------------

        /// <summary>
        /// The number of Rounds done.
        /// </summary>
        public uint CurrentRound { get; private set; } = 0;

        /// <summary>
        /// The number of done Fantom Moves.
        /// </summary>
        public uint CurrentMove { get; private set; } = 0;


        /// <summary>
        /// The value of Rounds checked against for Conditions.
        /// </summary>
        public required uint MaxRounds { get; init; }

        /// <summary>
        /// The value of Moves checked against for Conditions.
        /// </summary>
        public required uint MaxMoves { get; init; }

        // The last tile that the Fantom was publicly seen at (includes double Moves).
        public int? LastPublicFantomPosition { get; private set; } = null;
        // The move number when the Fantom was last seen.
        public uint LastPublicFantomRevelationMoveCount { get; private set; } = 0;

        // The move number when the Fantom will next reveal, skipping the current one
        public uint NextRevealingMove()
        {
            foreach(var move in RevealingMoves)
            {
                if (move > CurrentMove)
                    return move;
            }
            // none will come again
            return MaxMoves + 1;
        }

        public void FantomMoved()
        {
            CurrentMove++;

            // If a revealing Move -> remember the position and move when it was revealed
            if (IsRevealing())
            {
                LastPublicFantomPosition = ActorsManager.GetTileOf(0);
                LastPublicFantomRevelationMoveCount = CurrentMove;
            }
        }

        public int? GetFantomRevealedPosition()
        {
            if (LastPublicFantomPosition is null)
                return null;

            if (CurrentMove == LastPublicFantomRevelationMoveCount)
                return LastPublicFantomPosition;

            return null;
        }

        public bool IsRevealing()
        {
            return RevealingMoves.Contains(CurrentMove);
        }

        public void SeekerMoved()
        {
            CurrentSeekerIndex = (CurrentSeekerIndex + 1) % SeekersCount;
        }

        public void RoundEnded()
        {
            CurrentRound++;
        }

        // Game conditions checking --------------------------------------

        public bool CheckConditions(GameOverConditions[] conditions)
        {
            // Each integer represents multiple variables, if at least one is true, then that clause is fulfilled
            // All clauses have to be true, if one is false -> fail

            foreach (var clause in conditions)
            {
                if (!CheckClause(clause))
                    // no variable was true -> clause failed (empty -None- clause is false by default)
                    return false;
            }


            // default all are true (no clauses true by default)
            return true;
        }

        public GameOverConditions[] GetPassingConditions(GameOverConditions[] conditions)
        {
            List<GameOverConditions> passing = [];

            foreach (var clause in conditions)
            {
                GameOverConditions passingCond = GameOverConditions.None;

                foreach (var condition in Enum.GetValues<GameOverConditions>())
                {
                    if (clause.HasFlag(condition))
                    {
                        // eager eval -> one being true is fine
                        if (CheckCondition(condition))
                            passingCond |= condition;
                    }
                }

                if (passingCond != GameOverConditions.None)
                    passing.Add(passingCond);
            }

            return [.. passing];
        }

        private bool CheckClause(GameOverConditions clause)
        {
            // check all active conditions
            foreach (var condition in Enum.GetValues<GameOverConditions>())
            {
                if (clause.HasFlag(condition))
                {
                    // eager eval -> one being true is fine
                    if (CheckCondition(condition))
                        return true;
                }
            }

            return false;
        }

        // TODO: fill all the conditions
        private bool CheckCondition(GameOverConditions condition)
        {
            return condition switch
            {
                GameOverConditions.FC => IsFantomCaught(),
                GameOverConditions.NCR => HasFantomAvoidedBeingCaughtToLastRound(),
                GameOverConditions.NCM => HasFantomAvoidedBeingCaughtToLastMove(),
                GameOverConditions.FM => FantomCannotMove(),
                GameOverConditions.SM => SeekersCannotMove(),

                // These later with expansions etc.
                GameOverConditions.RB => false,
                GameOverConditions.NRB => false,
                GameOverConditions.DC => false,

                // writing it out for completeness
                GameOverConditions.None or _ => false,
            };
        }

        public bool IsFantomCaught()
        {
            // The Fantom is Caught when a Detective has moved him off the Board.
            // The Fantom is always zero-index Actor
            // Console.WriteLine($"Checking Fantom position {ActorsManager.GetTileOf(0)}");
            return !ActorsManager.IsActorOnBoard(0);
        }

        public bool HasFantomAvoidedBeingCaughtToLastRound()
        {
            // This will be true if the Round counter has passed the required amount and the Fantom is on the Board
            return
               CurrentRound >= MaxRounds && !IsFantomCaught();
        }

        public bool HasFantomAvoidedBeingCaughtToLastMove()
        {
            // This will be true if the Move counter has passed the required amount and the Fantom is on the Board
            return
                CurrentMove >= MaxMoves && !IsFantomCaught();
        }

        public bool FantomCannotMove()
        {
            return GameManager.HaveAllSeekersMoved() && !CanActorMove(0, out _);
        }

        public bool SeekersCannotMove()
        {
            // Is seekers turn
            if (!GameManager.IsSeekersTurn())
                return false;

            for (int i = 0; i < SeekersCount; ++i)
            {
                // Some Seeker can move
                if (CanActorMove(1 + i, out _))
                    return false;
            }

            return true;
        }

        // Game resolution -----------------------------------------------

        /// <summary>
        /// The state of the current game.
        /// </summary>
        public GameResolutionStates gameResolutionResult { get; private set; } = GameResolutionStates.Playing;

        public GameOverConditions[] trueWinningConditions { get; private set; }

        public void SeekersWon(GameOverConditions[] trueWinningConditions)
        {
            gameResolutionResult = GameResolutionStates.SeekersWon;
            this.trueWinningConditions = trueWinningConditions;
        }

        public void FantomWon(GameOverConditions[] trueWinningConditions)
        {
            gameResolutionResult = GameResolutionStates.FantomWon;
            this.trueWinningConditions = trueWinningConditions;
        }

        public void Draw(GameOverConditions[] trueWinningConditions)
        {
            gameResolutionResult = GameResolutionStates.Draw;
            this.trueWinningConditions = trueWinningConditions;
        }
    }
}
