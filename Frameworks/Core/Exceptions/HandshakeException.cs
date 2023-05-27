using System;

namespace GoPlay.Services.Exceptions
{
    public class HandshakeException : Exception
    {
        public HandshakeException(string msg) : base(msg)
        {
        }
    }
}