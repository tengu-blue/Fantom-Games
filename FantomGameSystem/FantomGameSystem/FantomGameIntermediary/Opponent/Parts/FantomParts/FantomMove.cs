using FantomGamesCore;
using FantomGamesIntermediary.Opponent.Interfaces;

namespace FantomGamesIntermediary.Opponent.Parts.FantomParts
{
    internal unsafe struct FantomMove : IActorMove<FantomMove>
    {
        // stored as fixed integers to be Stack friendly.
        // Fantom might use the double Ticket which would result in two destinations
        fixed int _destinations[2];
        // essentially Tickets as integers instead of enums
        fixed int _modes[2];
        // if he was revealed at any of the possible two moves
        fixed bool _revealed[2];
        // know if actually Moved
        fixed bool _moved[2];

        public int MovesCount { get; private set; } = 0;
        public bool IsDouble { get; private set; } = false;


        public FantomMove(int firstDestination, bool moved, TicketKinds firstTicket, bool willRevealFirst)
        {
            _destinations[0] = firstDestination;
            _destinations[1] = -1;

            _modes[0] = (int) firstTicket;
            _modes[1] = 0;

            _revealed[0] = willRevealFirst;
            _revealed[1] = false;

            _moved[0] = moved;
            _moved[1] = false;

            IsDouble = false;
            MovesCount = 1;
        }

        public FantomMove(
            int firstDestination, bool firstMoved, TicketKinds firstTicket, bool willRevealFirst,
            int secondDestination, bool secondMoved, TicketKinds secondTicket, bool willRevealSecond)
        {
            _destinations[0] = firstDestination;
            _destinations[1] = secondDestination;

            _modes[0] = (int)firstTicket;
            _modes[1] = (int)secondTicket;

            _revealed[0] = willRevealFirst;
            _revealed[1] = willRevealSecond;

            // NOTE: probably always true, but maybe ran out on second one 
            _moved[0] = firstMoved;
            _moved[1] = secondMoved;

            IsDouble = true;
            MovesCount = 2;
        }

        public FantomMove(int fantomPosition)
        {
            _destinations[0] = fantomPosition;
            _destinations[1] = -1;

            // avoid issues with invalid Ticket kinds
            _modes[0] = 0;
            _modes[1] = 0;
        }

        public void SetDouble()
        {
            IsDouble = true;
        }

        // NOTE: move destination might not be known 
        public void SetMove(int moveIndex, int mode)
        {
            _destinations[moveIndex] = -1;
            _modes[moveIndex] = mode;
            _moved[moveIndex] = true;

            MovesCount++;
        }

        public void SetMove(int moveIndex, int destination, int mode)
        {
            _destinations[moveIndex] = destination;
            _modes[moveIndex] = mode;
            _moved[moveIndex] = true;
            _revealed[moveIndex] = true;

            MovesCount++;
        }

        // No index checks for speed, this is internal, so up to me..
        public bool Moved(int moveIndex)
        {
            return _moved[moveIndex];
        }

        public int? GetDestination(int moveIndex)
        {
            if (_destinations[moveIndex] < 0)
                return null;
            else
                return _destinations[moveIndex];
        }

        public TicketKinds GetTicketUsed(int moveIndex)
        {
            return (TicketKinds)_modes[moveIndex];
        }

        public int GetTicketAsInt(int moveIndex)
        {
            return _modes[moveIndex];
        }

        public bool WasRevealing(int moveIndex)
        {
            return _revealed[moveIndex];
        }


        public override string ToString()
        {
            if (!IsDouble)
                return $"0: {_destinations[0]} via {Enum.GetName((TicketKinds)_modes[0])}";
            else
                return $"0: {_destinations[0]} via {Enum.GetName((TicketKinds)_modes[0])}, 1: {_destinations[1]} via {Enum.GetName((TicketKinds)_modes[1])}";
        }


        public static bool operator ==(FantomMove a, FantomMove b)
        {
            return 
                a._destinations[0] == b._destinations[0] &&
                a._destinations[1] == b._destinations[1] &&
                a._modes[0] == b._modes[0] &&
                a._modes[1] == b._modes[1] &&
                a._revealed[0] == b._revealed[0] &&
                a._revealed[1] == b._revealed[1] &&
                a._moved[0] == b._moved[0] &&
                a._moved[1] == b._moved[1];
        }

        public static bool operator !=(FantomMove a, FantomMove b)
        {                        
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            else if (obj is FantomMove other)
                return this == other;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (_destinations[0], _destinations[1], _modes[0], _modes[1], _revealed[0], _revealed[1]).GetHashCode();
        }
    }
}
