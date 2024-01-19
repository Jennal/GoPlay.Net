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

public class TestTime
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
        
        _server.Register(new TimeProcessor());

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
    public async Task TestGetTime()
    {
        var (status, resp) = await _client.Time_GetTime();
        
        Assert.AreEqual(StatusCode.Success, status.Code);
        Assert.IsTrue((DateTime.UtcNow - resp.Value.ToDateTime()) < TimeSpan.FromSeconds(1));
    }
}