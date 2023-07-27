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
using GoPlay.Core.Transport.NetCoreServer;

namespace UnitTest
{
    public class TestNetCoreServer
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
            _client.RequestTimeout = TimeSpan.MaxValue;
            if (!await _client.Connect("127.0.0.1", 8686))
            {
                throw new Exception("connect failed!");
            }
        }

        [Test]
        public async Task TestClientConnectError()
        {
            var insideOnError = false;
            var client = new Client<NcClient>();
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
            var (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello", result.Value);
            
            (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello1"
            });
            Assert.AreEqual(status.Code, StatusCode.Error);
            Assert.AreEqual(status.Message, "SYSTEM_ERR");
            Assert.AreEqual(null, result);
            
            (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello2"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello2", result.Value);
        }
        
        [Test]
        public async Task BenchmarkRequest()
        {
            var count = 10000 * 10;//1000 * 10000;
            var timer = new System.Diagnostics.Stopwatch();
            
            var client = new Client<NcClient>();
            await client.Connect("127.0.0.1", 8686);
            client.RequestTimeout = TimeSpan.MaxValue;
            client.OnError += err => Console.WriteLine($"ERROR: {err.Message}\n{err.StackTrace}"); 
            
            timer.Start();
            for (var i = 0; i < count; i++)
            {
                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                {
                    Value = $"Hello_{i}"
                });
                
                // Console.WriteLine($"{i}, {status}, {result}");
                
                Assert.AreEqual("", status.Message);
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual($"[Test] Server reply: Hello_{i}", result.Value);
            }
            timer.Stop();

            var total = timer.ElapsedMilliseconds; 
            var avg = (float)total / count;
            Console.WriteLine($"Total millisec: {total}");
            Console.WriteLine($"Average millisec: {avg}");
            Console.WriteLine(Profiler.Statistics());
            
            await client.DisconnectAsync();
        }
        
        [Test]
        public async Task BenchmarkMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 100;

            var encoder = ProtobufEncoder.Instance;
            var server = new Server<NcServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5557);

            var tasks = new List<Task>();
            for (int i = 0; i < clientCount; i++)
            {
                var clientId = i;
                var profilerKey = $"Request_{clientId}";
                var t = Task.Run(async () => { 
                    var client = new Client<NcClient>();
                    client.RequestTimeout = TimeSpan.MaxValue;
                    client.Connect("127.0.0.1", 5557).Wait();

                    for (var j = 0; j < requestCount; j++)
                    {
                        var id = clientId * j;
                        Profiler.Begin(profilerKey);
                        var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                        {
                            Value = $"Hello_{id}"
                        });
                        Profiler.End(profilerKey);
                
                        Assert.AreEqual(status.Code, StatusCode.Success);
                        Assert.AreEqual(result.Value, $"[Test] Server reply: Hello_{id}");
                    }
                });
                
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine(Profiler.StatisPrefix("Request"));
        }
        
        [Test]
        public async Task TestAddListenerOnce()
        {
            var server = new Server<NcServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5556);

            var client = new Client<NcClient>();
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
    }
}