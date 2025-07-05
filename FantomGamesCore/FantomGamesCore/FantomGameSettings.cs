using FantomGamesCore.Interfaces;

namespace FantomGamesCore
{
    /// <summary>
    /// A structure for passing game settings to the Game Core.
    /// </summary>
    public readonly record struct FantomGameSettings
    {
        // GS2.1 - Control over Actors Count with 1 Fantom always
        /// <summary>
        /// How many Detective Actors are playing
        /// </summary>
        public int DetectivesCount { get; init; }

        /// <summary>
        /// How many Bobby Actors are playing
        /// </summary>
        public int BobbiesCount { get; init; }

        /// <summary>
        /// Helper property which gives the sum of Detectives and Bobbies Counts.
        /// </summary>
        public int SeekersCount => DetectivesCount + BobbiesCount;

        // GS2.3, GS2.6 - All Tickets with Names

        /// <summary>
        /// The Tickets in the common Supply.
        /// </summary>
        public required TicketGroup[] SupplyTickets { get; init; }

        // GS2.2 - Starting Tickets for the Detectives and the Fantom
        /// <summary>
        /// The number of Tickets given to each Detective.
        /// </summary>
        public required TicketGroup[] DetectiveStartingTickets { get; init; }
        /// <summary>
        /// The number of Tickets given to the Fantom
        /// </summary>
        public required TicketGroup[] FantomStartingTickets { get; init; }


        // GS2.4 - Total Rounds or Moves (see Victory conditions)        
        /// <summary>
        /// The maximum number of Rounds
        /// </summary>
        public uint MaxRounds { get; init; }

        /// <summary>
        /// The maximum number of Moves 
        /// </summary>
        public uint MaxMoves { get; init; }


        // GS2.5 - Victory/Draw/Defeat conditions
        /// <summary>
        /// Represents a number of conditions that all have to be met in CNF.
        /// </summary>
        public GameOverConditions[] FantomVictoryConditions { get; init; } = [(GameOverConditions.NCR | GameOverConditions.SM | GameOverConditions.DC)];

        /// <summary>
        /// Represents a number of conditions that all have to be met in CNF.
        /// </summary>
        public GameOverConditions[] SeekersVictoryConditions { get; init; } = [GameOverConditions.FC | GameOverConditions.FM];

        /// <summary>
        /// Represents a number of conditions that all have to be met in CNF.
        /// </summary>
        public GameOverConditions[] DrawConditions { get; init; } = [GameOverConditions.None];

        // GS2.7 - Order of Detective moves matters
        /// <summary>
        /// Whether the order in which Detective turn Moves are accepted matters. If true, only one order is accepted.
        /// </summary>
        public bool SeekerOrder { get; init; } = true;

        // GS2.8 - First Fantom Move must confirm his current location
        /// <summary>
        /// Whether Fantom's first Move is 'stationary' or should lead to a new position.
        /// </summary>
        public bool FantomFirstMoveStatic { get; init; } = true;


        // GS4 - Which Moves are revealing.Default 3, 8, 13, 18, 24.
        /// <summary>
        /// Which Moves the Fantom position is revealed to the Seekers at (they can ask for it).
        /// </summary>
        public uint[] RevealingMoves { get; init; } = [3, 8, 13, 18, 24];

        // GS5 - Starting positions
        /// <summary>
        /// A set of starting positions for the Seekers chosen randomly. If null, has to be specified manually at runtime.
        /// </summary>
        public int[]? SeekerStartingPositions { get; init; } = null;

        /// <summary>
        /// A set of starting positions for the Detectives chosen randomly. If null, has to be specified manually at runtime.
        /// </summary>
        public int[]? FantomStartingPositions { get; init; } = null;

        // GS6+ - Mechanics and Abilities
        /// <summary>
        /// Represents which additional functions are enabled based on <see cref="GameMechanics"/>.
        /// </summary>
        public GameMechanics MechanicsAndAbilities { get; init; } = GameMechanics.None;


        // GS3 - The playing board
        /// <summary>
        /// Determines how the game board is loaded. If null will try to use the currently loaded Game Board if one exists.
        /// </summary>
        public IGameBoardLoader? GameBoardLoader { get; init; } = null;

        public FantomGameSettings() {}

        // GS1 - Logging
        /// <summary>
        /// If null, logging is disabled. Else logging done through this Writer.
        /// </summary>
        public TextWriter? LoggerDestination { get; init; } = null;

        /// <summary>
        /// Checks if the given settings are correct.
        /// </summary>
        /// <returns></returns>
        public ReturnCode<bool> CheckValidity()
        {

            // At least one opponent else the game is pointless
            if (DetectivesCount < 0 || BobbiesCount < 0 || DetectivesCount + BobbiesCount == 0)
            {
                return new(false, ReturnCodes.InvalidSettings, 
                    $"There has to be at least one opponent for the Fantom. Got : {DetectivesCount} Detectives and {BobbiesCount} Bobbies.");
            }

            if (DetectivesCount + BobbiesCount > MAXIMUM_SEEKERS_COUNT)
            {
                return new(false, ReturnCodes.InvalidSettings, 
                    $"There can only be {MAXIMUM_SEEKERS_COUNT} opponents for the Fantom at most. Got : {DetectivesCount + BobbiesCount}");
            }

            // At least one play cycle else the game is pointless
            if (MaxMoves <= 0 || MaxRounds <= 0)
            {
                return new(false, ReturnCodes.InvalidSettings,
                    $"Game has to have at least one Round and Move. Got : {MaxMoves} Moves {MaxRounds} Rounds");
            }

            // If specified a set of positions, have to have at least enough to place all Actors
            if (SeekerStartingPositions != null && SeekerStartingPositions.Length < DetectivesCount + BobbiesCount)
            {
                return new(false, ReturnCodes.InvalidSettings,
                    $"Not enough Detective Starting Positions given!");
            }
            if (FantomStartingPositions != null && FantomStartingPositions.Length < 1)
            {
                return new(false, ReturnCodes.InvalidSettings,
                    $"Not enough Fantom Starting Positions given!");
            }

            var actorsCount = 1 + DetectivesCount + BobbiesCount;
            // At least as many tiles as Actors
            if (GameBoardLoader != null && GameBoardLoader.TileCount < MAXIMUM_SEEKERS_COUNT + 1)
            {
                return new(false, ReturnCodes.InvalidSettings,
                    $"The given board does not have enough tiles for all Actors. Got : {GameBoardLoader.TileCount} need {actorsCount}");
            }

            // all passed
            return new(true);
        }

        /// <summary>
        /// How many known ticket kinds there are.
        /// </summary>
        public const int TICKET_KINDS_COUNT = 7;

        /// <summary>
        /// How many seekers there might be at most in one game.
        /// </summary>
        public const int MAXIMUM_SEEKERS_COUNT = 5;
    }

    /// <summary>
    /// All known Ticket types.
    /// </summary>
    public enum TicketKinds
    {
        /// <summary>
        /// Represents the most common of travel modes' tickets.
        /// </summary>
        Mode1,
        /// <summary>
        /// Represents the second most common of travel modes' tickets.
        /// </summary>
        Mode2, 
        /// <summary>
        /// Represents the rarest of travel modes' tickets.
        /// </summary>
        Mode3,
        /// <summary>
        /// Represents the Fantom's secret movement ticket.
        /// </summary>
        Black,
        /// <summary>
        /// Represents the Fantom's Double ticket.
        /// </summary>
        Double,
        /// <summary>
        /// Represents the Fantom's River movement ticket.
        /// </summary>
        River,
        /// <summary>
        /// Represents the Fantom's Balloon escape ticket.
        /// </summary>
        Balloon

    }

    /// <summary>
    /// A helper struct for representing a ticket kind with an amount to give to an Actor.
    /// </summary>
    public readonly record struct TicketGroup
    {
        /// <summary>
        /// Creates a new TicketGroup.
        /// </summary>
        /// <param name="TicketKind">The kind of the ticket for this group.</param>
        /// <param name="Count">The amount of this ticket kind.</param>
        public TicketGroup(TicketKinds TicketKind, int Count) 
        {
            this.TicketKind = TicketKind;
            this.Count = Count;
        }

        /// <summary>
        /// The ticket kind of this group.
        /// </summary>
        public TicketKinds TicketKind { get; init; }
        /// <summary>
        /// The amount of tickets of this group.
        /// </summary>
        public int Count { get; init; }
    }

    /// <summary>
    /// Various Conditions checked by the Game System. Can be combined into Victory / Defeat / Draw conditions for the game.
    /// </summary>
    [Flags]
    public enum GameOverConditions
    {
        /// <summary>
        /// Always False
        /// </summary>
        None = 0,

        /// <summary>
        /// Fantom Caught
        /// </summary>
        FC = 1,

        /// <summary>
        /// Not Caught by the last Round
        /// </summary>
        NCR = 2,

        /// <summary>
        /// Fantom cannot Move
        /// </summary>
        FM = 4,

        /// <summary>
        /// Seekers cannot Move
        /// </summary>
        SM = 8,

        /// <summary>
        /// Robbed at least one Bank
        /// </summary>
        RB = 16,

        /// <summary>
        /// Hasn't robbed any Bank
        /// </summary>
        NRB = 32,

        /// <summary>
        /// Done 3 Crimes
        /// </summary>
        DC = 64,

        /// <summary>
        /// Not Caught by last Move
        /// </summary>
        NCM = 128
    }

    /// <summary>
    /// Various game mechanics and Actor abilities that can be enabled. To be used as flags.
    /// </summary>
    [Flags]
    public enum GameMechanics
    {
        None = 0,
        /// <summary>
        /// Some tiles have a Bank, that the Fantom can Rob.
        /// </summary>
        BankMechanic = 1,
        /// <summary>
        /// Detectives can ask for three possible tiles where the Fantom is.
        /// </summary>
        InformerMechanic = 2,
        /// <summary>
        /// Fantom can commit a crime at one of three locations.
        /// </summary>
        CrimeMechanic = 4,
        /// <summary>
        /// Reveals Fantom's tiles District property.
        /// </summary>
        Manhunt = 8,
        /// <summary>
        /// Bans Fantom from using either M2 or M3 next turn (Move).
        /// </summary>
        TransportationBan = 16,
        /// <summary>
        /// Ask if Fantom's tile Area type is Park, Landmark or River - answer is yes/no.
        /// </summary>
        Interrogation = 32,
        /// <summary>
        /// Ask Fantom's position from two turns past.
        /// </summary>
        SearchingClues = 64,
        /// <summary>
        /// Blocks the tile the Detective is on and Fantom cannot move to it anymore.
        /// </summary>
        Roadblock = 128,
        /// <summary>
        /// Let's Fantom take a Double or Black Ticket from the Supply.
        /// </summary>
        TakeTicket = 256,
        /// <summary>
        /// Fantom can announce he's hiding instead of revealing current position.
        /// </summary>
        DeepDive = 512,
        /// <summary>
        /// Fantom can move to a random starting position.
        /// </summary>
        Escape = 1024
    }
}
