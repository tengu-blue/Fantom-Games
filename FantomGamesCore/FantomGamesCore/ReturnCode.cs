namespace FantomGamesCore
{

    public enum ReturnCodes
    {
        /// <summary>
        /// Successful action with a valid result.
        /// </summary>
        OK,

        /// <summary>
        /// The state machine is not in a valid state for the issued command.
        /// </summary>
        InvalidState,
        /// <summary>
        /// Index was not valid for the issued command.
        /// </summary>
        BadIndex,
        /// <summary>
        /// The given position is currently occupied and cannot be used by the issued command.
        /// </summary>
        PositionOccupied,
        /// <summary>
        /// The issued command is not currently possible.
        /// </summary>
        InvalidOperation,
        /// <summary>
        /// The passed argument for the issued command is not valid.
        /// </summary>
        BadArgument,
        /// <summary>
        /// Does not have enough tickets for the issued command.
        /// </summary>
        NotEnoughTickets,
        /// <summary>
        /// The specified board tiles are not connected.
        /// </summary>
        BoardTilesNotConnected,

        /// <summary>
        /// Passed settings are not valid.
        /// </summary>
        InvalidSettings,

        /// <summary>
        /// The issued command failed in some way.
        /// </summary>
        Fail
    }

    public struct ReturnCode<T>
    {
        public T Value;

        public ReturnCodes Code;
        public string? Message;

        public ReturnCode(T value)
        {
            Value = value;
            Code = ReturnCodes.OK;
            Message = null;
        }

        public ReturnCode(T value, ReturnCodes code)
        {
            Value = value;
            Code = code;
            Message = null;
        }

        public ReturnCode(T value, ReturnCodes code, string? message)
        {
            Value = value;
            Code = code;
            Message = message;
        }

        public void UpdateMessage(string? message)
        {
            Message = message;
        }

        public static implicit operator T(ReturnCode<T> returnCode)
        {
            return returnCode.Value;
        }

    }
}
