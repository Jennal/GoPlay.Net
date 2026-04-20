using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestListeners
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
            await _client.Connect("127.0.0.1", _port);
        }

        [TearDown]
        public async Task TearDown()
        {
            try { if (_client != null) await _client.DisconnectAsync(); } catch { /* ignore */ }
            try { _server?.Stop(); } catch { /* ignore */ }
        }

        [Test]
        public async Task TestRequestCallback()
        {
            var cbStr = new PbString();
            _client.AddListener<PbString>("test.echo", str =>
            {
                cbStr = str;
                Console.WriteLine(str.Value);
            });
            var (status, str) = await _client.Request<PbString, PbString>("test.echo", new PbString
            {
                Value = "Hello"
            });
            
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual(cbStr, str);
        }
        
        [Test]
        public async Task TestPushCallback()
        {
            var task = new TaskCompletionSource();
            var cbStr = new PbString();
            _client.AddListener<PbString>("test.push", str =>
            {
                cbStr = str;
                Console.WriteLine(str.Value);
                
                task.TrySetResult();
            });
            _client.Notify("test.notify", new PbString
            {
                Value = "Hello"
            });

            await task.Task;
            Assert.AreEqual(cbStr.Value, "Push: Hello");
        }
        
        [Test]
        public async Task TestPushPackageCallback()
        {
            var task = new TaskCompletionSource();
            var cbStr = new PbString();
            _client.AddListener<Package>("test.push", pack =>
            {
                Console.WriteLine(pack.ToString());
                var p = Package.ParseFromRaw<PbString>(pack);
                cbStr = p.Data;

                task.TrySetResult();
            });
            _client.Notify("test.notify", new PbString
            {
                Value = "Hello"
            });

            await task.Task;
            Assert.AreEqual(cbStr.Value, "Push: Hello");
        }
        
        [Test]
        public async Task TestPushPackageGenericCallback()
        {
            var task = new TaskCompletionSource();
            var cbStr = new PbString();
            _client.AddListener<Package<PbString>>("test.push", pack =>
            {
                Console.WriteLine(pack.ToString());
                cbStr = pack.Data;

                task.TrySetResult();
            });
            _client.Notify("test.notify", new PbString
            {
                Value = "Hello"
            });

            await task.Task;
            Assert.AreEqual(cbStr.Value, "Push: Hello");
        }
        
        [Test]
        public async Task TestWaitFor()
        {
            var task = _client.WaitFor<Package<PbString>>("test.push");
            _client.Notify("test.notify", new PbString
            {
                Value = "Hello"
            });
            var pack = await task;
            Assert.AreEqual(pack.Data.Value, "Push: Hello");
        }
    }
}
