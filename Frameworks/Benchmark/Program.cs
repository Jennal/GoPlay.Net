using BenchmarkDotNet.Running;

namespace GoPlay.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchmarkRoute>();
            BenchmarkRunner.Run<BenchmarkRequest>();
        }
    }
}