#define DEBUG
#define PROFILER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Encodes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestServer
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
            await _client.Connect("127.0.0.1", 8686);
        }

        [Test]
        public async Task TestClientConnectError()
        {
            var insideOnError = false;
            var client = new Client<TcpClient>();
            client.OnError += err =>
            {
                insideOnError = true;
            };
            var result = await client.Connect("localhost", 9999);
            Assert.AreEqual(false, result);
            Assert.AreEqual(true, insideOnError);
        }
        
        [Test]
        public async Task TestRequest()
        {
            var server = new Server<TcpServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5558);

            var client = new Client<TcpClient>();
            client.Connect("127.0.0.1", 5558).Wait();

            var (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello", result.Value);
            
            (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello1"
            });
            Assert.AreEqual(status.Code, StatusCode.Error);
            Assert.AreEqual(status.Message, "SYSTEM_ERR");
            Assert.AreEqual(null, result);
            
            (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello2"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello2", result.Value);
            
            server.Stop();
        }
        
        [Test]
        public async Task TestMultiClientRequest()
        {
            var clientCount = 10;
            var requestCount = 100;

            var encoder = ProtobufEncoder.Instance;
            var server = new Server<TcpServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5557);

            var tasks = new List<Task>();
            for (int i = 0; i < clientCount; i++)
            {
                var clientId = i;
                var t = Task.Run(async () => { 
                    var client = new Client<TcpClient>();
                    client.Connect("127.0.0.1", 5557).Wait();

                    for (var j = 0; j < requestCount; j++)
                    {
                        var id = clientId * j;
                        var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                        {
                            Value = $"Hello_{id}"
                        });
                
                        Assert.AreEqual(status.Code, StatusCode.Success);
                        Assert.AreEqual(result.Value, $"[Test] Server reply: Hello_{id}");
                    }
                });
                
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
        }
        
        [Test]
        public async Task TestAddListenerOnce()
        {
            var server = new Server<TcpServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5556);

            var client = new Client<TcpClient>();
            await client.Connect("127.0.0.1", 5556);

            var once = 0;
            var twice = 0;
            
            client.AddListenerOnce<PbString>("test.push", val =>
            {
                once++;
                Console.WriteLine($"ONCE: {val.Value}");
            });
            
            client.AddListener<PbString>("test.push", val =>
            {
                twice++;
                Console.WriteLine($"ALL: {val.Value}");
            });
            
            client.Notify("test.notify", new PbString
            {
                Value = "hello"
            });

            await Task.Delay(TimeSpan.FromSeconds(1));
            
            Assert.AreEqual(1, once);
            Assert.AreEqual(2, twice);
        }
        
        [Test]
        public void TestProfilerStatus()
        {
            var status = new ProfileStatus
            {
                Key = "Test",
                RunCount = 100000000,
                TotalTime = TimeSpan.FromSeconds(22.123)
            };
            Console.WriteLine(status.ToString());
        }

        [Test]
        public void TestProcessorBase()
        {
            var test = new TestProcessor();
            // Console.WriteLine(test.GetRouteDict().Dump());
        }
    }
}