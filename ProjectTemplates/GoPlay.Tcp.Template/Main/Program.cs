using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using GoPlay;
using GoPlay.Common;
using GoPlay.Core.Transport.Ws;
using GoPlay.Core.Transport.Wss;
using GoPlayProj.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoPlayProj.Main;

public static partial class Program
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
                (string host, int port, string config, bool removeCache) =>
                {
                    RunArgs.AppConfigFile = config;
                    RunArgs.ClearCache = removeCache;
                    
                    var hostBuilder = new HostBuilder()
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.AddHostedService(_ => new GoPlayProjService(host, port));
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

internal class GoPlayProjService : IHostedService
{
    private Server<WsServer> _server;
    private Task _serverTask;
    
    private string _host;
    private int _port;

    public GoPlayProjService(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public Server<WsServer> InitServer()
    {
        _server = new Server<WsServer>();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff)}] Server<{_server.TransportType.Name}> starting: {_host}:{_port}");

        _server.RegisterFilter(new LoggerFilter());
        _server.RegisterFilter(new IdleClearFilter());
                    
        _server.Register(new DbSaverProcessor());
        _server.Register(new AdminProcessor());
        
        // _server.AddStaticContent("./dist");
        return _server;
    }
    
    public Task StartAsync(CancellationToken _)
    {
        _server = InitServer();
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