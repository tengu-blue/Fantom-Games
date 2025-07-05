using FantomGamesCore;
using FantomGamesIntermediary;
using FantomGamesSystemUtils;

namespace ConsoleFantomGamesFacade
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // parse settings from args, no support for runtime settings changing
            var facade = new ConsoleFacade();

            if (!Directory.Exists("../Logs/"))
                Directory.CreateDirectory("../Logs/");

            using TextWriter logger = new StreamWriter($"../Logs/{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt");
            var intermediary = FantomIntermediary.CreateIntermediary(
                facade,
                new()
                {
                    LivingPlayerIsFantom = false,
                    LivingPlayerIsSeekers = true,
                },
                new()
                {
                    DetectivesCount = 5,
                    BobbiesCount = 0,

                    MaxRounds = 22,
                    MaxMoves = 24,

                    DetectiveStartingTickets =
                        [
                            new TicketGroup(TicketKinds.Mode1, 11),
                                new TicketGroup(TicketKinds.Mode2, 8),
                                new TicketGroup(TicketKinds.Mode3, 4),
                        ],
                    FantomStartingTickets =
                        [
                            new TicketGroup(TicketKinds.Mode1,  2),     // 4
                                new TicketGroup(TicketKinds.Mode2,  5),     // 3
                                new TicketGroup(TicketKinds.Mode3,  3),     // 3
                                new TicketGroup(TicketKinds.Black,  5),
                                new TicketGroup(TicketKinds.Double, 2),
                        ],
                    SupplyTickets = [],

                    SeekerStartingPositions = [13, 26, 29, 34, 50, 53, 91, 94, 103, 112, 117, 123, 138, 141, 155, 174],
                    FantomStartingPositions = [35, 45, 51, 71, 78, 104, 106, 127, 132, 146, 166, 170, 172],

                    FantomFirstMoveStatic = false,

                    LoggerDestination = logger,

                    SeekerOrder = false,

                    GameBoardLoader = new IntermediaryFantomBoardLoader("../GameBoard.txt")

                }
            );

            // main loop.
            // translation of text commands to ICommander method calls.

            bool running = true;
            CommandParser parser = new(intermediary, facade);

            while (running)
            {
                string? command = Normalize(Console.ReadLine());
                if (command == null || command == "exit")
                {
                    running = false;
                    intermediary.Exit();
                    break;
                }

                if (!parser.Parse(command))
                {
                    Console.Error.WriteLine($"'{command}' is not valid.");
                }
            }

        }

        private static string? Normalize(string? raw)
        {
            return raw?.Trim().ToLower();
        }

    }

    internal class ConsoleFacade : IFantomGamesFacade, IFacadeCommander
    {

        FantomGameState lastState;

        public void ErrorMessage(string message)
        {
            Console.Error.WriteLine(message);
        }

        public void FantomCouldNotBeMoved()
        {
            Console.WriteLine("Fantom stayed.");
        }

        public void FantomHistoryMoveTo(int tile)
        {
            // pass 
        }

        public void FantomMovedTo(int tile, TicketKinds via)
        {
            Console.WriteLine($"Fantom Moved to {tile} using {via} : {Enum.GetName(via)}.");
        }

        public void FantomPlacedAt(int tile)
        {
            Console.WriteLine($"Fantom Placed at {tile}.");
        }

        public void FantomRevealedAt(int tile)
        {
            Console.WriteLine($"Fantom is at {tile}.");
        }

        public void FantomTurnBegin(uint fantomMove)
        {
            Console.WriteLine($"Fantom is playing.");
        }

        public void FantomTurnEnd()
        {
            Console.WriteLine($"Fantom has played.");
        }

        public void FantomUsedDouble()
        {
            Console.WriteLine($"Fantom is Moving twice.");
        }

        public void FantomUsedTicket(TicketKinds ticketKind)
        {
            Console.WriteLine($"Fantom Moved using {ticketKind} : {Enum.GetName(ticketKind)}.");
        }

        public void GameFinished(FantomGameResult gameResult)
        {
            Console.Write("Game is over - ");
            if (gameResult.FantomWon)
                Console.WriteLine("Fantom Won!");
            else if (gameResult.SeekersWon)
                Console.WriteLine("Seekers Won!");
            else
                Console.WriteLine("No one won :(");
        }

        public void GameReset()
        {
            Console.WriteLine("Game reset.");
        }

        public void GameRestarted(FantomGameSettings newSettings)
        {
            // TODO: ?
            Console.WriteLine("Game restarted.");
        }

        public void GameStarted(FantomGameState gameState)
        {
            Console.WriteLine("New Game Begun: ");
            Reload(gameState);
        }

        public void Reload(FantomGameState? state)
        {
            if (state == null)
                return;
            /* Not used in the GUI 
            // Position playing Seekers
            for (int index = 0; index < state?.SeekersCount; ++index)
            {
                Console.WriteLine($"Seeker {index} is at: {state?.GetSeekerPosition(index)}");
            }            

            // Position Fantom if known / last known
            if (state != null && state.Value.IsRevealing)
                Console.WriteLine($"Fantom is at: {state?.FantomPosition}");
            else if (state?.FantomLastKnownPosition is not null)
            {
                Console.WriteLine($"Fantom was at: {state?.FantomLastKnownPosition} in Move: {state?.FantomLastRevealingMove}");
            }
            

            // Print out Tickets
            for (int index = 0; index < state?.DetectivesCount; ++index)
            {
                Console.WriteLine($"Detective {index} has " +
                    $"{state?.GetDetectiveTickets(index, TicketKinds.Mode1)} Mode 1, " +
                    $"{state?.GetDetectiveTickets(index, TicketKinds.Mode2)} Mode 2 and " +
                    $"{state?.GetDetectiveTickets(index, TicketKinds.Mode3)} Mode 3 Tickets.");
            }

            Console.WriteLine($"Fantom has " +
                    $"{state?.GetFantomTickets(TicketKinds.Mode1)} Mode 1, " +
                    $"{state?.GetFantomTickets(TicketKinds.Mode2)} Mode 2, " +
                    $"{state?.GetFantomTickets(TicketKinds.Mode3)} Mode 3, " +
                    $"{state?.GetFantomTickets(TicketKinds.River)} River, " +
                    $"{state?.GetFantomTickets(TicketKinds.Black)} Black and " +
                    $"{state?.GetFantomTickets(TicketKinds.Double)} Double Tickets.");            
            */
        }

        public void RoundOver(uint round)
        {
            Console.WriteLine("Seekers have played.");
        }

        public void SeekerCouldNotBeMoved(int seekerIndex)
        {
            Console.WriteLine("Seeker stayed.");
        }

        public void SeekerMovedTo(int seekerIndex, int tile, TicketKinds via)
        {
            Console.WriteLine($"Seeker {seekerIndex} Moved to {tile} using {via} : {Enum.GetName(via)}.");
        }

        public void SeekerPlacedAt(int seekerIndex, int tile)
        {
            Console.WriteLine($"Seeker {seekerIndex} Placed at {tile}.");
        }

        public void SeekersCouldNotBeMoved()
        {
            Console.WriteLine("Seekers stayed.");
        }

        public void SeekerTurnBegin(int seekerIndex)
        {
            Console.WriteLine($"Seeker {seekerIndex} is playing.");
        }

        public void SeekerTurnEnd(int seekerIndex)
        {
            Console.WriteLine($"Seeker {seekerIndex} has played.");
        }

        public void ShowInfo(Info status)
        {
            if (status.IsFantomTurn)
                Console.WriteLine("Fantom is playing.");
            else if (status.IsSeekersTurn)
                Console.WriteLine($"Seeker {status.SeekerIndex} is playing.");
            else
                Console.Error.WriteLine("Invalid state, neither Fantom nor Seekers are playing.");
        }



    }
}
