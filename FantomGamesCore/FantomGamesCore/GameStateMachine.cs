namespace FantomGamesCore
{

    public enum GameStates
    {
        /// <summary>
        /// Initial state of the state machine.
        /// </summary>
        Init,
        /// <summary>
        /// In this state each playing figure in the game must be placed on the board somehow.
        /// </summary>
        Choosing,

        /// <summary>
        /// Fantom's turn, can Move, UseDouble or Pass.
        /// </summary>
        FantomTurn,
        /// <summary>
        /// Fantom's decided to UseDouble so now has to Move twice or Pass.
        /// </summary>
        FantomDouble1,
        /// <summary>
        /// Fantom's second move.
        /// </summary>
        FantomDouble2,
        /// <summary>
        /// Right after Fantom's turn, need to confirm turn over.
        /// </summary>
        PostFantomTurn,
        /// <summary>
        /// A Seeker's turn, can Move or Pass.
        /// </summary>
        SeekersTurn,
        /// <summary>
        /// Right after a Seeker's turn, need to confirm turn over.
        /// </summary>
        PostSeekerTurn,
        /// <summary>
        /// Current round is over, will continue to the next immediately.
        /// </summary>
        RoundOver,

        /// <summary>
        /// The game is over, results can be retrieved or game can be reset.
        /// </summary>
        GameOver,

        /// <summary>
        /// The core got invalid settings and cannot continue until valid ones are given via Restart
        /// </summary>
        Fail,

        /// <summary>
        /// The core has been terminated, no more actions are possible.
        /// </summary>
        Terminated
    }

    internal delegate void StateChanged(GameStates oldState, GameStates newState);


    internal class GameStateMachine
    {

        private readonly object _lock = new();
        private readonly Thread _selfThread;
        private readonly Action<string> _logger;

        public GameStateMachine(Action<string> logger)
        {
            _logger = logger;

            _running = true;
            _selfThread = new Thread(Loop);
            _selfThread.Start();
        }

        private GameStates _lastChangedState = GameStates.Init;

        private readonly Queue<GameStates> _stateChanges = new();
        private GameStates _currentState = GameStates.Init;

        private readonly Dictionary<GameStates, StateChanged?> _stateTransitions = [];
        private StateChanged? _stateChanged = null;

        /// <summary>
        /// Attaches a listener to whenever the given state is entered.
        /// </summary>
        /// <param name="state">The state to called from when entered</param>
        /// <param name="transition">The function to call</param>
        public void AddStateTransition(GameStates state, StateChanged transition)
        {

            if (_stateTransitions.ContainsKey(state))
                _stateTransitions[state] += transition;
            else
                _stateTransitions[state] = transition;

        }

        public void RemoveStateTransition(GameStates state, StateChanged transition)
        {

            _stateTransitions[state] -= transition;

        }


        public void AddStateChangedListener(StateChanged? stateChanged)
        {

            if (_stateChanged == null)
                _stateChanged = stateChanged;
            else
                _stateChanged += stateChanged;

        }

        public void RemoveStateChangedListener(StateChanged? stateChanged)
        {
            _stateChanged -= stateChanged;
        }

        public void ChangeState(GameStates newState)
        {
            if (newState != _lastChangedState)
            {
                lock (_lock)
                {
                    _stateChanges.Enqueue(newState);                    

                    _logger($"GM.SM) Internal State transition {_lastChangedState} to {newState}");

                    var cachedState = _lastChangedState;
                    _lastChangedState = newState;

                    // NOTE: if state is changed when calling the listeners the transitions will only get called after they've finished, which means the transitions will be called in reverse order.
                    // 

                    if (_stateTransitions.TryGetValue(newState, out StateChanged? transition))
                    {
                        transition?.Invoke(cachedState, newState);
                    }

                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _stateChanges.Clear();
            }
        }

        public void Terminate()
        {
            lock (_lock)
            {
                _logger("GM.SM) Terminating State Machine");

                _stateChanges.Clear();
                _running = false;
                _lastChangedState = GameStates.Terminated;
            }
        }

        public GameStates CurrentState
        {
            get
            {
                lock (_lock)
                {
                    return _lastChangedState;
                }
            }
        }

        // -------------------------------------------------------------------

        private volatile bool _running = false;
        private void Loop()
        {
            while (_running)
            {
                if (_stateChanges.Count > 0)
                {
                    GameStates newState;
                    lock (_lock)
                    {
                        newState = _stateChanges.Dequeue();
                    }

                    _logger($"GM.SM) Event from {_currentState} to {newState}");                    

                    _stateChanged?.Invoke(_currentState, newState);

                    _currentState = newState;
                }
            }

        }

    }
}
