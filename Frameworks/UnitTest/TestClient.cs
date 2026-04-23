using System;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestClient
    {
        private Server<TcpServer> _server;
        private Client<TcpClient> _client;
        private int _port;
        
        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();
            _port = TestPort.GetFree();
            
            _server = new Server<TcpServer>();
            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", _port);
            
            _client = new Client<TcpClient>();
            _client.OnConnected += OnClientConnected;
            await _client.Connect("127.0.0.1", _port);
        }

        [TearDown]
        public async Task TearDown()
        {
            try { if (_client != null) await _client.DisconnectAsync(); } catch { /* ignore */ }
            try { _server?.Stop(); } catch { /* ignore */ }
        }

        private void OnClientConnected()
        {
            Console.WriteLine("Client Connected!");
        }

        [Test]
        public void TestDisconnect()
        {
            _client.Disconnect();
            Assert.Pass();
        }
    }
}
