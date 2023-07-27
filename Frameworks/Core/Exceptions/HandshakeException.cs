using System;

namespace GoPlay.Exceptions
{
    public class HandshakeException : Exception
    {
        public HandshakeException(string msg) : base(msg)
        {
        }
    }
}