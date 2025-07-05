using System.Diagnostics;
using FantomGamesCore.Interfaces;

namespace FantomGamesCore.Managers
{
    /// <summary>
    /// The main entry for the Game Core. Use the appropriate interfaces to control it. 
    /// </summary>
    public class FantomGameManager : IFantomGameInterface, IFantomPlayerInterface, ISeekersPlayerInterface
    {
        // Since the manager might be accessed by multiple Threads at the same time (for placing, or moving) will lock certain
        // operations just to be sure. A very simple locking mechanism only.
        private readonly object _lock = new();

        // The core state Machine of this Manager
        private readonly GameStateMachine StateMachine;


        private FantomGameSettings ActiveSettings;

        private readonly TicketsManager TicketsManager = new();
        private readonly ActorsManager ActorsManager = new();
        private FantomBoard? BoardManager;

        private RoundContextManager? RoundContextManager;

        private FantomGameContext? GameContext;

        // ---------------------------------------------------------------

        private TextWriter? _activeLogger = null;

        private FantomGameStateChangedCallback? stateChangedListeners = null;
        private long _stateId = 0L;

        /// <summary>
        /// Create an instance of a new Game Core using the passed initial settings.
        /// </summary>
        /// <param name="settings">The initial settings.</param>
        /// <returns>A return code with an IFantomGameInterface instance of this newly created Game Core.</returns>
        public static ReturnCode<IFantomGameInterface> CreateGame(FantomGameSettings settings)
        {
            var man = new FantomGameManager();

            // Some error (likely invalid settings)
            if (!man.ApplySettings(settings, out ReturnCode<bool> applyResult))
                return new(man, applyResult.Code, applyResult.Message);

            // Ok 
            return new(man);
        }

        private FantomGameManager()
        {
            StateMachine = new(Log);
            StateMachine.AddStateChangedListener(EveryStateChanged);

            StateMachine.AddStateTransition(GameStates.Choosing, ChoosingStateEntered);

            StateMachine.AddStateTransition(GameStates.FantomDouble2, FantomMoved);
            StateMachine.AddStateTransition(GameStates.PostFantomTurn, FantomMoved);

            StateMachine.AddStateTransition(GameStates.PostSeekerTurn, SeekerMoved);

            StateMachine.AddStateTransition(GameStates.RoundOver, RoundOver);

            RoundContextManager = null;
            GameContext = null;
        }

        public void Start()
        {
            Log("GM) Start game request.");

            lock (_lock)
            {
                Log("GM) Start game request processing.");

                if (StateMachine.CurrentState == GameStates.Init)
                    PrepareGame();
                else
                    Log("GM) Start game request denied.");
            }
        }

        public void Stop()
        {
            Log("GM) Stop game request.");

            lock (_lock)
            {
                Log("GM) Stop game request processing.");

                StateMachine.Terminate();
            }
        }

        private bool ApplySettings(FantomGameSettings settings, out ReturnCode<bool> applyResult)
        {
            Log("GM) Trying to apply new settings.");

            // basic check the settings can do on their own
            // avoid loading if clearly won't be good
            var setValid = settings.CheckValidity();
            if (!setValid)
            {
                Log($"GM) Settings are not valid: {setValid.Message}.");

                StateMachine.ChangeState(GameStates.Fail);
                applyResult = setValid;
                return false;
            }

            // other validity checks (TODO)
            if (BoardManager == null && settings.GameBoardLoader == null)
            {
                Log($"GM) Does not have a valid Playing Game Board.");

                StateMachine.ChangeState(GameStates.Fail);
                applyResult = new(false, ReturnCodes.BadArgument,
                    $"No playing board is loaded and no board loader given in settings.");
                return false;
            }

            // TODO: catch error loading the board (bad indices etc.)
            if (settings.GameBoardLoader != null)
            {
                Log($"GM) Setting a new Game Board.");

                BoardManager = new FantomBoard(settings.GameBoardLoader);
            }

            // Board at this point cannot be null
            Debug.Assert(BoardManager != null);

            Log($"GM) Resetting Round Context Manager.");
            if (RoundContextManager == null)
            {
                RoundContextManager = new(
                settings.SeekersCount,
                settings.DetectivesCount,
                BoardManager)
                {
                    ActorsManager = ActorsManager,
                    TicketsManager = TicketsManager,
                };
            }
            else
            {
                RoundContextManager.Restart(
                    settings.SeekersCount,
                    settings.DetectivesCount,
                    BoardManager
                    );
            }
          
            Log($"GM) Resetting Actor Manager.");

            ActorsManager.Reset(1 + settings.SeekersCount, BoardManager.TileCount);

            // change to a new logger if given
            if (settings.LoggerDestination != null)
                _activeLogger = settings.LoggerDestination;

            

            Log($"GM) Settings applied.");

            // if valid
            ActiveSettings = settings;

            // Ok
            applyResult = new(true);
            return true;
        }

        private void EveryStateChanged(GameStates oldState, GameStates newState)
        {
            Log($"GM) State changed from {oldState} to {newState}.");

            ++_stateId;
            stateChangedListeners?.Invoke(_stateId, oldState, newState);
        }

        private void PrepareGame()
        {
            Log("GM) Preparing game.");

            _stateId = Random.Shared.NextInt64();

            // These shouldn't be null if Prepare is ever called
            Debug.Assert(BoardManager != null);

            GameContext = new(BoardManager)
            {
                ActorsManager = ActorsManager,
                TicketsManager = TicketsManager,
                GameManager = this,

                KeepOrder = ActiveSettings.SeekerOrder,
                SeekersCount = ActiveSettings.DetectivesCount + ActiveSettings.BobbiesCount,
                DetectivesCount = ActiveSettings.DetectivesCount,
                MaxMoves = ActiveSettings.MaxMoves,
                MaxRounds = ActiveSettings.MaxRounds,
                RevealingMoves = ActiveSettings.RevealingMoves
            };

            Debug.Assert(RoundContextManager != null);
            RoundContextManager.Reset();

            Log("GM) Setting Tickets.");
            TicketsManager.Reset(1 + ActiveSettings.DetectivesCount);

            Log("GM) Setting Supply Tickets.");
            // set supply tickets
            TicketsManager.SetSupplyTickets(ActiveSettings.SupplyTickets);

            Log("GM) Setting Fantom Tickets.");
            // give Fantom his starting Tickets
            TicketsManager.SetActorTickets(0, ActiveSettings.FantomStartingTickets);

            Log("GM) Setting Detectives Tickets.");
            // give Detectives their Tickets
            for (int i = 0; i < ActiveSettings.DetectivesCount; i++)
                TicketsManager.SetActorTickets(1 + i, ActiveSettings.DetectiveStartingTickets);

            Log("GM) Replacing Actors.");
            // reset Actor positions
            ActorsManager.ResetAllActors();

            // place Actors if positions are given
            if (ActiveSettings.FantomStartingPositions != null)
            {
                // Move Fantom to one of the random positions.
                var pos = ActiveSettings.FantomStartingPositions[Random.Shared.Next(ActiveSettings.FantomStartingPositions.Length)];

                Log($"GM) Placing Fantom at {pos}.");
                ActorsManager.Move(0, pos);
            }

            if (ActiveSettings.SeekerStartingPositions != null)
            {
                // From settings we have enough to cover all detectives and bobbies
                List<int> possiblePositions = [.. ActiveSettings.SeekerStartingPositions];

                for (
                    int seekerIndex = 0;
                    seekerIndex < ActiveSettings.DetectivesCount + ActiveSettings.BobbiesCount;
                    ++seekerIndex)
                {
                    // possibly slow, but mutual exclusion guaranteed
                    int randomIndex = Random.Shared.Next(possiblePositions.Count);
                    int pos = possiblePositions[randomIndex];
                    
                    Log($"GM) Placing Seeker {seekerIndex} at {pos}.");
                    
                    ActorsManager.Move(1 + seekerIndex, pos);
                    possiblePositions.RemoveAt(randomIndex);
                }
            }

            Log("GM) Game prepared.");
            StateMachine.ChangeState(GameStates.Choosing);
        }

        // Game Interface -----------------------------

        public ReturnCode<FantomGameState> GetGameState()
        {
            lock (_lock)
            {
                return GetGameState(false);
            }
        }

        private ReturnCode<FantomGameState> GetPublicGameState()
        {
            if (GameContext == null || !GameContext.IsRevealing())
            {
                return GetGameState(true);
            }

            return GetGameState(false);
        }

        public bool HaveAllSeekersMoved()
        {
            lock (_lock)
            {
                return RoundContextManager is not null && RoundContextManager.HaveAllMoved();
            }
        }

        private ReturnCode<FantomGameState> GetGameState(bool isPublic)
        {
            var c_move = CurrentMove();
            var c_round = CurrentRound();

            int[] seeker_positions = new int[ActiveSettings.DetectivesCount + ActiveSettings.BobbiesCount];
            int? fantom_position = null;
            int[,] actor_tickets = new int[1 + ActiveSettings.DetectivesCount, FantomGameSettings.TICKET_KINDS_COUNT];
            int[] supply_tickets = new int[FantomGameSettings.TICKET_KINDS_COUNT];

            // Always fine
            Debug.Assert(GameContext != null);

            // if not valid, wouldn't make any sense to fill out, so just leave empty
            if (IsValid())
            {
                // Fantom position if private or is revealing
                if (!isPublic || GameContext.IsRevealing())
                    fantom_position = ActorsManager.GetTileOf(0);

                // fill out Seekers' positions
                for (int i = 0; i < seeker_positions.Length; ++i)
                    seeker_positions[i] = ActorsManager.GetTileOf(1 + i);

                // actor Tickets
                for (int i = 0; i < 1 + ActiveSettings.DetectivesCount; ++i)
                    for (int c = 0; c < FantomGameSettings.TICKET_KINDS_COUNT; ++c)
                        actor_tickets[i, c] = TicketsManager.GetActorTickets(i, (TicketKinds)c);

                // supply Tickets
                for (int c = 0; c < FantomGameSettings.TICKET_KINDS_COUNT; ++c)
                    supply_tickets[c] = TicketsManager.GetSupplyTickets((TicketKinds)c);
            }
            
            var gameState = new FantomGameState(   
                ActiveSettings.DetectivesCount,
                ActiveSettings.BobbiesCount,
                seeker_positions,                
                fantom_position,
                GameContext.LastPublicFantomPosition,
                actor_tickets,
                supply_tickets
                )
            {
                CurrentMove = c_move,
                CurrentRound = c_round
            };

            return IsValid() ? new(gameState) : new(gameState, ReturnCodes.InvalidState);
            
        }

        public bool Reset()
        {
            Log($"GM) Reset request.");

            lock (_lock)
            {

                Log($"GM) Reset request processing.");

                
                if (IsValid())
                {
                    Log("GM) Resetting game.");

                    StateMachine.Reset();

                    PrepareGame();
                    return true;
                }
                else
                // Do nothing, waiting for restart
                {
                    Log("GM) Failed to reset.");

                    return false;
                }
            }
        }

        public ReturnCode<bool> Restart(FantomGameSettings newSettings)
        {
            Log($"GM) Restart with new settings request.");

            lock (_lock)
            {                

                Log($"GM) New Settings: '{newSettings}'.");

                if (!ApplySettings(newSettings, out ReturnCode<bool> applyResult))
                {
                    Log($"GM) Cannot restart with given settings: {applyResult.Message}.");
                    return applyResult;
                }

                Log("GM) Restarting game with new settings.");

                Reset();

                // Ok
                return new(true);
            }
        }

        private void CheckGameOver()
        {
            // The only time a game over check happens is if we are in a valid state, so never will be null
            Debug.Assert(GameContext != null);

            if (GameContext.CheckConditions(ActiveSettings.SeekersVictoryConditions))
            {
                GameContext.SeekersWon(GameContext.GetPassingConditions(ActiveSettings.SeekersVictoryConditions));
                Log("GM) Seekers have won the game.");
            }
            else if (GameContext.CheckConditions(ActiveSettings.FantomVictoryConditions))
            {
                GameContext.FantomWon(GameContext.GetPassingConditions(ActiveSettings.FantomVictoryConditions));
                Log("GM) Fantom has won the game.");
            }
            else if (GameContext.CheckConditions(ActiveSettings.DrawConditions))
            {
                GameContext.Draw(GameContext.GetPassingConditions(ActiveSettings.DrawConditions));
                Log("GM) The game is a draw.");
            }

            // Avoid firing the event multiple times
            if (StateMachine.CurrentState != GameStates.GameOver &&
                GameContext.gameResolutionResult != GameResolutionStates.Playing)
            {
                Log("GM) Game is over.");

                StateMachine.ChangeState(GameStates.GameOver);
            }
        }

        // ---------------------------------------------

        public ReturnCode<FantomGameResult> GetGameResult()
        {
            Log($"GM) Game over result request.");

            lock (_lock)
            {

                Log($"GM) Game over result request processing.");

                if (!IsOver() || GameContext == null)
                {
                    Log($"GM) Game over result request denied.");

                    return new(new(), ReturnCodes.InvalidState,
                        $"The game is not initialized or over yet.");
                }

                Log($"GM) Game over result returning.");

                return new(new()
                {
                    FantomWon = GameContext.gameResolutionResult == GameResolutionStates.FantomWon,
                    SeekersWon = GameContext.gameResolutionResult == GameResolutionStates.SeekersWon,
                    GameDraw = GameContext.gameResolutionResult == GameResolutionStates.Draw,
                    trueWinningConditions = GameContext.trueWinningConditions,
                });
            }
        }


        public bool IsValid()
        {
            return StateMachine.CurrentState != GameStates.Init && StateMachine.CurrentState != GameStates.Fail;
        }

        public bool IsOver()
        {
            return StateMachine.CurrentState == GameStates.GameOver;
        }

        public bool IsFantomRevealing()
        {
            if (GameContext == null)
                return false;

            return GameContext.IsRevealing();
        }

        public bool IsFantomTurn()
        {
            return
                StateMachine.CurrentState == GameStates.FantomTurn ||
                StateMachine.CurrentState == GameStates.FantomDouble1 ||
                StateMachine.CurrentState == GameStates.FantomDouble2;
        }

        public bool IsSeekersTurn()
        {
            return StateMachine.CurrentState == GameStates.SeekersTurn;
        }

        // ---------------------------------------------

        public IReadOnlyFantomBoard? GetBoard()
        {
            return BoardManager;
        }

        public ReturnCode<int> GetSeekerIndex()
        {
            if (GameContext == null)
                return new(-1, ReturnCodes.InvalidState,
                    "The game isn't properly initialized yet.");

            // Ok
            return new(GameContext.CurrentSeekerIndex);
        }

        public FantomGameSettings GetActiveSettings()
        {
            // Note: the setting is a rather large struct, and could in theory be modified, so to avoid problems, lock here too
            lock (_lock)
            {
                return ActiveSettings;

            }
        }

        public ReturnCode<int> GetRemainingTicketsForDetective(int detectiveIndex, TicketKinds ticketKind)
        {
            if (RoundContextManager == null)
                return new(-1, ReturnCodes.InvalidState,
                    "The game isn't properly initialized yet.");

            // Index check
            if (RoundContextManager.CheckSeekerIndex(detectiveIndex, out ReturnCode<bool> checkResult))
                return new(-1, checkResult.Code, checkResult.Message);

            // Ok
            return new(TicketsManager.GetActorTickets(1 + detectiveIndex, ticketKind));
        }

        public ReturnCode<int> GetRemainingTicketsForFantom(TicketKinds ticketKind)
        {
            return new(TicketsManager.GetActorTickets(0, ticketKind));
        }

        public ReturnCode<int> GetRemainingTicketsInSupply(TicketKinds ticketKind)
        {
            return new(TicketsManager.GetSupplyTickets(ticketKind));
        }


        public ReturnCode<uint> CurrentRound()
        {

            if (GameContext == null)
                return new(0, ReturnCodes.InvalidState,
                    "The game isn't properly initialized yet.");

            // Ok
            return new(GameContext.CurrentRound);
        }

        public ReturnCode<uint> CurrentMove()
        {
            if (GameContext == null)
                return new(0, ReturnCodes.InvalidState,
                    "The game isn't properly initialized yet.");

            // Ok
            return new(GameContext.CurrentMove);
        }

        // State machine changes ---------------------------------------------

        // Choosing state ----

        private void ChoosingStateEntered(GameStates oldState, GameStates newState)
        {
            // The choosing state is only entered from the Init state
            Log("GM) Checking if game prepared to start.");

            // Checks if all Actors have been placed
            TryTransitionToGame();
        }

        private void TryTransitionToGame()
        {
            // All Actors have to be on the Board
            for (int i = 0; i < ActorsManager.ActorsCount; i++)
                if (!ActorsManager.IsActorOnBoard(i))
                {
                    Log($"GM) Actor {i} is not placed yet.");
                    return;
                }

            Log($"GM) Game ready, starting.");

            // Start the game
            StateMachine.ChangeState(GameStates.FantomTurn);
        }

        public ReturnCode<bool> PlaceSeekerAt(int seekerIndex, int tileIndex)
        {
            Log($"SI) Place Seeker {seekerIndex} at {tileIndex} request.");

            lock (_lock)
            {
                Log($"SI) Place Seeker {seekerIndex} at {tileIndex} request processing.");

                if (RoundContextManager == null)
                    return new(false, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                if (StateMachine.CurrentState != GameStates.Choosing)
                    return new(false, ReturnCodes.InvalidState,
                        $"Cannot place Seekers in the current state {StateMachine.CurrentState}");

                if (!RoundContextManager.PlaceSeekerAt(seekerIndex, tileIndex, out ReturnCode<bool> placementResult))
                {
                    Log($"SI) Place Seeker {seekerIndex} at {tileIndex} request denied: {placementResult.Message}.");

                    return placementResult;
                }

                Log($"SI) Placed Seeker {seekerIndex} at {tileIndex}.");

                TryTransitionToGame();

                // Ok
                return new(true);
            }
        }

        public ReturnCode<bool> PlaceFantomAt(int tileIndex)
        {
            Log($"FI) Place Fantom at {tileIndex} request.");

            lock (_lock)
            {
                Log($"FI) Place Fantom at {tileIndex} request processing.");

                if (RoundContextManager == null)
                    return new(false, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                if (StateMachine.CurrentState != GameStates.Choosing)
                    return new(false, ReturnCodes.InvalidState,
                            $"Cannot place Fantom in the current state {StateMachine.CurrentState}");

                if (!RoundContextManager.PlaceFantomAt(tileIndex, out ReturnCode<bool> placementResult))
                {
                    Log($"FI) Place Fantom at {tileIndex} request denied: {placementResult.Message}.");

                    return placementResult;
                }

                Log($"FI) Placed Fantom at {tileIndex}.");

                TryTransitionToGame();

                // Ok
                return new(true);

            }

        }


        // Fantom moved ----

        private void FantomMoved(GameStates oldState, GameStates newState)
        {
            Debug.Assert(GameContext != null);

            Log($"GM) Fantom Moved");

            GameContext.FantomMoved();
            CheckGameOver();
        }

        public bool ConfirmFantomTurnOver()
        {
            Log("GM) Trying to confirm Fantom turn over.");
            
            lock (_lock)
            {
                Log("GM) Trying to confirm Fantom turn over processing.");

                if (StateMachine.CurrentState == GameStates.PostFantomTurn && !IsOver())
                {
                    Log("GM) Fantom turn over confirmed.");

                    // It's now Seekers' turn
                    StateMachine.ChangeState(GameStates.SeekersTurn);
                    return true;
                }

                Log("GM) Fantom cannot end turn.");

                return false;
            }
        }

        // Seeker moved ----

        private void SeekerMoved(GameStates oldState, GameStates newState)
        {
            Debug.Assert(GameContext != null);
            Debug.Assert(RoundContextManager != null);

            Log($"GM) Seeker Moved");

            GameContext.SeekerMoved();
            CheckGameOver();

            if (!IsOver())
            {
                // Check if all Seekers (that can) have moved
                if (!RoundContextManager.HaveAllMoved())
                {
                    Log($"GM) Some Seekers can still Move");

                    StateMachine.ChangeState(GameStates.SeekersTurn);
                }
            }
        }

        public bool ConfirmSeekersTurnOver()
        {
            Log("GM) Trying to confirm Seekers turn over.");

            lock (_lock)
            {
                Log("GM) Trying to confirm Seekers turn over processing.");

                if (RoundContextManager is null)
                    return false;


                if (!IsOver())
                {
                    if (RoundContextManager.HaveAllMoved())
                    {
                        Log("GM) Seekers turn over confirmed.");

                        // It is now Round over
                        StateMachine.ChangeState(GameStates.RoundOver);
                        return true;
                    }

                    Log("GM) Some Seekers have not Moved yet.");
                }

                Log("GM) Seekers cannot end turn.");

                return false;
            }
        }

        // Round over ----

        private void RoundOver(GameStates oldState, GameStates newState)
        {
            Debug.Assert(GameContext != null);
            Debug.Assert(RoundContextManager != null);

            Log($"GM) Round Over");

            GameContext.RoundEnded();
            CheckGameOver();

            if (!IsOver())
            {
                Log($"GM) Game not over - preparing for new round");

                RoundContextManager.Reset();
                StateMachine.ChangeState(GameStates.FantomTurn);
            }
        }

        // ---------------------------------------------

        public IFantomPlayerInterface GetFantomPlayerTools()
        {
            return this;
        }


        public ISeekersPlayerInterface GetSeekersPlayerTools()
        {
            return this;
        }


        #region Fantom tools

        ReturnCode<FantomGameState> IFantomPlayerInterface.GetGameState()
        {
            // Fantom Tools / Interface is authorized to get all private information - Fantom position
            return GetGameState();
        }

        ReturnCode<bool> IFantomPlayerInterface.UseDouble()
        {
            Log($"FI) Fantom Use Double request.");

            lock (_lock)
            {
                Log($"FI) Fantom Use Double request processing.");


                // The Double Ticket can be used only when it's the Fantom's turn, and he hasn't used it yet
                // He has to have at least one Double Ticket available

                if (RoundContextManager == null)
                    return new(false, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                if (StateMachine.CurrentState != GameStates.FantomTurn)
                    return new(false, ReturnCodes.InvalidState,
                            $"Fantom cannot use Double Ticket in the current state {StateMachine.CurrentState}");

                if (!RoundContextManager.FantomUseDouble(out ReturnCode<bool> useResult))
                {
                    Log($"FI) Fantom Use Double request denied: {useResult.Message}.");
                    return useResult;
                }

                Log($"FI) Fantom Used Double.");

                // Go into a special state
                StateMachine.ChangeState(GameStates.FantomDouble1);

                // Ok
                return new(true);
            }
        }

        ReturnCode<bool> IFantomPlayerInterface.Move(int tileIndex_Where, TicketKinds ticketKind)
        {
            Log($"FI) Fantom Move request to {tileIndex_Where} via {Enum.GetName(ticketKind)}.");

            lock (_lock)
            {
                Log($"FI) Fantom Move request to {tileIndex_Where} via {Enum.GetName(ticketKind)} processing.");

                // Fantom can only move in Fantom Turn, Double1, Double2 states
                // Fantom can only move to free tiles, else the Game would be over
                // Can only move to tiles, where there's a path between the current position and the wanted one
                // Has to have at least one Ticket of that kind

                // Certain Tickets currently unsupported or have to be used in a different way

                if (RoundContextManager == null)
                    return new(false, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                if (StateMachine.CurrentState != GameStates.FantomTurn &&
                    StateMachine.CurrentState != GameStates.FantomDouble1 &&
                    StateMachine.CurrentState != GameStates.FantomDouble2)
                    return new(false, ReturnCodes.InvalidState,
                        $"Fantom cannot move in the current state {StateMachine.CurrentState}.");

                if (!RoundContextManager.FantomMove(tileIndex_Where, ticketKind, out ReturnCode<bool> moveResult))
                {
                    Log($"FI) Fantom Move request denied: {moveResult.Message}");
                    return moveResult;
                }

                Log($"FI) Fantom Moved to {tileIndex_Where} via {Enum.GetName(ticketKind)}.");

                // If used a Double Ticket or not: 
                // Based on the current state, transition to the next
                if (StateMachine.CurrentState == GameStates.FantomDouble1)
                    StateMachine.ChangeState(GameStates.FantomDouble2);
                else
                    StateMachine.ChangeState(GameStates.PostFantomTurn);

                // Ok
                return new(true);
            }
        }

        ReturnCode<int> IFantomPlayerInterface.CannotMove()
        {
            Log("FI) Fantom No Move request.");

            lock (_lock)
            {
                Log("FI) Fantom No Move request processing.");

                if (GameContext == null)
                    return new(-1, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                // Fantom cannot move if he has no Tickets or is Blocked by the Seekers
                // This might lead to a game over, but only when in a state, where this is checked
                // To get to it, a confirmation from the Fantom is required - I cannot move

                // Can Move -> this operation is not allowed

                if (GameContext.CanActorMove(0, out ReturnCode<int> canResult))
                {
                    Log($"FI) Fantom No Move request denied - Fantom can Move to {canResult.Value}.");
                    return new(canResult.Value, ReturnCodes.Fail,
                        $"Fantom can Move to {canResult.Value} so No Move isn't possible.");
                }

                Log($"FI) Fantom cannot Move, so skipping turn.");

                // As if Moved
                StateMachine.ChangeState(GameStates.PostFantomTurn);

                // Ok 
                return new(-1);
            }
        }


        ReturnCode<bool> IFantomPlayerInterface.PlaceAt(int tileIndex_Where)
        {
            // Just a utility redirect

            return PlaceFantomAt(tileIndex_Where);
        }

        bool IFantomPlayerInterface.IsFantomTurn()
        {
            return IsFantomTurn();
        }

        bool IFantomPlayerInterface.ConfirmTurnOver()
        {
            return ConfirmFantomTurnOver();
        }

        #endregion


        #region Seekers tools

        ReturnCode<FantomGameState> ISeekersPlayerInterface.GetGameState()
        {
            lock (_lock)
            {
                // Might not know the position of Fantom if it isn't a Revealing Move
                return GetPublicGameState();
            }
        }

        ReturnCode<bool> ISeekersPlayerInterface.Move(int seekerIndex, int tileIndex_Where, TicketKinds ticketKind)
        {
            Log($"SI) Seeker {seekerIndex} Move request to {tileIndex_Where} via {Enum.GetName(ticketKind)}.");


            lock (_lock)
            {
                Log($"SI) Seeker {seekerIndex} Move request to {tileIndex_Where} via {Enum.GetName(ticketKind)} processing.");

                // Can only use the Mode1, Mode2, Mode3

                // Can only move in DetectiveTurn state
                // If the ordering of Detectives is enabled, can only move the current one
                // A Seeker can only move if it hasn't moved yet this round
                // Detectives can only move if they have enough tickets

                // Seekers can only move to tiles that are free, or have the Fantom on them
                // Seekers can move to tiles that are connected by the specified mode

                // After moving, Detectives give their Tickets to the Fantom

                if (RoundContextManager == null || GameContext == null)
                    return new(false, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                if (StateMachine.CurrentState != GameStates.SeekersTurn)
                    return new(false, ReturnCodes.InvalidState,
                        $"Seekers cannot move in the current state {StateMachine.CurrentState}.");

                if (!GameContext.IsValidSeekerIndex(seekerIndex))
                    return new(false, ReturnCodes.BadArgument,
                        $"Seeker {seekerIndex} is not a valid seeker index.");

                if (!GameContext.IsThisSeekersTurn(seekerIndex))
                    return new(false, ReturnCodes.InvalidState,
                        $"Seeker {seekerIndex} cannot play as it is {GameContext.CurrentSeekerIndex}'s turn.");

                // Try to do the Move, if it fails, inform caller
                if (!RoundContextManager.SeekerMove(seekerIndex, tileIndex_Where, ticketKind,
                    out ReturnCode<bool> moveResult))
                {
                    Log($"SI) Seeker {seekerIndex} Move request denied: {moveResult.Message}");
                    return moveResult;
                }

                Log($"SI) Seeker {seekerIndex} Moved to {tileIndex_Where} via {Enum.GetName(ticketKind)}.");

                // Update state
                StateMachine.ChangeState(GameStates.PostSeekerTurn);

                // Ok
                return new(true);
            }
        }

        ReturnCode<int> ISeekersPlayerInterface.CannotMove()
        {
            Log("SI) Seeker No Move request.");

            lock (_lock)
            {
                Log("SI) Seeker No Move request processing.");

                if (RoundContextManager == null || GameContext == null)
                    return new(-1, ReturnCodes.InvalidState,
                        "The game isn't properly initialized yet.");

                // If ordering matters and the next Seeker cannot Move, then this method can be used
                // If ordering doesn't matter and
                // If All unmoved Seekers Cannot Move then the round can be ended via this method
                // If at least one that can, then this is invalid
                // If no Seekers can Move, this should be a game over condition, but that is checked only 
                // in certain states, so to get to them, this method can be used

                ReturnCode<int> canResult;

                if (ActiveSettings.SeekerOrder)
                {
                    Log("SI) Seeker play order enabled so current seeker is checked only.");

                    // The one that is currently to play
                    if (!RoundContextManager.HasMoved(GameContext.CurrentSeekerIndex) &&
                        GameContext.CanActorMove(1 + GameContext.CurrentSeekerIndex, out canResult))
                    {
                        Log($"SI) Seeker No Move request denied - Seeker {GameContext.CurrentSeekerIndex} can Move to {canResult.Value}.");
                        return new(canResult.Value, ReturnCodes.Fail,
                            $"Seeker {GameContext.CurrentSeekerIndex} can Move to {canResult.Value} so No Move isn't possible.");
                    }

                    Log($"SI) Seeker {GameContext.CurrentSeekerIndex} cannot Move, so skipping turn.");

                    // Mark this one as having Moved
                    RoundContextManager.Moved(GameContext.CurrentSeekerIndex);

                }
                else
                {
                    Log("SI) Seeker play order disabled so current all seekers have to be checked.");

                    for (int i = 0; i < RoundContextManager.SeekersCount; ++i)
                    {
                        // One that hasn't moved, can move
                        if (!RoundContextManager.HasMoved(i) &&
                            GameContext.CanActorMove(1 + i, out canResult))
                        {
                            Log($"SI) Seeker No Move request denied - Seeker {i} can Move to {canResult.Value}.");
                            return new(canResult.Value, ReturnCodes.Fail,
                                $"Seeker {i} can Move to {canResult.Value} so No Move isn't possible.");
                        }
                    }

                    Log("SI) All Seekers cannot Move, skipping now.");

                    // Mark all as having Moved
                    for (int i = 0; i < RoundContextManager.SeekersCount; ++i)
                        RoundContextManager.Moved(i);
                }

                StateMachine.ChangeState(GameStates.PostSeekerTurn);

                // Ok
                return new(-1);
            }
        }

        ReturnCode<bool> ISeekersPlayerInterface.PlaceAt(int seekerIndex, int tileIndex_Where)
        {
            return PlaceSeekerAt(seekerIndex, tileIndex_Where);
        }

        ReturnCode<int> ISeekersPlayerInterface.SeekerIndex()
        {
            return GetSeekerIndex();
        }

        bool ISeekersPlayerInterface.IsSeekerTurn()
        {
            return IsSeekersTurn();
        }

        bool ISeekersPlayerInterface.ConfirmTurnOver()
        {
            return ConfirmSeekersTurnOver();
        }

        #endregion

        // ---------------------------------------------

        public void AddGameStateChangeCallback(FantomGameStateChangedCallback callback)
        {
            Log("GM) Adding game state change callback");
            stateChangedListeners += callback;
        }

        public void RemoveGameStateChangeCallback(FantomGameStateChangedCallback callback)
        {
            Log("GM) Removing game state change callback");
            stateChangedListeners -= callback;
        }

        public void SetLogger(TextWriter? logger)
        {
            Log("GM) Changing logger destination.");
            _activeLogger = logger;
            Log("GM) Logger destination changed.");
        }

        // ---------------------------------------------

        private object _logLock = new();

        private void Log(string message)
        {
            // avoid locking
            if (_activeLogger is null)
                return;

            // multiple-threads might try to access the log
            lock (_logLock)
            {
                _activeLogger?.WriteLine(message);
                _activeLogger?.Flush();
            }
        }

        public ReturnCode<object> DoCommand(object args)
        {
            return new(0);
        }
    }
}
