using System;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Exceptions
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