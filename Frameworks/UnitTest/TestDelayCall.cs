using System;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestDelayCall
    {
        private Server<NcServer> _server;
        private Client<NcClient> _client;
        private int _port;
        
        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();
            _port = TestPort.GetFree();
            
            _server = new Server<NcServer>();
            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", _port);
            
            _client = new Client<NcClient>();
            _client.RequestTimeout = TimeSpan.MaxValue;
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

        [Test]
        public async Task TestDelayCalls()
        {
            var pushResp = string.Empty;
            _client.AddListener("test.push", (PbString data) =>
            {
                pushResp = data.Value;
                Console.WriteLine($"Recv Push: {data.Value}");
                Assert.AreEqual("Delay Push", data.Value);
            });

            var (status, resp) = await _client.Request<PbString, PbString>("test.echo.delay", new PbString
            {
                Value = "Test"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("[Test] Server reply: Test", resp.Value);
            Assert.AreEqual(string.Empty, pushResp);
            await Task.Delay(2000);
            Assert.AreEqual("Delay Push", pushResp);
        }
    }
}
