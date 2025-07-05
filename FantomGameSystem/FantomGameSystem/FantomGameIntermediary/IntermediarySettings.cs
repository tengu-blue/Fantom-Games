using FantomGamesSystemUtils;

namespace FantomGamesIntermediary
{
    /// <summary>
    /// A structure for the intermediary settings.
    /// </summary>
    public readonly struct IntermediarySettings
    {
        /// <summary>
        /// True if the user is playing as the Fantom.
        /// </summary>
        public readonly bool LivingPlayerIsFantom { get; init; }
        /// <summary>
        /// True if the user is playing as the Seekers.
        /// </summary>
        public readonly bool LivingPlayerIsSeekers { get; init; }
    
        /// <summary>
        /// True if the intermediary should supply the computer Seekers opponent.
        /// </summary>
        public readonly bool ComputerSeeker { get; init; }
        /// <summary>
        /// True if the intermediary should supply the computer Fantom opponent.
        /// </summary>
        public readonly bool ComputerFantom { get; init; }
    }

    internal struct FacadeVisibilitySettings
    {
        public bool IsPublic { get; set; }
        public IFantomGamesFacade GameFacade { get; init; }
    }

}
