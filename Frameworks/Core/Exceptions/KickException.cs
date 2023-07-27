using System;

namespace GoPlay.Exceptions
{
    public class KickException : Exception
    {
        public KickException(string reason) : base(reason)
        {
        }
    }
}