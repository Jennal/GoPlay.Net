using System;

namespace GoPlay.Services.Exceptions
{
    public class ConnectionException : Exception
    {
        public ConnectionException(string host, int port, Exception err) : base($"Connect \"{host}:{port}\" failed", err)
        {
        }
    }
}