using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;
using FantomGamesIntermediary.Opponent.Parts.FantomParts;
using FantomGamesIntermediary.Opponent.Parts.SeekerParts;
using FantomGamesSystemUtils;
using System.Diagnostics;

namespace FantomGamesIntermediary.Opponent
{
    internal class FantomOpponent(
        IFantomCommander _commander,
        IReadOnlyFantomBoard _board,
        FantomGameSettings _settings) :

        IOpponent<FantomOpponent>
    {

        // 10 seconds to think
        private static int _thinkingTime = 10 * 1000;

        TimedSearch<FantomMove, SeekersMove, FantomBrain, FantomState, FantomMovesGenerator, FantomStateEvaluator> _searcher
            = TimedSearch<FantomMove, SeekersMove, FantomBrain, FantomState, FantomMovesGenerator, FantomStateEvaluator>.CreateFantomThinker(
                _commander,
                _board,
                _settings,
                // If starting positions are not specified than all tile positions are possible
                _settings.FantomStartingPositions is not null ?
                    _settings.FantomStartingPositions :
                    _board.AllTiles,
                _settings.MaxRounds,
                _settings.RevealingMoves,
                _thinkingTime);

        // ------------------------------------------------------------------------

        public void Sleep()
        {
            _searcher.Sleep();
        }

        public void Wake()
        {
            _searcher.Wake();
        }

        public void Terminate()
        {
            _searcher.Terminate();
        }


        public void GameReset()
        {
           // var state = _commander.GetPrivateGameState();
            //Debug.Assert(state != null);

           // _searcher.Reset(state.Value);

           // Console.WriteLine("RESETING");
        }

        public void GameRestarted(FantomGameSettings newSettings)
        {
            _searcher.AbortSearch();
        }

        public void GameFinished(FantomGameResult gameResult)
        {
            _searcher.AbortSearch();
        }

        // ------------------------------------------------------------------------

        private readonly int[] _cachedSeekerPositions = new int[_settings.SeekersCount];
        private int _cachedFantomPosition = 0;

        private bool _fantomMoved = false;
        private FantomMove _cachedFantomMove;

        private bool _seekerMoved = false;
        private SeekersMove _cachedSeekersMove;

        public void GameStarted(FantomGameState gameState)
        {            
            _seekerMoved = false;
            _fantomMoved = false;

            for (int seekerIndex = 0; seekerIndex < _settings.SeekersCount; ++seekerIndex)
                _cachedSeekerPositions[seekerIndex] = gameState.GetSeekerPosition(seekerIndex);

            // NOTE: should always be valid
            _cachedFantomPosition = gameState.FantomPosition;

            _searcher.Reset(gameState);
        }

        public void FantomTurnBegin(uint fantomMove) 
        {            
            // At this point the state should always be valid (since the Fantom has played)
            var state = _commander.GetPrivateGameState();
            Debug.Assert(state != null);

            _searcher.CacheGameState(state.Value);
            if (_fantomMoved) _searcher.CacheOpponentMove(_cachedSeekersMove);

            // technically should be at FantomTurnBegin (when last Seeker Moved) but here is fine too
            if (_seekerMoved) _searcher.CacheOwnMove(_cachedFantomMove);

            // Start a new Search
            _searcher.StartSearch();

            _seekerMoved = false;
            _fantomMoved = false;
        }

        public void FantomUsedTicket(TicketKinds ticketKind)
        {
            // NOTE: double Tickets before the actual movement tickets
            var state = _commander.GetPrivateGameState();
            Debug.Assert(state != null);

            if (!_fantomMoved)
            {
                _fantomMoved = true;
                _cachedFantomMove = new(_cachedFantomPosition);
            }

            if (ticketKind == TicketKinds.Double)
            {
                _cachedFantomMove.SetDouble();
            }
            else if (state.Value.FantomPosition > 0)
            {
                _cachedFantomMove.SetMove(_cachedFantomMove.MovesCount, state.Value.FantomPosition, (int)ticketKind);
                _cachedFantomPosition = state.Value.FantomPosition;
            }
            else
            {
                // NOTE: should never get here...
                _cachedFantomMove.SetMove(_cachedFantomMove.MovesCount, (int)ticketKind);
                _cachedFantomPosition = -1;
            }

        }

        public void SeekerMovedTo(int seekerIndex, int tile, TicketKinds via)
        {
            if (!_seekerMoved)
            {
                _seekerMoved = true;
                _cachedSeekersMove = new(_cachedSeekerPositions);
            }

            _cachedSeekersMove.SetMove(seekerIndex, tile, (int)via);
            _cachedSeekerPositions[seekerIndex] = tile;
        }


        // WOULD BE USED FOR Fantom Games (not SY)

        public void FantomPlacedAt(int tile) { }
        public void SeekerPlacedAt(int seekerIndex, int tile) { }

        // NOT USED ---

        public void FantomTurnEnd() { }
        public void RoundOver(uint round) { }
        public void SeekerTurnEnd(int seekerIndex) { }
        public void FantomUsedDouble() { }
        public void FantomMovedTo(int tile, TicketKinds via) { }
        public void FantomCouldNotBeMoved() { }
        public void FantomRevealedAt(int tile) { }
        public void SeekerCouldNotBeMoved(int seekerIndex) { }
        public void SeekersCouldNotBeMoved() { }
        public void SeekerTurnBegin(int seekerIndex) { }

        public void ErrorMessage(string message) { }

        public void FantomHistoryMoveTo(int tile) {}
    }
}
