using System;
using System.Threading.Tasks;
using GoPlay;
using GoPlay.Common;
using GoPlay.Core;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;
using NUnit.Framework;
using GoPlayProj.Extension.Backend;
using GoPlayProj.Processors;

namespace UnitTests;

public class TestEcho
{
    private Server<NcServer> _server = null;
    private Client<NcClient> _client = null;
    
    [SetUp]
    public async Task Setup()
    {
        if (_server != null) return;
        
        RunArgs.AppConfigFile = "app.local.json";
        _server = new Server<NcServer>();

        _server.OnError += OnServerError;
        _server.OnClientConnected += OnClientConnected;
        _server.OnClientDisconnected += OnClientDisconnected;
        
        _server.Register(new EchoProcessor());

        _server.Start("*", 8888);

        _client = new Client<NcClient>();
        _client.OnConnected += OnConnected;
        _client.OnError += OnClientError;
        await _client.Connect("localhost", 8888);
    }

    private void OnClientDisconnected(uint obj)
    {
        Console.WriteLine($"<= Client Disconnected: {obj}");
    }

    private void OnClientConnected(uint obj)
    {
        Console.WriteLine($"=> Client Connected: {obj}");
    }

    private void OnClientError(Exception err)
    {
        Console.WriteLine($"Client Error: {err}");
    }

    private void OnConnected()
    {
        Console.WriteLine($"Client Connected!");
    }

    private void OnServerError(uint clientId, Exception err)
    {
        Console.WriteLine($"Server Error: {clientId} -> {err}");
    }

    [TearDown]
    public void TearDown()
    {
        Console.WriteLine("TearDown");
        _client.Disconnect();
        Console.WriteLine("_client.Disconnect");
        _server.Stop();
        Console.WriteLine("_server.Stop");

        _client = null;
        _server = null;
    }
    
    [Test]
    public async Task TestRequest()
    {
        var (status, resp) = await _client.Echo_Request(new PbString
        {
            Value = "Hello"
        });
        
        Assert.AreEqual(StatusCode.Success, status.Code);
        Assert.AreEqual("Serv reply: Hello", resp.Value);
    }
    
    [Test]
    public async Task TestNotify()
    {
        _client.AddListener("echo.push", (PbString items) =>
        {
            Console.WriteLine(items);
        });
        
        _client.Echo_Notify(new PbString
        {
            Value = "Notify"
        });
        var resp = await _client.WaitFor<PbString>(ProtocolConsts.Push_EchoPush);
        Assert.AreEqual("Serv push: Notify", resp.Value);
    }

    [Test]
    public async Task TestTimeOut()
    {
        var (status, resp) = await _client.Echo_Timeout(new PbString
        {
            Value = "Timeout"
        });
        
        Assert.AreEqual(StatusCode.Timeout, status.Code);
        Assert.AreEqual("REQUEST_TIMEOUT", status.Message);
        Assert.AreEqual(null, resp);
    }
    
    [Test]
    public async Task TestError()
    {
        var (status, resp) = await _client.Echo_Err(new PbString
        {
            Value = "Error"
        });
        
        Assert.AreEqual(StatusCode.Failed, status.Code);
        Assert.AreEqual("ERROR_CODE", status.Message);
        Assert.AreEqual(null, resp);
    }
}