namespace FantomGamesCore.Interfaces
{

    /// <summary>
    /// Through this interface the game can be controlled.
    /// </summary>
    public interface IFantomGameInterface
    {

        /// <summary>
        /// Returns the private (Fantom / Full) current game state with information about all Actor positions, Tickets etc.
        /// </summary>
        /// <returns>Returns the current game state.</returns>
        public ReturnCode<FantomGameState> GetGameState();

        /// <summary>
        /// An initial start command, can only be called once at the start. Purpose is to give control to the
        /// user over when the game finally begins.
        /// </summary>
        public void Start();

        /// <summary>
        /// For stopping the game correctly. Once called, the game cannot be started again.
        /// </summary>
        public void Stop();

        /// <summary>
        /// Required to be called by the user after the Fantom has played to continue the game.
        /// </summary>
        /// <returns></returns>
        public bool ConfirmFantomTurnOver();

        /// <summary>
        /// Required to be called by the user after the Seekers have played to continue the game.
        /// </summary>
        /// <returns></returns>
        public bool ConfirmSeekersTurnOver();

        // GS1 - 

        /// <summary>
        /// If the current settings led to a valid state will return true.
        /// </summary>
        /// <returns>True if the game can be played.</returns>
        public bool IsValid();

        /// <summary>
        /// If the game has ended.
        /// </summary>
        /// <returns>True if the game is over.</returns>
        public bool IsOver();


        /// <summary>
        /// Once the game is over, this will return information about how the game ended. Invalid when the game is not over yet.
        /// </summary>
        /// <returns></returns>
        public ReturnCode<FantomGameResult> GetGameResult();


        /// <summary>
        /// If this Turn Fantom will be revealed.
        /// </summary>
        /// <returns>True if it is a revealing Move.</returns>
        public bool IsFantomRevealing();

        /// <summary>
        /// If it's currently Fantom's turn.
        /// </summary>
        /// <returns>True if is Fantom's turn.</returns>
        public bool IsFantomTurn();

        /// <summary>
        /// If it's currently Seekers' turn.
        /// </summary>
        /// <returns>True if it is Seekers' turn.</returns>
        public bool IsSeekersTurn();

        /// <summary>
        /// The current seeker index if ordering is enabled. Not defined if ordering disabled.
        /// </summary>
        /// <returns>The current seeking Actor index.</returns>
        public ReturnCode<int> GetSeekerIndex();

        /// <summary>
        /// Sets the logger to a TextWriter or disables logging if null. Logger is also passed via settings.
        /// </summary>
        /// <param name="logger">The new logger destination or null.</param>
        public void SetLogger(TextWriter? logger);


        // also GS2.0

        /// <summary>
        /// Starts the game again with current (last valid) settings.
        /// </summary>
        public bool Reset();

        /// <summary>
        /// Changes the current game settings to new ones.
        /// </summary>
        /// <param name="newSettings">The new game settings to be set.</param>
        public ReturnCode<bool> Restart(FantomGameSettings newSettings);

        /// <summary>
        /// Returns the currently loaded settings. Might return default (empty) if in invalid state.
        /// </summary>
        /// <returns>Currently loaded settings.</returns>
        public FantomGameSettings GetActiveSettings();


        // GS2.3 - Current tickets for Actors

        /// <summary>
        /// Current number of tickets of a given type held by the specified Detective. Undefined for Bobbies.
        /// </summary>
        /// <param name="detectiveIndex">The index of the detective in question.</param>
        /// <param name="ticketKind">The kind of the Tickets.</param>
        /// <returns>Remaining number of Tickets for that Detective.</returns>
        public ReturnCode<int> GetRemainingTicketsForDetective(int detectiveIndex, TicketKinds ticketKind);

        /// <summary>
        /// Current number of tickets of a given type held by the Fantom.
        /// </summary>
        /// <param name="ticketKind">The kind of the Tickets.</param>
        /// <returns></returns>
        public ReturnCode<int> GetRemainingTicketsForFantom(TicketKinds ticketKind);

        /// <summary>
        /// Current number of tickets of a given type in the supply.
        /// </summary>
        /// <param name="ticketKind">The kind of the Tickets.</param>
        /// <returns></returns>
        public ReturnCode<int> GetRemainingTicketsInSupply(TicketKinds ticketKind);


        // GS2.4 - Current Rounds and Moves (remaining can be calculated from settings)
        /// <summary>
        /// The number of the current Round.
        /// </summary>
        /// <returns>The number of the current Round.</returns>
        public ReturnCode<uint> CurrentRound();


        /// <summary>
        /// The number of the done Fantom Moves.
        /// </summary>
        /// <returns>The number of the current Fantom Move.</returns>
        public ReturnCode<uint> CurrentMove();


        /// <summary>
        /// Interface for controlling the Fantom safely.
        /// </summary>
        /// <returns></returns>
        public IFantomPlayerInterface GetFantomPlayerTools();

        /// <summary>
        /// Interface for controlling the Seekers safely.
        /// </summary>
        /// <returns></returns>
        public ISeekersPlayerInterface GetSeekersPlayerTools();


        // GS3 - For available moves through this Interface. The structure of the playing Board through it.

        /// <summary>
        /// Retrieve the currently loaded playing board. 
        /// </summary>
        /// <returns>A readonly version of the currently loaded playing board or null if none is loaded.</returns>
        public IReadOnlyFantomBoard? GetBoard();



        // GS5.1 - Specify starting positions

        /// <summary>
        /// If starting positions aren't specified, user has to specify before game can begin.
        /// </summary>
        /// <param name="detectiveIndex"></param>
        /// <param name="tileIndex"></param>
        public ReturnCode<bool> PlaceSeekerAt(int seekerIndex, int tileIndex);

        /// <summary>
        /// If starting positions aren't specified, user has to specify before game can begin.
        /// </summary>
        /// <param name="tileIndex"></param>
        public ReturnCode<bool> PlaceFantomAt(int tileIndex);

        /// <summary>
        /// A general purpose command for future core expansions.
        /// </summary>
        /// <param name="args">Arguments for the generic command.</param>
        /// <returns>A return code for the command.</returns>
        public ReturnCode<object> DoCommand(object args);

        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Add a new game state changed listener.
        /// </summary>
        /// <param name="callback"></param>
        public void AddGameStateChangeCallback(FantomGameStateChangedCallback callback);

        /// <summary>
        /// Remove a game state changed listener.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveGameStateChangeCallback(FantomGameStateChangedCallback callback);
    }

    /// <summary>
    /// Delegate called whenever the Game State Machine state is changed. Useful for limiting calls to the interface.
    /// </summary>
    /// <param name="id">The current state id. Guaranteed to be different from the last call. Might repeat during the duration of the game.</param>
    public delegate void FantomGameStateChangedCallback(long id, GameStates from, GameStates to);
}
