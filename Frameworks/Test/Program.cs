using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GoPlay;
using GoPlay.Core.Attributes;
using GoPlay.Core.Debug;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;
using GoPlay.Core.Processors;
using GoPlay.Core.Transports.TCP;

[Processor("test")]
class TestProcessor : ProcessorBase
{
    private ConcurrentDictionary<uint, bool> m_clients = new ConcurrentDictionary<uint, bool>();

    public override string[] Pushes => new string[]
    {
        "test.push"
    };

    public TestProcessor()
    {
        // Task.Run(async () =>
        // {
        //     while(Server == null || !Server.IsStarted) await Task.Delay(100);
        //
        //     while (Server.IsStarted)
        //     {
        //         foreach (var clientId in m_clients.Keys)
        //         {
        //             Send(clientId, "test.push", PackageType.Push, new PbString
        //             {
        //                 Value = "Hello"
        //             });
        //         }
        //         await Task.Delay(1000);
        //     }
        // });
    }
    
    public override void OnClientConnected(uint clientId)
    {
        m_clients.TryAdd(clientId, true);

        // var id = clientId;
        // Task.Run(() =>
        // {
        //     Thread.Sleep(3000);
        //     Server.Kick(id);
        // });
    }

    public override void OnClientDisconnected(uint clientId)
    {
        m_clients.TryRemove(clientId, out _);
    }

    [Request("echo")]
    public PbString Echo(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Echo Recv: {str.Value}");
        return new PbString
        {
            Value = $"Server reply: {str.Value}"
        };
    }
    
    [Request("notify")]
    public void Notify(Header header, PbString str)
    {
        Console.WriteLine($">>>> Server.Notify Recv: {str.Value}");
        str.Value = $"Push: {str.Value}";
        Push("test.push", header, str);
        Push("test.push", header, str);
    }
}

class DumpFilter : IFilter
{
    public void OnRegistered(IFilterable filterable)
    {
    }

    public void OnClientConnected(uint clientId)
    {
    }

    public void OnClientDisconnected(uint clientId)
    {
    }

    public bool OnPreSend(Package pack)
    {
        // if (pack.Header.PackageInfo.Type != PackageType.Ping &&
        //     pack.Header.PackageInfo.Type != PackageType.Pong)
    //     {
    //         Console.ForegroundColor = ConsoleColor.DarkCyan;
    //         Console.WriteLine($@"OnSend: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    // <= pack = {pack}
    // <= data = {pack.GetBytes().Dump()}");
    //         Console.ResetColor();
    //     }

        return false;
    }

    public void OnPostSend(Package pack)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($@"OnPostSend: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    <= pack = {pack}
    <= data = {pack.GetBytes().Dump()}");
        Console.ResetColor();
    }

    public bool OnPreRecv(Package pack)
    {
        // if (pack.Header.PackageInfo.Type != PackageType.Ping &&
        //     pack.Header.PackageInfo.Type != PackageType.Pong)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($@"OnRecv: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    <= pack = {pack}
    <= data = {pack.GetBytes().Dump()}");
            Console.ResetColor();
        }

        return false;
    }

    public void OnPostRecv(Package pack)
    {
    }

    public void OnError(uint clientId, Exception err)
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(args.Dump());
        switch (args[0])
        {
            case "-s":
                EchoServer();
                break;
            case "-c":
                EchoClient(args[1]);
                break;
        }
        
        // WHSDemo(args);
        
        AppUtil.WaitForKill().Wait();
        // NetMqDemo(args);
    }

    static void EchoServer()
    {
        var server = new Server<TcpServer>();
        server.OnClientConnected += cid => Console.WriteLine($" ++ Client Connected: {cid}");
        server.OnClientDisconnected += cid => Console.WriteLine($" -- Client Disconnected: {cid}");
        server.OnError += (cid, err) => Console.WriteLine($" -- Client Error[{cid}]: {err}");
        server.Register(new TestProcessor());
        server.RegisterFilter(new DumpFilter());
        server.Start("*", 5555);
    }

    static async void EchoClient(string host)
    {
        var client = new Client<TcpClient>();
        client.OnConnected += () => Console.WriteLine(" => Connected");
        client.OnDisconnected += () => Console.WriteLine(" => Disconnected");
        client.OnKicked += msg => Console.WriteLine($" => Kicked: {msg}");
        client.OnError += err => Console.WriteLine($" => Error: {err}");
        
        client.RegisterFilter(new DumpFilter());
        await client.Connect(host, 5555);

        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;

            var (s, r) = await client.Request<PbString, PbString>("test.echo", new PbString
            {
                Value = line
            });
            if (s.Code == StatusCode.Success)
            {
                Console.WriteLine($"Recv: \"{r.Value}\"");
            }
            else
            {
                Console.WriteLine($"Recv Error: {s.Code}: {s.Message}");
            }
        }
    }
    
    static async void WHSDemo(string[] args)
    {
        var server = new Server<TcpServer>();
        server.Register(new TestProcessor());
        var task = server.Start("127.0.0.1", 5555);

        var client = new Client<TcpClient>();
        await client.Connect("127.0.0.1", 5555);
        client.AddListener<PbString>("test.push", data =>
        {
            Console.WriteLine($" => {data.Value}");
        });

        client.Notify("test.notify", new PbString
        {
            Value = "notify hello"
        });
        var (s, r) = await client.Request<PbString, PbString>("test.echo", new PbString
        {
            Value = $"request hello"
        });
        Console.WriteLine($" => {r.Value}");

        Console.ReadKey();
        return;
        for (int i = 0; i < 100; i++)
        {
            Profiler.Begin("Request");
            var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
            {
                Value = $"Hello_{i}"
            });
            if (result.Value != $"Server reply: Hello_{i}") throw new Exception($"Error!\n <= Hello_{i}\n => {result.Value}");
            Profiler.End("Request");
        }

        Console.WriteLine(Profiler.Statistics());
    }
    
    // static void NetMqDemo(string[] args)
    // {
    //     Console.WriteLine(args.Dump());
    //     switch (args[0])
    //     {
    //         case "-c":
    //             Client(args[1]);
    //             break;
    //         case "-s":
    //         default:
    //             Server();
    //             break;
    //     }
    // }
    //
    // static void Client(string clientId)
    // {
    //     using (var clientSocket = new StreamSocket())
    //     {
    //         clientSocket.Connect("tcp://127.0.0.1:5555");
    //         for (var i = 0; i < 10; i++)
    //         {
    //             var id = clientSocket.Options.Identity!;
    //             clientSocket.SendMoreFrame(id).SendFrame($"{clientId}: {id.Dump()}-{i}");
    //             id = clientSocket.ReceiveFrameBytes();
    //             var answer = clientSocket.ReceiveFrameString();
    //             Console.WriteLine($"Answer from server[{id.Dump()}]: {answer}");
    //             Thread.Sleep(1000);
    //         }
    //         // clientSocket.SendFrame($"exit");
    //     }
    // }
    //
    // static void Server()
    // {
    //     using (NetMQSocket serverSocket = new StreamSocket())
    //     {
    //         serverSocket.Bind("tcp://127.0.0.1:5555");
    //         Console.WriteLine("Server Started at: tcp://127.0.0.1:5555");
    //         var count = 0;
    //         while (true)
    //         {
    //             var clientId = serverSocket.ReceiveFrameBytes();
    //             string message1 = serverSocket.ReceiveFrameString();
    //             
    //             string[] msg = message1.Split(':');
    //             string message = msg[1];
    //             
    //             Console.WriteLine($"=>[{clientId.Dump()}] {msg[0]} | {msg[1]}");
    //
    //             #region 根据接收到的消息，返回不同的信息
    //             serverSocket.SendMoreFrame(clientId).SendFrame(message);
    //             #endregion
    //
    //             count++;
    //         }
    //     }
    // }
}