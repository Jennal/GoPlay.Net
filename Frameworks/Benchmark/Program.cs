using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace GoPlay.Benchmarks
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // 用法：
            //   dotnet run -c Release -- report       # (默认) 快速 stopwatch 对比表，几十秒出结果
            //   dotnet run -c Release -- concurrency  # BenchmarkDotNet 跑单连接 Concurrency × Delay 矩阵
            //   dotnet run -c Release -- route        # 只跑 BenchmarkRoute（反射 vs 编译委托）
            //   dotnet run -c Release -- request      # 只跑 BenchmarkRequest（空业务 Echo 单 RTT）
            //   dotnet run -c Release -- multi        # 只跑 BenchmarkMultiClient（N clients × M 串行请求）
            //   dotnet run -c Release -- all          # 全部 BenchmarkDotNet 全跑一遍

            var mode = args is { Length: > 0 } ? args[0].ToLowerInvariant() : "report";

            switch (mode)
            {
                case "report":
                    await ConcurrencyReport.Run();
                    break;
                case "concurrency":
                    BenchmarkRunner.Run<BenchmarkConcurrency>();
                    break;
                case "route":
                    BenchmarkRunner.Run<BenchmarkRoute>();
                    break;
                case "request":
                    BenchmarkRunner.Run<BenchmarkRequest>();
                    break;
                case "multi":
                    BenchmarkRunner.Run<BenchmarkMultiClient>();
                    break;
                case "all":
                    BenchmarkRunner.Run<BenchmarkRoute>();
                    BenchmarkRunner.Run<BenchmarkRequest>();
                    BenchmarkRunner.Run<BenchmarkConcurrency>();
                    BenchmarkRunner.Run<BenchmarkMultiClient>();
                    await ConcurrencyReport.Run();
                    break;
                default:
                    Console.WriteLine($"Unknown mode: {mode}. Valid: report | concurrency | route | request | multi | all");
                    break;
            }
        }
    }
}
