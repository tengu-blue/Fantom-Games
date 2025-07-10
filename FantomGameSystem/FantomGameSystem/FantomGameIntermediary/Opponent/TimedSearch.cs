using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesIntermediary.Opponent.Parts.FantomParts;
using FantomGamesIntermediary.Opponent.Parts.SeekerParts;
using FantomGamesSystemUtils;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FantomGamesIntermediary.Opponent
{

    // TODO: future search costs falloff slightly? uncertainty of the future 
    // TODO: brain compare when the best is gonna happen (so if WIN sooner, then update too)
    // TODO: limit with max rounds 

    internal class TimedSearch<MoveType, OpponentMoveType, BrainType, InputState, MovesGenerator, StateEvalType>
        where MoveType : struct, IActorMove<MoveType>
        where OpponentMoveType : struct, IActorMove<OpponentMoveType>
        where BrainType : struct, IOpponentBrain<MoveType>
        where InputState : struct, IActorState<InputState, MoveType>
        where MovesGenerator : struct, ILegalMovesGenerator<MoveType, InputState>
    {

        // Search activity control

        private Thread _mainSearchThread;
        private volatile bool _living = false;
        private ManualResetEvent _pauseEvent = new(false);

        private Timer _timer;
        private int _thinkingTime;
        private int _maxDepth = 1;

        private TimedSearch(
            BrainType brain,
            MovesGenerator movesGenerator,
            IStateEvaluator<StateEvalType, MoveType, OpponentMoveType> stateEvaluator,
            int thinkingTime,
            int maxDepth)
        {
            _brain = brain;
            _movesGenerator = movesGenerator;
            _stateEvaluator = stateEvaluator;
            _thinkingTime = thinkingTime;
            _maxDepth = maxDepth;
            _timer = new Timer(UseBrainToMove, null, Timeout.Infinite, Timeout.Infinite);

            _living = true;
            _mainSearchThread = new Thread(Loop);
            _mainSearchThread.Start();
        }

        BrainType _brain;

        private void UseBrainToMove(object? _)
        {
            // try to avoid calling the best move more than once, but not too bad if it is called
            StopTimer();
            if(!_terminated)
                _brain.MakeBestMove();
        }

        // make the seekers / fantom opponents
        public static TimedSearch<SeekersMove, FantomMove, SeekerBrain, SeekersState, SeekerMovesGenerator, SeekersStateEvaluator> CreateSeekerThinker(
            ISeekersCommander commander,
            IReadOnlyFantomBoard board,
            FantomGameSettings settings,
            IEnumerable<int> fantomStartingPositions,
            int thinkingTime)
        {
            return new TimedSearch<SeekersMove, FantomMove, SeekerBrain, SeekersState, SeekerMovesGenerator, SeekersStateEvaluator>(
                new SeekerBrain(commander, settings.DetectivesCount, settings.BobbiesCount),
                new SeekerMovesGenerator(board, settings.SeekersCount, settings.DetectivesCount, settings.SeekerOrder),
                new SeekersStateEvaluator(settings.SeekersCount, settings.DetectivesCount, board, fantomStartingPositions),
                thinkingTime,
                1);
        }

        public static TimedSearch<FantomMove, SeekersMove, FantomBrain, FantomState, FantomMovesGenerator, FantomStateEvaluator> CreateFantomThinker(
            IFantomCommander commander,
            IReadOnlyFantomBoard board,
            FantomGameSettings settings,
            IEnumerable<int> fantomStartingPositions,
            uint maxRounds,
            IEnumerable<uint> revealingMoves,
            int thinkingTime)
        {
            return new TimedSearch<FantomMove, SeekersMove, FantomBrain, FantomState, FantomMovesGenerator, FantomStateEvaluator>(
                new FantomBrain(commander),
                new FantomMovesGenerator(board, revealingMoves),
                new FantomStateEvaluator(settings.SeekersCount, settings.DetectivesCount, board, fantomStartingPositions, maxRounds, revealingMoves),
                thinkingTime,
                3);
        }


        // ----------------------------------------------------------------------------------
        // Search control

        private object _searchLock = new();

        private volatile bool _searchRequest = false;
        private volatile bool _resetRequest = false;
        private volatile bool _terminated = false;
        private bool _searching = false;
        private CancellationTokenSource _abortSearchSource = new();


        // TODO: lock access to these ? (maybe not necessary since only one thread should be doing anything at one time, but do it anyway)
        private FantomGameState? _cachedGamesState;
        private MoveType? _cachedOwnMove;
        private OpponentMoveType? _cachedOpponentMove;


        // Search resources
        // TODO: multi-thread - multiple instances, managed
        private IStateEvaluator<StateEvalType, MoveType, OpponentMoveType> _stateEvaluator;

        public void Reset(FantomGameState initState)
        {
            lock (_searchLock)
            {
                _cachedGamesState = initState;
                _resetRequest = true;
                AbortSearch();
            }
        }

        public void CacheOwnMove(MoveType ownMove)
        {
            lock (_searchLock)
            {
                _cachedOwnMove = ownMove;
            }
        }

        public void CacheOpponentMove(OpponentMoveType opMove)
        {
            lock (_searchLock)
            {
                _cachedOpponentMove = opMove;
            }
        }

        public void CacheGameState(FantomGameState gameState)
        {
            lock (_searchLock)
            {
                _cachedGamesState = gameState;
            }
        }

        public void StartSearch()
        {
            lock (_searchLock)
            {
                AbortSearch();
                _searchRequest = true;
            }
        }

        public void AbortSearch()
        {
            lock (_searchLock)
            {
                if (_searching)
                {
                    _searchRequest = false;
                    _abortSearchSource?.Cancel();
                    StopTimer();
                }
            }
        }


        /// <summary>
        /// Pause the Search, stop using resources.
        /// </summary>
        public void Sleep()
        {
            lock (_searchLock)
            {
                _pauseEvent.Reset();
                AbortSearch();
            }
        }

        /// <summary>
        /// Get ready to start the Search again.
        /// </summary>
        public void Wake()
        {
            lock (_searchLock)
            {
                _pauseEvent.Set();
                AbortSearch();
            }
        }

        /// <summary>
        /// End the Search, free all resources.
        /// </summary>
        public void Terminate()
        {
            lock (_searchLock)
            {
                _terminated = true;
                _living = false;
                AbortSearch();
                _pauseEvent.Set();
                _pauseEvent.Dispose();
            }
        }

        // ----------------------------------------------------------------------

        private void StartTimer()
        {
            _timer.Change(_thinkingTime, Timeout.Infinite);
        }

        private void StopTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        
        private void Reset()
        {
            // timer should be stopped by the request
            // StopTimer();

            // should already be handled by the public Reset call
            // _searchRequest = false;
            _resetRequest = false;
            // State is set to new before Reset
            //_cachedGamesState = initState;
            _cachedOwnMove = null;
            _cachedOpponentMove = null;

            _abortSearchSource.Dispose();
            _abortSearchSource = new();

            Debug.Assert(_cachedGamesState != null);

            _stateEvaluator.Reset(_cachedGamesState.Value);
            _brain.Reset();

        }

        private void RecalculateBestSequence(
            IStateEvaluator<StateEvalType, MoveType, OpponentMoveType> stateEvaluator,
            FantomGameState currentState)
        {
            float worstValue = float.MaxValue;
            float bestValue = float.MinValue;
            var currentBest = _brain.GetMovesSequence();
            var updatedBest = new MovesSequence<MoveType>();
            InputState state = InputState.FromState(currentState);
            for (int level = 0; level < currentBest.CurrentMoves; ++level)
            {
                // if the move is legal, recalculate the value
                var move = currentBest.GetMove(level);

                if (_movesGenerator.IsLegal(state, move))
                {
                    var value = stateEvaluator.Evaluate(move);
                    // during search such a Move would be abandoned
                    if (value <= 0)
                        break;

                    updatedBest.SetMove(move, value, level);
                    worstValue = Math.Min(worstValue, value);
                    bestValue = Math.Max(bestValue, value);                    

                    // next iteration will happen
                    if (level + 1 < currentBest.CurrentMoves)
                    {
                        state += move;
                        stateEvaluator.AssumeMove(move, level);
                    }
                }
                // this move isn't legal anymore, so update only up until it
                else
                {
                    break;
                }
            }
            _brain.UpdateMoveSequence(worstValue, bestValue, updatedBest);
            stateEvaluator.ForgetMove(0);

        }

        private void Loop()
        {
            while (_living)
            {
                
                // respond to the intermediary to pause / resume
                _pauseEvent.WaitOne();

                if (_terminated)
                    break;

                if (_resetRequest)
                {
                    lock (_searchLock)
                    {
                        Reset();
                    }
                }

                // NOTE: search might get cancelled by other Thread before started
                // no lock on _searchRequest, if false might get it next loop iteration
                // if true, but actually cancelled already, then the atomicity of the setup
                // will guarantee consistency
                if (_searchRequest)
                {
                    FantomGameState localGameState;
                    MoveType? localOwnMove = null;
                    OpponentMoveType? localOpMove = null;

                    // 0) prepare thread-safely values for preparing the search

                    lock (_searchLock)
                    {
                        _searchRequest = false;

                        // see NOTE above
                        if (_abortSearchSource.IsCancellationRequested)
                        {
                            _abortSearchSource?.Dispose();
                            _abortSearchSource = new();

                            continue;
                        }
                        Debug.Assert(_cachedGamesState != null);
                        localGameState = _cachedGamesState.Value;
                        localOwnMove = _cachedOwnMove;
                        localOpMove = _cachedOpponentMove;

                        _cachedOwnMove = null;
                        _cachedOpponentMove = null;

                        _searching = true;
                    }

                    // 1) prepare for the search

                    // NOTE: the order here is very important ! Own Before Opponent
                    // TODO: update all resources for multithread (possibly)
                    // update the first, copy to the rest
                    // update with Seekers Move
                    // NOTE: on the first round no move made 
                    if (localOwnMove != null)
                        _stateEvaluator.OwnPlay(localOwnMove.Value);


                    // _cachedSeekersMove = new();

                    // Update with Fantom Move
                    if (localOpMove != null)
                        _stateEvaluator.OpponentPlay(localOpMove.Value);


                    _stateEvaluator.PrepareForSearch();
                    // _cachedFantomMove = new();

                    // recalculate best sequence values from the updated stateEvaluator
                    // makes sure that the moves are legal under the updated state 
                    RecalculateBestSequence(_stateEvaluator, localGameState);
                   
                    // ----------------------------

                    // 2) Start the Search

                    if (_terminated)
                        break;

                    StartTimer();

                    // do the search
                    // it might either finish or be cancelled
                    Search(
                        _stateEvaluator,
                        new MovesSequence<MoveType>(),
                        InputState.FromState(localGameState),
                        float.MinValue,
                        float.MaxValue,
                        0,
                        _abortSearchSource.Token);


                    // 3) Search done or aborted

                    lock (_searchLock)
                    {
                        _searching = false;

                        // If the Search wasn't cancelled (ie hasn't played yet)
                        if (!_abortSearchSource.IsCancellationRequested)
                        {
                            // all worker threads are done at this point (main Search returned)
                            // if still my turn, make the best move; done by the brain
                            UseBrainToMove(null);
                        }

                        // If Search finished and was waiting for start, it might receiver an abort request, that will be reset
                        // here
                        _abortSearchSource?.Dispose();
                        _abortSearchSource = new();
                    }

                    // unlock the first Move
                    _brain.NewSearchSetup();

                    // ^^ shift the _movesSequence
                    // _bestSequence.RemoveMove(0);

                }

            }

            // Done in Terminate
            //_pauseEvent.Dispose();
        }

        // ----------------------------------------------------------------------------------
        // Search implementation

        // static int SEARCH_DEPTH = 1;

        MovesGenerator _movesGenerator;


        // Search generates all possible Moves in the given state, then evaluates them using its stateEvaluator.
        // Recursively it checks all of the moves as new states, updating the stateEvaluator in the process.
        // Remembers the best found move along each branch to update the best found path to play in the future.
        // TODO: uses limited resources to split into multiple parallel searches w. tasks
        private void Search(
            // Search resources
            // TODO: ResourceManager<StateEvaluator<SeekerState>>
            IStateEvaluator<StateEvalType, MoveType, OpponentMoveType> stateEvaluator,
            // Search tree branch
            MovesSequence<MoveType> pathHere,
            InputState stateHere,
            float branchBestValue,
            float branchWorstValue,
            // Search control
            int depth,
            CancellationToken abortSearch
            // Result in brain            
            )
        {
            
            if (abortSearch.IsCancellationRequested)
                return;

            // NOTE: if gonna switch to generating Moves one by one, ideally cache some, and keep the values because then 
            // gonna update the state evaluator and it will give bad results

            var moves = _movesGenerator.PossibleMoves(stateHere).ToArray();
            var values = moves.Select(stateEvaluator.Evaluate);

            if (moves.Length != 0)
            {
                if (abortSearch.IsCancellationRequested)
                    return;

                var move_values_sorted = moves.Zip(values).ToArray().OrderByDescending(pair => pair.Second).ToArray();
               
                // Array.Sort(moves, (a, b) => stateEvaluator.Evaluate(b).CompareTo(stateEvaluator.Evaluate(a)));
                var best = move_values_sorted[0].First;
                var bestMoveValue = move_values_sorted[0].Second;

                // Evaluation is ""kinda between"" 0 and MAX_EVAL_VALUE
                // Doing a 'bad' move now might result in a very good position in the future
                // But if doing a good move now is better then make it maybe 

                // Recursion stop
                if (depth >= _maxDepth)
                {
                    branchBestValue = Math.Max(branchBestValue, bestMoveValue);
                    branchWorstValue = Math.Min(branchWorstValue, bestMoveValue);

                    // This branch always ends with defeat, do anything else
                    if (branchWorstValue <= 0)
                        return;

                    // The current move at this depth level is worse lock and update
                    // TEMP: is better than the first move
                    // TODO: figure out how to properly determine which move sequence is better to go with
                    if (_brain.CanSetWith(branchWorstValue, branchBestValue))
                    {
                        pathHere.SetMove(best, stateEvaluator.Evaluate(best), depth);

                        // Should be fast-ish check to see if the branch is still fine
                        if (_brain.CanSetWith(pathHere))
                        {                            
                            _brain.SetBestMoveSequence(branchWorstValue, branchBestValue, pathHere);
                        }
                        // else
                        // this is a dead branch so return                        
                    }
                }
                // Recursion to future possible states
                else
                {
                    // TODO: parallel, if has free resource available (stateEvaluators) start a Task maybe
                    // Go over all Moves in order - belief that good states are more likely to lead to other good states
                    // when getting it, do _stateEval.updatefrom(stateEval given) then update with AssumeMove on the recursing with
                    for (int i = 0; i < moves.Length; ++i)
                    {
                        var makeMove = move_values_sorted[i].First;
                        var moveValue = move_values_sorted[i].Second;

                        // NOTE: they are sorted, and these are bad, so don't bother anymore
                        // this Move will lose the game
                        if (moveValue <= 0)
                            return;

                        pathHere.SetMove(makeMove, moveValue, depth);

                        if (abortSearch.IsCancellationRequested)
                        {
                            return;
                        }
                        // break to reset the mask TODO: rethink considering how masks are set ?
                        // return as the unset mask part of set on level above
                        if (!_brain.CanSetWith(pathHere))
                        {   
                            return;
                        }
                        // Apply mask to the State Evaluator - positions and tickets from the current move, next level
                        stateEvaluator.AssumeMove(makeMove, depth);

                        if (abortSearch.IsCancellationRequested)
                        {
                            return;
                        }

                        Search(
                            stateEvaluator,
                            pathHere,
                            stateHere + makeMove,
                            Math.Max(branchBestValue, moveValue),
                            Math.Min(branchWorstValue, moveValue),
                            depth + 1,
                            abortSearch);
                    }
                    // forget part of set on level above
                    //stateEvaluator.ForgetMove(depth);

                }

            }
            // else no moves generated -> ?? 
            // TODO: 
            // NOTE: update part of path if still better -> good for when Fantom finds he will lose in next couple of steps to still try to pick the best partial one so maybe if  Seekers make a mistake will be able to play anyway

            // TODO: make sure all workers are finished at this point
        }

    }
}
