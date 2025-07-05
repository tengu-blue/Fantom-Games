using FantomGamesCore;
using FantomGamesSystemUtils;
using System.Globalization;

namespace ConsoleFantomGamesFacade
{
    public class CommandParser(FantomGamesIntermediary.FantomIntermediary commander, IFacadeCommander facade)
    {

        public bool Parse(string command)
        {
            if (command == "")
                return true;

            string[] tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            bool playingAsFantom = commander.GetIntermediarySettings().LivingPlayerIsFantom;
            bool playingAsSeekers = commander.GetIntermediarySettings().LivingPlayerIsSeekers;

            switch (tokens[0])
            {
                // NOTE: restart same as reset because no support for changing settings at runtime (at least yet)
                case "restart":
                case "reset":
                    commander.Reset();
                    return true;

                case "info":
                    facade.ShowInfo(new Info()
                    {
                        IsFantomTurn = commander.IsFantomTurn(),
                        IsSeekersTurn = commander.IsSeekersTurn(),
                        SeekerIndex = commander.SeekerIndex(),
                    });
                    return true;

                case "reload":
                    facade.Reload(
                        playingAsFantom ?
                        commander.GetPrivateGameState() :
                        commander.GetPublicGameState());

                    return true;

                // Context based 

                case "place":
                    
                    // First try as the Seekers
                    if (playingAsSeekers) {
                        if (Has(tokens, out int index, out int pos))
                        {
                            commander.PlaceSeekerAt(index, pos);
                            return true;
                        }
                    }

                    // Then try as the Fantom
                    if (playingAsFantom)
                    {
                        if (Has(tokens, out int pos))
                        {
                            commander.PlaceFantomAt(pos);
                            return true;
                        }
                    }

                    return false;

                // TODO: add with ticket names ideally
                case "move":

                    if (playingAsFantom && commander.IsFantomTurn())
                    {
                        if (Has(tokens, out int pos, out int via))
                        {
                            commander.MoveFantom(pos, (TicketKinds)via);
                            commander.ConfirmFantomTurnOver();
                            return true;
                        }
                    }
                    
                    if (playingAsSeekers && commander.IsSeekersTurn()) {
                        if (Has(tokens, out int index, out int pos, out int via))
                        {
                            commander.MoveSeeker(index, pos, (TicketKinds)via);
                            commander.ConfirmSeekersTurnOver();
                            return true;
                        }
                    }

                    return false;

                case "skip":
                    
                    if (commander.IsFantomTurn())
                    {
                        commander.CannotMoveFantom();
                        commander.ConfirmFantomTurnOver();
                    } else
                    {
                        commander.CannotMoveSeeker();
                        commander.ConfirmSeekersTurnOver();
                    }

                    return true;

                case "double":

                    if (playingAsFantom)
                    {
                        ((IFantomCommander)commander).UseDouble();
                        return true;
                    }

                    return false;
            }


            return false;
        }


        private static bool Has<T>(string[] tokens, out T output) where T : struct, IParsable<T>
        {

            if (tokens.Length < 2)
            {
                output = new();
                return false;
            }

            return T.TryParse(tokens[1], CultureInfo.InvariantCulture, out output);
        }

        private static bool Has<T1, T2>(string[] tokens, out T1 output1, out T2 output2)
            where T1 : struct, IParsable<T1>
            where T2 : struct, IParsable<T2>
        {
            output1 = new();
            output2 = new();

            if (tokens.Length < 3)
                return false;

            return
                T1.TryParse(tokens[1], CultureInfo.InvariantCulture, out output1) &&
                T2.TryParse(tokens[2], CultureInfo.InvariantCulture, out output2);
        }

        private static bool Has<T1, T2, T3>(string[] tokens, out T1 output1, out T2 output2, out T3 output3)
            where T1 : struct, IParsable<T1>
            where T2 : struct, IParsable<T2>
            where T3 : struct, IParsable<T3>
        {
            output1 = new();
            output2 = new();
            output3 = new();

            if (tokens.Length < 4)
                return false;

            return
                T1.TryParse(tokens[1], CultureInfo.InvariantCulture, out output1) &&
                T2.TryParse(tokens[2], CultureInfo.InvariantCulture, out output2) &&
                T3.TryParse(tokens[3], CultureInfo.InvariantCulture, out output3);
        }
    }
}
