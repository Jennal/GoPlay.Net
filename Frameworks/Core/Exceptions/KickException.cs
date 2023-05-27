using System;

namespace GoPlay.Services.Exceptions
{
    public class KickException : Exception
    {
        public KickException(string reason) : base(reason)
        {
        }
    }
}