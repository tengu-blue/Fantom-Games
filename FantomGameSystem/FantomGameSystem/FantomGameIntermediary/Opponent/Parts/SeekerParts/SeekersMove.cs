using FantomGamesCore;
using FantomGamesCore.Interfaces;
using FantomGamesIntermediary.Opponent.Interfaces;


namespace FantomGamesIntermediary.Opponent.Parts.SeekerParts
{
    /// <summary>
    /// A Helper struct that holds the locations and Tickets used, for a single Round's Seekers' Move.
    /// </summary>
    internal unsafe struct SeekersMove : IActorMove<SeekersMove>
    {
        // stored as fixed integers to be Stack friendly.
        fixed int _destinations[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];
        // essentially TravelModes as integers instead of enums
        fixed int _modes[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];
        // if actually made a Move - didn't use the No Move 
        fixed bool _moved[FantomGameSettings.MAXIMUM_SEEKERS_COUNT];

        public SeekersMove(int[] seekerDestinations, TicketKinds[] travelModes, bool[] moved)
        {
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                if (i < seekerDestinations.Length)
                {
                    _destinations[i] = seekerDestinations[i];
                    _modes[i] = (int)travelModes[i];
                    _moved[i] = moved[i];
                }

                // for Seekers who aren't playing
                else
                {
                    _destinations[i] = -1;
                    _modes[i] = -1;
                    _moved[i] = false;
                }

            }
        }

        // TODO: issue when seeker doesn't move, returning -1; have to have a NoMove, default, so constructor takes
        // current starting positions / previous ones

        public SeekersMove(int[] seekerPositions)
        {
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                if (i < seekerPositions.Length)
                {
                    // set mode to 0, to avoid problems, but by default, assume No Move happened
                    _destinations[i] = seekerPositions[i];
                    _modes[i] = 0;
                    _moved[i] = false;
                } else
                {
                    _destinations[i] = -1;
                    _modes[i] = -1;
                    _moved[i] = false;
                }
            }
        }

        public void SetMove(int seekerIndex, int destination, int mode)
        {
            _destinations[seekerIndex] = destination;
            _modes[seekerIndex] = mode;
            _moved[seekerIndex] = true;
        }


        // No index checks for speed, this is internal, so up to me..
        public bool Moved(int seekerIndex)
        {
            return _moved[seekerIndex];
        }

        public int GetDestination(int seekerIndex)
        {
            return _destinations[seekerIndex];
        }

        public TicketKinds GetTicketUsed(int seekerIndex)
        {
            return (TicketKinds)_modes[seekerIndex];
        }

        public int GetTicketAsInt(int seekerIndex)
        {
            return _modes[seekerIndex];
        }



        public static bool operator ==(SeekersMove left, SeekersMove right)
        {
            for (int i = 0; i < FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                if (left._destinations[i] != right._destinations[i] ||
                    left._modes[i] != right._modes[i] ||
                    left._moved[i] != right._moved[i])
                    return false;
            }
            return true;
        }

        public static bool operator !=(SeekersMove left, SeekersMove right)
        {
            return !(left == right);
        }

        
        public override string ToString()
        {
            return $"[{0}: {GetDestination(0)} via {Enum.GetName(GetTicketUsed(0))}][{1}: {GetDestination(1)} via {Enum.GetName(GetTicketUsed(1))}][{2}: {GetDestination(2)} via {Enum.GetName(GetTicketUsed(2))}][{3}: {GetDestination(3)} via {Enum.GetName(GetTicketUsed(3))}][{4}: {GetDestination(4)} via {Enum.GetName(GetTicketUsed(4))}]";
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            else if (obj is SeekersMove other)
                return this == other;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (_destinations[0], _destinations[1], _destinations[2], _destinations[3], _destinations[4],
                    _modes[0], _modes[1], _modes[2], _modes[3], _modes[4],
                    _moved[0], _moved[1], _moved[2], _moved[3], _moved[4]).GetHashCode();
        }
    }
}
