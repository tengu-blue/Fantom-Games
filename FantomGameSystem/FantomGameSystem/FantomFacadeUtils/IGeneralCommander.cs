using FantomGamesCore;
using FantomGamesCore.Interfaces;

namespace FantomGamesSystemUtils
{
    /// <summary>
    /// A general purpose interface for controlling aspects of the Intermediary and the game.
    /// </summary>
    public interface IGeneralCommander
    {

        
        /// <summary>
        /// Restarts the game with new settings.
        /// </summary>
        /// <param name="fSettings"></param>
        /// <returns>True if the given settings led to a validly set up game.</returns>
        bool Restart(FantomGameSettings fSettings);

        /// <summary>
        /// Resets the game with current settings.
        /// </summary>
        /// <returns>True if reset was successful - if False a restart is probably needed.</returns>
        bool Reset();

        /// <summary>
        /// Get the current Game Settings.
        /// </summary>
        /// <returns>Active GameSettings.</returns>
        FantomGameSettings GetActiveSettings();

        /// <summary>
        /// Gets the current Playing Board.
        /// </summary>
        /// <returns>Current Playing Board.</returns>
        IReadOnlyFantomBoard? GetBoard();

        /// <summary>
        /// Returns true if it is the Fantom's turn.
        /// </summary>
        /// <returns>True if it is the Fantom's turn.</returns>
        bool IsFantomTurn();

        /// <summary>
        /// Returns true if Fantom's turn was successfully ended.
        /// </summary>
        /// <returns>True if the Fantom's turn was ended.</returns>
        bool ConfirmFantomTurnOver();

        /// <summary>
        /// Returns true if it is the Seekers' turn.
        /// </summary>
        /// <returns>True if it is the Seekers' turn.</returns>
        bool IsSeekersTurn();

        /// <summary>
        /// If Seekers turn, will return the index of the currently playing Seeker if ordering is enabled in the Game settings.
        /// </summary>
        /// <returns>Index of the Seeker if conditions are met. Undefined otherwise.</returns>
        int SeekerIndex();

        /// <summary>
        /// Returns true if Seekers' turn was successfully ended.
        /// </summary>
        /// <returns>True if the Seekers' turn was ended.</returns>
        bool ConfirmSeekersTurnOver();

    }
}
