using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace UnitTest.Helpers
{
    /// <summary>
    /// 给单元测试分配空闲端口。
    /// 用法：var port = TestPort.GetFree();
    ///
    /// 实现：让 OS 临时绑定 0 端口拿到一个空闲值再立刻释放，
    /// 之后由真正的服务器去抢占。窗口期极短，单机跑测试足够稳。
    /// </summary>
    public static class TestPort
    {
        private static readonly object s_lock = new object();
        private static readonly HashSet<int> s_used = new HashSet<int>();

        public static int GetFree()
        {
            lock (s_lock)
            {
                for (var attempt = 0; attempt < 32; attempt++)
                {
                    var listener = new TcpListener(IPAddress.Loopback, 0);
                    try
                    {
                        listener.Start();
                        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                        if (s_used.Add(port)) return port;
                    }
                    finally
                    {
                        listener.Stop();
                    }
                }

                throw new Exception("TestPort.GetFree: failed to find free port after 32 attempts");
            }
        }
    }
}
