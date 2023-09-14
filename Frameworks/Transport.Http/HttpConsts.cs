using System;

namespace GoPlay.Core.Transport.Http
{
    public static class HttpConsts
    {
        public const string POST_URL = "/api";
        public static readonly TimeSpan REQUEST_TIME_OUT = TimeSpan.FromMilliseconds(5000);
    }
}