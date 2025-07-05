using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesCore.Managers;
using FantomGamesIntermediary.Opponent;
using FantomGamesSystemUtils;
using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;

namespace FantomGamesIntermediary
{

    /// <summary>
    /// Manages the entities playing the Game. Passes valid information to involved parties when appropriate.
    /// </summary>
    public class FantomIntermediary : IFantomGamesCommander
    {
        private List<FacadeVisibilitySettings> _managedFacades = [];
        private IFantomGameInterface _gameManager;

        private IntermediarySettings _activeSettings;

        private SeekerOpponent seeker;
        private FantomOpponent fantom;

        /// <summary>
        /// Creates a new Fantom Games Intermediary with the given settings and the main player's facade.
        /// </summary>
        /// <param name="facade">The facade of the user.</param>
        /// <param name="iSettings">Initial intermediary settings.</param>
        /// <param name="fSettings">Initial fantom games settings.</param>
        /// <returns>The newly created FantomIntermediary instance.</returns>
        public static FantomIntermediary CreateIntermediary(
            IFantomGamesFacade facade,
            IntermediarySettings iSettings,
            FantomGameSettings fSettings)
        {
            return new FantomIntermediary(facade, iSettings, fSettings);
        }

        private FantomIntermediary(
            IFantomGamesFacade livingPlayerFacade,
            IntermediarySettings iSettings,
            FantomGameSettings fSettings)
        {
            _activeSettings = iSettings;

            // based on which role the Living Player is playing, set the visibility of the passed Facade
            _managedFacades.Add(new() { GameFacade = livingPlayerFacade, IsPublic = !iSettings.LivingPlayerIsFantom });

            // initial game setup
            var gm = FantomGameManager.CreateGame(fSettings);
            if (gm.Code != ReturnCodes.OK)
                SendError(gm.Message);
            _gameManager = gm.Value;

            _gameManager.AddGameStateChangeCallback(StateChanged);

            var gameBoard = _gameManager.GetBoard();
            Debug.Assert(gameBoard != null);

            RecreateOpponents();
            Debug.Assert(seeker != null && fantom != null);

            _gameManager.Start();

            if (iSettings.ComputerSeeker)
                seeker.Wake();

            if (iSettings.ComputerFantom)
                fantom.Wake();

            /* Check all tiles interconnected, change modes
            var board = _gameManager.GetBoard() ?? throw new Exception("Board Invalid");
            for (int mode = 0; mode < 4; ++mode)
            {
                for (int t = 1; t <= board.TileCount; ++t)
                {
                    for (int j = 0; j < board.CountNeighbors((TravelModes)mode, t); ++j)
                    {
                        // (u,v) edge -> (v,u) as well
                        int u = t;
                        int v = board.GetNeighbor((TravelModes)mode, t, j);
                        if (!board.IsNeighbor((TravelModes)mode, v, u))
                            Console.WriteLine($"{u} is neighbor of {v} but not the other way.");
                    }
                }
            }
            */
            /* Check for connectedness via multiple modes of travel
            var board = _gameManager.GetBoard() ?? throw new Exception("Board Invalid");
            for (int mode = 0; mode < 4; ++mode)
            {
                for (int t = 1; t <= board.TileCount; ++t)
                {
                    for (int j = 0; j < board.CountNeighbors((TravelModes)mode, t); ++j)
                    {
                        // (u,v) edge -> (v,u) as well
                        int u = t;
                        int v = board.GetNeighbor((TravelModes)mode, t, j);

                        if (u > v)
                            continue;

                        // is there edge between them via a different mode?
                        for (int mode2 = mode+1; mode2 < 4; ++mode2)
                        {
                            if (board.IsNeighbor((TravelModes)mode2, u, v))
                                Console.WriteLine($"{u} and {v} are connected via {mode} and also via {mode2}.");
                        }
                    }
                }
            }
            */
        }

        private void RecreateOpponents()
        {
            var gameBoard = _gameManager.GetBoard();
            Debug.Assert(gameBoard != null);

            seeker?.Terminate();
            fantom?.Terminate();

            seeker = new SeekerOpponent(this, gameBoard, _gameManager.GetActiveSettings());
            fantom = new FantomOpponent(this, gameBoard, _gameManager.GetActiveSettings());

            // the 0-th one is always the user
            if (_managedFacades.Count > 1)
            {
                _managedFacades.RemoveAt(2);
                _managedFacades.RemoveAt(1);
            }
            _managedFacades.Add(new() { GameFacade = seeker, IsPublic = true });
            _managedFacades.Add(new() { GameFacade = fantom, IsPublic = false });
        }


        /// <summary>
        /// Change the intermediary settings.
        /// </summary>
        /// <param name="newSettings">The new settings.</param>
        public void ChangeSettings(IntermediarySettings newSettings)
        {
            _activeSettings = newSettings;
           
            // index 0 = living player always
            _managedFacades[0] = new() { GameFacade = _managedFacades[0].GameFacade, IsPublic = !newSettings.LivingPlayerIsFantom };

            // settings changed -> reset
            ResetIntermediary();
        }

        /// <summary>
        /// Get current intermediary settings.
        /// </summary>
        /// <returns></returns>
        public IntermediarySettings GetIntermediarySettings()
        {
            return _activeSettings;
        }

 
        private void ResetIntermediary()
        {       
            if (_activeSettings.ComputerSeeker)
            {
                seeker.Wake();                
            }
            else
            {
                seeker.Sleep();
            }

            if (_activeSettings.ComputerFantom)
            {
                fantom.Wake();
            }
            else
            {
                fantom.Sleep();
            }

            Reset();
        }

        /// <summary>
        /// Stop the intermediary completely.
        /// </summary>
        public void Exit()
        {
            seeker.Terminate();
            fantom.Terminate();

            _gameManager.Stop();
        }

        // ---------------------------------------------------------------

        private void StateChanged(long id, GameStates from, GameStates to)
        {
            if (from == GameStates.Choosing)
            {
                // Notify.
                var gameStatePublic = GetPublicGameState();
                var gameStatePrivate = GetPrivateGameState();

                // in choosing, game state has to be fine
                Debug.Assert(gameStatePublic != null);
                Debug.Assert(gameStatePrivate != null);

                _managedFacades.ForEachPublicOnly(f => f.GameStarted(gameStatePublic.Value));
                _managedFacades.ForEachPrivate(f => f.GameStarted(gameStatePrivate.Value));

            }

            if (to == GameStates.GameOver)
            {
                // NOTE: if in GameOver state, result should be fine, but just in case...
                var gameResult = _gameManager.GetGameResult();

                if (gameResult.Code == ReturnCodes.OK)
                    _managedFacades.ForEachPublic(f => f.GameFinished(gameResult));
                else
                    SendError(gameResult.Message);
            }

            if (to == GameStates.FantomTurn)
            {
                _managedFacades.ForEachPublic(f => f.FantomTurnBegin(_gameManager.CurrentMove()));
            }

            if (to == GameStates.SeekersTurn && from == GameStates.PostFantomTurn)
            {
                _managedFacades.ForEachPublic(f => f.FantomTurnEnd());
            }

            if (to == GameStates.SeekersTurn && _gameManager.GetActiveSettings().SeekerOrder)
            {
                _managedFacades.ForEachPublic(f => f.SeekerTurnBegin(SeekerIndex()));
            }

            if (from == GameStates.SeekersTurn && _gameManager.GetActiveSettings().SeekerOrder)
            {
                _managedFacades.ForEachPublic(f => f.SeekerTurnEnd(SeekerIndex()));
            }

            if (to == GameStates.RoundOver)
            {
                _managedFacades.ForEachPublic(f => f.RoundOver(_gameManager.CurrentRound()));
            }
        }

        // General controls ---------------------------------------------------------------

        public FantomGameState? GetGameState()
        {
            var state = _gameManager.GetGameState();
            if (state.Code == ReturnCodes.OK)
                return state;
            else
            {
                SendError(state.Message);
                return null;
            }
        }

        public FantomGameState? GetPrivateGameState()
        {
            return GetGameState();
        }

        public FantomGameState? GetPublicGameState()
        {
            var state = _gameManager.GetSeekersPlayerTools().GetGameState();
            if (state.Code == ReturnCodes.OK)
                return state;
            else
            {
                SendError(state.Message);
                return null;
            }
        }


        public FantomGameSettings GetActiveSettings()
        {
            return _gameManager.GetActiveSettings();
        }

        public IReadOnlyFantomBoard? GetBoard()
        {
            return _gameManager.GetBoard();
        }

        public bool Restart(FantomGameSettings fSettings)
        {
            var retCode = _gameManager.Restart(fSettings);

            if (retCode)
            {
                // TODO: Create new Opponents !! 
                RecreateOpponents();

                if (_activeSettings.ComputerSeeker)
                    seeker.Wake();

                if (_activeSettings.ComputerFantom)
                    fantom.Wake();

                _managedFacades.ForEachPublic(f => f.GameRestarted(fSettings));
            }
            else
            {
                SendError(retCode.Message);
            }

            return retCode;
        }

        public bool Reset()
        {
            bool result = _gameManager.Reset();

            if (result)
                _managedFacades.ForEachPublic(f => f.GameReset());
            else
                SendError("Game cannot be Reset.");

            return result;
        }


        // Fantom controls ---------------------------------------------------------------

        public bool IsFantomTurn()
        {
            return _gameManager.IsFantomTurn();
        }

        bool IFantomCommander.IsOwnTurn()
        {
            return IsFantomTurn();
        }

        public bool PlaceFantomAt(int tileIndex)
        {
            var placed = _gameManager.PlaceFantomAt(tileIndex);

            if (placed)
            {
                // where the Fantom is placed is private for the Fantom only
                _managedFacades.ForEachPrivate(f => f.FantomPlacedAt(tileIndex));
            }
            else
            {
                SendError(placed.Message, false);
            }

            return placed;
        }

        bool IFantomCommander.PlaceAt(int tile)
        {
            return PlaceFantomAt(tile);
        }


        public bool MoveFantom(int tile, TicketKinds via)
        {
            var moved = _gameManager.GetFantomPlayerTools().Move(tile, via);

            if (moved)
            {

                // announce used Ticket by Fantom
                _managedFacades.ForEachPublic(f => f.FantomUsedTicket(via));

                // Fantom moved, let private Facades know
                _managedFacades.ForEachPrivate(f => f.FantomMovedTo(tile, via));
                _managedFacades.ForEachPublic(f => f.FantomHistoryMoveTo(tile));

                // if revealed announce to all 
                if (_gameManager.IsFantomRevealing())
                    _managedFacades.ForEachPublic(f => f.FantomRevealedAt(tile));
            }
            else
            {
                SendError(moved.Message, false);
            }

            return moved;
        }

        bool IFantomCommander.Move(int tile, TicketKinds via)
        {
            return MoveFantom(tile, via);
        }

        public bool UseDouble()
        {
            var used = _gameManager.GetFantomPlayerTools().UseDouble();

            if (used)
            {
                _managedFacades.ForEachPublic(f => f.FantomUsedDouble());
                _managedFacades.ForEachPublic(f => f.FantomUsedTicket(TicketKinds.Double));
            }
            else
            {
                // could send publicly (Tickets are known to all) but for safety
                SendError(used.Message, false);
            }

            return used;
        }

        public int? CannotMoveFantom()
        {
            var pos = _gameManager.GetFantomPlayerTools().CannotMove();

            // Truly couldn't Move
            if (pos.Code == ReturnCodes.OK)
            {
                // Instead of Ticket we tell everyone that Fantom remained
                _managedFacades.ForEachPublic(f => f.FantomCouldNotBeMoved());
                return null;

                // Could be Moved or got a Fail
            }
            else
            {
                // Private message with the possible position
                SendError(pos.Message, false);
                return (pos.Code == ReturnCodes.Fail) ? pos : null;
            }
        }

        int? IFantomCommander.CannotMove()
        {
            return CannotMoveFantom();
        }

        // Seekers' controls ----------------------------------------

        public bool IsSeekersTurn()
        {
            return _gameManager.IsSeekersTurn();
        }

        bool ISeekersCommander.IsOwnTurn()
        {
            return IsSeekersTurn();
        }

        public int SeekerIndex()
        {
            return _gameManager.GetSeekerIndex();
        }

        public bool PlaceSeekerAt(int seekerIndex, int tileIndex)
        {
            var placed = _gameManager.PlaceSeekerAt(seekerIndex, tileIndex);

            if (placed)
            {
                // where the Seeker is placed is private for the Fantom only
                _managedFacades.ForEachPublic(f => f.SeekerPlacedAt(seekerIndex, tileIndex));
            }
            else
            {
                SendError(placed.Message);
            }

            return placed;
        }

        bool ISeekersCommander.PlaceAt(int seekerIndex, int tile)
        {
            return PlaceSeekerAt(seekerIndex, tile);
        }

        public bool MoveSeeker(int seekerIndex, int tile, TicketKinds via)
        {
            var moved = _gameManager.GetSeekersPlayerTools().Move(seekerIndex, tile, via);

            if (moved)
            {
                // Fantom moved, let private Facades know
                _managedFacades.ForEachPublic(f => f.SeekerMovedTo(seekerIndex, tile, via));
            }
            else
            {
                SendError(moved.Message);
            }

            return moved;
        }

        bool ISeekersCommander.Move(int seekerIndex, int tile, TicketKinds via)
        {
            return MoveSeeker(seekerIndex, tile, via);
        }


        public int? CannotMoveSeeker()
        {
            var pos = _gameManager.GetSeekersPlayerTools().CannotMove();

            // Truly couldn't Move
            if (pos.Code == ReturnCodes.OK)
            {

                if (_gameManager.GetActiveSettings().SeekerOrder)
                    _managedFacades.ForEachPublic(f => f.SeekerCouldNotBeMoved(SeekerIndex()));
                else
                    _managedFacades.ForEachPublic(f => f.SeekersCouldNotBeMoved());

                return null;

            }
            // Could be Moved or got a Fail
            else
            {
                // Private message with the possible position
                SendError(pos.Message);
                return (pos.Code == ReturnCodes.Fail) ? pos : null;
            }
        }

        int? ISeekersCommander.CannotMove()
        {
            return CannotMoveSeeker();
        }

        bool ISeekersCommander.ConfirmTurnOver()
        {
            return ConfirmSeekersTurnOver();
        }

        bool IFantomCommander.ConfirmTurnOver()
        {
            return ConfirmFantomTurnOver();
        }

        public bool ConfirmFantomTurnOver()
        {
            return _gameManager.ConfirmFantomTurnOver();
        }

        public bool ConfirmSeekersTurnOver()
        {
            return _gameManager.ConfirmSeekersTurnOver();
        }

        // ----------------------------------------------------------
        private void SendError(string? message, bool IsPublic = true)
        {
            if (message is not null)
            {
                if (IsPublic)
                    _managedFacades.ForEachPublic(f => f.ErrorMessage(message));
                else
                    _managedFacades.ForEachPrivate(f => f.ErrorMessage(message));
            }
        }

    }


    static class FacadeListExtensions
    {
        // NOTE: all public info is also available to private Facades. But not all is available to public Facades.
        public static void ForEachPublic(this List<FacadeVisibilitySettings> list, Action<IFantomGamesFacade> action)
        {

            foreach (var facade in list)
            {
                action(facade.GameFacade);
            }
        }

        public static void ForEachPublicOnly(this List<FacadeVisibilitySettings> list, Action<IFantomGamesFacade> action)
        {

            foreach (var facade in list)
            {
                if (facade.IsPublic)
                    action(facade.GameFacade);
            }
        }

        public static void ForEachPrivate(this List<FacadeVisibilitySettings> list, Action<IFantomGamesFacade> action)
        {

            foreach (var facade in list)
            {
                if (!facade.IsPublic)
                    action(facade.GameFacade);
            }
        }
    }
}
