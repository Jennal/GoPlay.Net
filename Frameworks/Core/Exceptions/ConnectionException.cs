using System;

namespace GoPlay.Exceptions
{
    public class ConnectionException : Exception
    {
        public ConnectionException(string host, int port, Exception err) : base($"Connect \"{host}:{port}\" failed", err)
        {
        }
    }
}