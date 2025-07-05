namespace FantomGamesCore.Managers
{
    internal class TicketsManager
    {
        // Instead of dictionaries use enum values as indices for better performance

        // The common supply from which the Fantom can steal
        private readonly int[] _commonSupply = new int[TicketKindsCount()];

        // Tickets kept for all actors
        private ActorTickets[] _actorTickets = [];

        private readonly Dictionary<string, TicketKinds> _namesToTickets = [];

        public static int TicketKindsCount()
        {
            return FantomGameSettings.TICKET_KINDS_COUNT;
        }

        public void Reset(int actors)
        {
            // set all Ticket Counts to 0

            for (int i = 0; i < _commonSupply.Length; i++)
                _commonSupply[i] = 0;

            _actorTickets = new ActorTickets[actors];
            for (int i = 0; i < _actorTickets.Length; ++i)
                _actorTickets[i] = new ActorTickets();
        }

        
        public TicketKinds KindFromAlias(string alias)
        {
            // TODO: solve properly NOTE: throw error when not known
            return _namesToTickets[alias];
        }

        public void SetSupplyTickets(IEnumerable<TicketGroup> ticketGroups)
        {
            foreach (var g in ticketGroups)
            {
                _commonSupply[(int)g.TicketKind] = g.Count;
            }
        }

        public void SetActorTickets(int actorIndex, IEnumerable<TicketGroup> ticketGroups)
        {
            // maybe an exception
            if (actorIndex < 0 || actorIndex >= _actorTickets.Length)
                return;

            foreach (var g in ticketGroups)
            {
                _actorTickets[actorIndex].Set(g.TicketKind, g.Count);
            }
        }

        public int GetSupplyTickets(TicketKinds ticketKind)
        {
            return _commonSupply[(int)ticketKind];
        }

        public void RemoveSupplyTickets(TicketKinds ticketKind, int howMany)
        {
            _commonSupply[(int)ticketKind] -= howMany;
        }
        public void SetSupplyTickets(TicketKinds ticketKind, int count)
        {
            _commonSupply[(int)ticketKind] = count;
        }

        public int GetSupplyTickets(string ticketKind)
        {
            return GetSupplyTickets(KindFromAlias(ticketKind));
        }

        public void RemoveSupplyTickets(string ticketKind, int howMany)
        {
            RemoveSupplyTickets(KindFromAlias(ticketKind), howMany);
        }
        public void SetSupplyTickets(string ticketKind, int count)
        {
            SetSupplyTickets(KindFromAlias(ticketKind), count);
        }


        public int GetActorTickets(int actorIndex, TicketKinds ticketKind)
        {
           
            if (actorIndex < 0 || actorIndex >= _actorTickets.Length) return -1;

            return _actorTickets[actorIndex].GetCount(ticketKind);
        }

        public void RemoveActorTickets(int actorIndex, TicketKinds ticketKind, int howMany)
        {
            // Checked at the level above
            if (actorIndex < 0 || actorIndex >= _actorTickets.Length) return;

            _actorTickets[actorIndex].Change(ticketKind, -howMany);
        }

        public void AddActorTickets(int actorIndex, TicketKinds ticketKind, int howMany)
        {
            // maybe an exception
            if (actorIndex < 0 || actorIndex >= _actorTickets.Length)
                return;

            _actorTickets[actorIndex].Change(ticketKind, howMany);
        }

        public void SetActorTickets(int actorIndex, TicketKinds ticketKind, int count)
        {
            // maybe an exception
            if (actorIndex < 0 || actorIndex >= _actorTickets.Length)
                return;

            _actorTickets[actorIndex].Set(ticketKind, count);
        }

        public int GetActorTickets(int actorIndex, string ticketKind)
        {
            return GetActorTickets(actorIndex, KindFromAlias(ticketKind));
        }

        public void RemoveActorTickets(int actorIndex, string ticketKind, int howMany)
        {
            RemoveActorTickets(actorIndex, KindFromAlias(ticketKind), howMany);
        }
        public void SetActorTickets(int actorIndex, string ticketKind, int count)
        {
            SetActorTickets(actorIndex, KindFromAlias(ticketKind), count);
        }


        /// <summary>
        /// A helper struct for holding the number of Tickets of all kinds for each Actor.
        /// </summary>
        private struct ActorTickets
        {
            private int[] ticketCounts;
            public ActorTickets()
            {
                ticketCounts = new int[TicketKindsCount()];
            }

            public int GetCount(TicketKinds kind)
            {
                if ((int)kind >= 0 && (int)kind < ticketCounts.Length)
                    return ticketCounts[(int)kind];
                else
                    return -1;
            }

            public void Change(TicketKinds kind, int by)
            {
                if ((int)kind >= 0 && (int)kind < ticketCounts.Length)
                    ticketCounts[(int)kind] += by;
            }


            public void Set(TicketKinds kind, int count)
            {
                if ((int)kind >= 0 && (int)kind < ticketCounts.Length)
                    ticketCounts[(int)kind] = count;
            }

        }

    }


}
