``` ini

BenchmarkDotNet=v0.13.3, OS=macOS 13.2.1 (22D68) [Darwin 22.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD


```
| Method |     Mean |   Error |  StdDev | Ratio |
|------- |---------:|--------:|--------:|------:|
|   Echo | 217.9 μs | 3.63 μs | 4.72 μs |  1.00 |
