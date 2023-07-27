using System;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NUnit.Framework;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestClient
    {
        private Server<TcpServer> _server = null;
        private Client<TcpClient> _client = null;
        
        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();

            if (_server != null) return;
            
            _server = new Server<TcpServer>();
            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", 8686);
            
            _client = new Client<TcpClient>();
            _client.OnConnected += OnClientConnected;
            await _client.Connect("127.0.0.1", 8686);
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