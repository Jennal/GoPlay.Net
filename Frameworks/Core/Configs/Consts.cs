using System;

namespace GoPlay.Core
{
    public static class Consts
    {
        public static class Package
        {
            public const uint MAX_SIZE = ushort.MaxValue;
            public const uint MAX_CHUNK_SIZE = ushort.MaxValue - 2048;
        }

        public static class HeartBeat
        {
            public static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
            public static readonly TimeSpan Update = TimeSpan.FromSeconds(1);
            public static readonly TimeSpan Timeout = TimeSpan.MaxValue;//TimeSpan.FromSeconds(5);
        }
        
        public static class TimeOut
        {
            public static readonly TimeSpan Client = TimeSpan.FromMilliseconds(50);
            public static readonly TimeSpan Server = TimeSpan.FromMilliseconds(50);
            public static readonly TimeSpan Connect = TimeSpan.FromMilliseconds(2000);
            public static readonly TimeSpan Recv = TimeSpan.FromMilliseconds(50);
            public static readonly TimeSpan Send = TimeSpan.FromMilliseconds(500);
            public static readonly TimeSpan Update = TimeSpan.FromMilliseconds(100);
            public static readonly TimeSpan MinUpdate = TimeSpan.FromMilliseconds(1);
            public static readonly TimeSpan TimeoutUpdate = TimeSpan.FromMilliseconds(100);
            public static readonly TimeSpan KickDelayDisconnect = TimeSpan.FromSeconds(1);
        }

        public static class Buffer
        {
            public const int ReadSize = 4096;
            public const int WriteSize = 4096;
        }

        public static class Server
        {
            public const int MaxSendTask = 100;
        }
    }
}