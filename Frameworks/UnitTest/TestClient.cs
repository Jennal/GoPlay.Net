using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Transport.NetCoreServer;

namespace UnitTest
{
    public class TestClient
    {
        private Server<NcServer> _server = null;
        private Client<NcClient> _client = null;
        
        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();

            if (_server != null) return;
            
            _server = new Server<NcServer>();
            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", 8686);
            
            _client = new Client<NcClient>();
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