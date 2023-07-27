using System;
using GoPlay.Core.Protocols;

namespace GoPlay.Exceptions
{
    public class ProcessorMethodException : Exception
    {
        public StatusCode Code;
        public string Msg;

        public ProcessorMethodException(StatusCode code, string msg)
        {
            Code = code;
            Msg = msg;
        }
    }
}