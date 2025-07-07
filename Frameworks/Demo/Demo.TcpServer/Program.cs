using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using GoPlay;
using Demo.Common;
using Demo.WsServer.Processors;
using GoPlay.Core.Transport.NetCoreServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo.WsServer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();
        {
            rootCommand.AddCommand(CreateStart());
            rootCommand.AddCommand(CreateInfo());
        }
        await rootCommand.InvokeAsync(args);
    }

    private static Command CreateStart()
    {
        var cmd = new Command("start", "启动服务器");
        {
            cmd.AddOption(new Option<string>(new []{"-h", "--host"}, () => "*", "IP"));
            cmd.AddOption(new Option<int>(new []{"-p", "--port"}, () => 8888, "端口"));
            cmd.AddOption(new Option<string>(new []{"-c", "--config"}, () => "app.json", "配置文件"));
            cmd.AddOption(new Option<bool>(new []{"-r", "--remove-cache"}, () => false, "停止服务时，是否清除缓存"));
            
            //参数名要和Option名字对应，例如：port 对应 --port
            cmd.Handler = CommandHandler.Create(
                (string host, int port) =>
                {
                    var hostBuilder = new HostBuilder()
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.AddHostedService(_ => new GoPlayService(host, port));
                        })
                        .UseConsoleLifetime()
                        .Build();

                    hostBuilder.RunAsync().Wait();
                });
        }
        return cmd;
    }

    private static Command CreateInfo()
    {
        var cmd = new Command("info", "显示服务器信息");
        {
            cmd.Handler = CommandHandler.Create(() =>
                {
                    var arr = Environment.CommandLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    var dir = Path.GetDirectoryName(arr[0]);
                    Console.WriteLine($"Environment.CommandLine: {Environment.CommandLine}");
                    Console.WriteLine($"Dir: {dir}");
                    Console.WriteLine($"Working Dir: {Directory.GetCurrentDirectory()}");
                });
        }
        return cmd;   
    }
}

internal class GoPlayService : IHostedService
{
    private Server<NcServer> _server;
    private Task _serverTask;
    
    private string _host;
    private int _port;

    public GoPlayService(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public Task StartAsync(CancellationToken _)
    {
        _server = new Server<NcServer>();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff)}] Server<{_server.TransportType.Name}> starting: {_host}:{_port}");

        _server.RegisterFilter(new LoggerFilter());
                    
        _server.Register(new TestProcessor());
        _server.Register(new ChatProcessor());
        _server.Register(new AriPlaneProcessor());
        
        _serverTask = _server.Start(_host, _port);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Server stopping...");

        _server.Stop();
        await _serverTask;

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Server stopped");
    }
}