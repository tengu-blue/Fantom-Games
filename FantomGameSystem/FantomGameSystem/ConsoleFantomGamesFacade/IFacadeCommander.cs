using FantomGamesCore;

namespace ConsoleFantomGamesFacade
{
    public interface IFacadeCommander
    {
        void ShowInfo(Info status);

        void Reload(FantomGameState? currentState);
    }

    public readonly struct Info
    {
        public readonly bool IsFantomTurn { get; init; }
        public readonly bool IsSeekersTurn { get; init; }
        public readonly int SeekerIndex { get; init; }

    }
}
