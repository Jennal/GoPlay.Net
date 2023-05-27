``` ini

BenchmarkDotNet=v0.13.3, OS=macOS 13.2.1 (22D68) [Darwin 22.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD


```
|      Method |       Mean |   Error |  StdDev | Ratio |
|------------ |-----------:|--------:|--------:|------:|
| CreateRoute | 2,255.9 ns | 4.77 ns | 4.46 ns |  4.91 |
| InvokeRoute |   459.6 ns | 1.18 ns | 1.10 ns |  1.00 |
