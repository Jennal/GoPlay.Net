using System;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NUnit.Framework;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestDeferCall
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

        [Test]
        public async Task TestDeferCalls()
        {
            var pushResp = string.Empty;
            _client.AddListener("test.push", (PbString data) =>
            {
                pushResp = data.Value;
                Console.WriteLine($"Recv Push: {data.Value}");
                Assert.AreEqual("Delay Push", data.Value);
            });

            var (status, resp) = await _client.Request<PbString, PbString>("test.echo.defer", new PbString
            {
                Value = "Test"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("[Test] Server reply: Test", resp.Value);
            Assert.AreEqual(string.Empty, pushResp);
            await Task.Delay(1500);
            Assert.AreEqual("Delay Push", pushResp);
        }
    }
}