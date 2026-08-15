using System;
using GoPlay;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports.TCP;
using GoPlay.Interfaces;
using NUnit.Framework;
using UnitTest.Helpers;

namespace UnitTest
{
    [TestFixture]
    public class TestFilterLifecycle
    {
        [Test]
        public void TestFilterStartAndStopLifecycle()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            var filter = new LifecycleFilter();

            try
            {
                server.RegisterFilter(filter);

                Assert.AreSame(server, filter.RegisteredFilterable);
                Assert.AreEqual(0, filter.StartCount);
                Assert.AreEqual(0, filter.StopCount);

                server.Start("127.0.0.1", port);

                Assert.AreEqual(1, filter.StartCount);
                Assert.AreEqual(0, filter.StopCount);

                server.Stop();

                Assert.AreEqual(1, filter.StartCount);
                Assert.AreEqual(1, filter.StopCount);
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public void TestFilterStopIsNotCalledWhenServerNeverStarted()
        {
            var server = new Server<TcpServer>();
            var filter = new LifecycleFilter();

            server.RegisterFilter(filter);
            server.Stop();

            Assert.AreEqual(0, filter.StartCount);
            Assert.AreEqual(0, filter.StopCount);
        }

        private sealed class LifecycleFilter : IFilter, IStart, IStop
        {
            public IFilterable? RegisteredFilterable { get; private set; }
            public int StartCount { get; private set; }
            public int StopCount { get; private set; }

            public void OnRegistered(IFilterable filterable)
            {
                RegisteredFilterable = filterable;
            }

            public void OnStart()
            {
                StartCount++;
            }

            public void OnStop()
            {
                StopCount++;
            }

            public void OnClientConnected(uint clientId)
            {
            }

            public void OnClientDisconnected(uint clientId)
            {
            }

            public bool OnPreSend(Package pack)
            {
                return false;
            }

            public void OnPostSend(Package pack)
            {
            }

            public bool OnPreRecv(Package pack)
            {
                return false;
            }

            public void OnPostRecv(Package pack)
            {
            }

            public void OnError(uint clientId, Exception err)
            {
            }
        }
    }
}
