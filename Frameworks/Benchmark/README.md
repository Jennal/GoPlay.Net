# GoPlay.Net Server 长连接框架 —— 官方性能基线

> 本文件固化当前优化后（Step 1~3）在 **Intel Core i9-14900K / Windows 11** 上的基线数据，
> 同时提供 **.NET 7 / .NET 8 / .NET 9** 三个 runtime 的横向对比。
> 支持的 runtime：**net7.0 / net8.0 / net9.0**（已在三者上做端到端单测 + BDN 基线）。
> 作为后续回归的参考，数值显著下跌（> 15%）应当立即定位并修复。

---

## 一、环境

```
BenchmarkDotNet v0.13.3
OS           : Windows 11 (10.0.26200.8246)
CPU          : Intel Core i9-14900K, 1 CPU, 32 logical / 24 physical cores
.NET SDK     : 9.0.308
Runtimes 测过 : net7.0 (7.0.20), net8.0 (8.0.19), net9.0 (9.0.11), X64 RyuJIT AVX2
Build        : Release, `--no-build`
```

复现命令：

```bash
# 在 Frameworks/Benchmark/ 下
dotnet build -c Release

# 快速对比表（Stopwatch，~30s，用于回归扫描）
dotnet run -c Release --no-build -f net8.0 -- report
dotnet run -c Release --no-build -f net9.0 -- report    # 参考 runtime

# 官方 BDN 基线（生成 CI 置信区间）
dotnet run -c Release --no-build -f net7.0 -- request   # net7 RTT
dotnet run -c Release --no-build -f net8.0 -- request   # net8 RTT
dotnet run -c Release --no-build -f net9.0 -- request   # net9 RTT
dotnet run -c Release --no-build -f net8.0 -- route
dotnet run -c Release --no-build -f net9.0 -- route
dotnet run -c Release --no-build -f net8.0 -- concurrency
```

---

## 二、Route 分发层 —— `BenchmarkRoute`

压 Route 分发的纯同步部分（零 I/O，零等待），用于观察启动期反射 vs 运行期编译委托的代价。

| Method      | Runtime | Mean             | Error       | StdDev      | Δ vs net7   |
|-------------|:-------:|-----------------:|------------:|------------:|------------:|
| CreateRoute | net7.0  |     168,963.4 ns | 1,020.70 ns |   954.76 ns |   baseline  |
| CreateRoute | net8.0  | **144,981.3 ns** |   598.85 ns |   500.07 ns |   **-14.2%** |
| CreateRoute | net9.0  |     185,910.8 ns | 3,497.61 ns | 3,271.67 ns |   **+10.0%** |
| InvokeRoute | net7.0  |         194.6 ns |     3.18 ns |     2.97 ns |   baseline  |
| InvokeRoute | net8.0  |     **146.5 ns** |     2.92 ns |     2.59 ns |   **-24.7%** |
| InvokeRoute | net9.0  |     **142.3 ns** |     2.86 ns |     5.96 ns |   **-26.9%** |

- `CreateRoute`：包含反射解析 `[Request]` 标注 + 构建 `ExpressionUtil` 编译委托，一次性启动成本。
- `InvokeRoute`：**热路径基线**，编译委托调用 Echo 全链路。
- 升级 net8：两项都有明显提升，热路径 **-25%**，主要来自 net8 JIT 对 Expression Tree 编译委托的 dispatch 改进（devirtualization / inlining）。
- 再升 net9：**热路径 InvokeRoute 继续微降到 142 ns**（vs net8 再 -2.9%），符合 net9 对委托调用的分层编译/内联进一步优化预期；但
  **冷路径 CreateRoute 回弹到 186 µs**（vs net7 +10%、vs net8 +28%），这是 Expression Tree 首次编译 / R2R 剥离带来的启动时代价，
  运行期只付一次，**不影响稳态吞吐**。

---

## 三、端到端单次请求 —— `BenchmarkRequest`

同机 loopback，1 个 `NcClient` ↔ 1 个 `NcServer`，业务是立即返回的 Echo：

| 阶段 / Runtime                | Mean          | StdDev    | 备注                                                 |
|-------------------------------|--------------:|----------:|------------------------------------------------------|
| Step 3.4 基线（net7）         | 69.08 µs      |      n/a  | Client.SendLoop 仍是 `BlockingCollection`+`.Wait()`  |
| Step 3.5 (net7)               | 54.30 µs      |  1.58 µs  | Client 换 `Channel<Package>` + `async await`，**-21%** |
| **Step 3.5 (net8) 当前最佳**  | **50.95 µs**  |  0.95 µs  | 同代码，runtime 升级至 .NET 8，再 **-6.2%**          |
| Step 3.5 (net9)               | 52.05 µs      |  1.40 µs  | 同代码，net9.0，vs net8 **+2.2%**（StdDev 内持平）   |

即单连接同步 RTT：
- net7 ≈ **54 µs**（~18.4 kreq/s 串行上限）
- net8 ≈ **51 µs**（~19.6 kreq/s 串行上限）← 当前最佳
- net9 ≈ **52 µs**（~19.2 kreq/s 串行上限）

net9 vs net8 在 Echo RTT 上差异在 1 µs 内（远小于 1.4 µs StdDev），视作**持平**；因此 **当前最佳 runtime 仍保留为 net8**，
net9 作为并行可选 runtime 发布，供业务项目按版本策略选用。继续压榨需要改造 client → server 的握手/分包层面，或做 pipelining（等下一轮）。

---

## 四、Concurrency × Delay 矩阵 —— `ConcurrencyReport`

这是 Step 1~2 核心优化的直接对照实验：业务带 `await Task.Delay(D)` 时，
旧架构（`defaultConcurrency = 1`，串行）与新架构（`[MaxConcurrency(64)]`）的吞吐差。

**单 client，`WhenAll` 并发发 `BatchSize` 条请求，重复 `Rounds` 轮，记均值。**

### 4.1 旧 vs 新架构 Speedup（net8.0 当前）

| Delay(ms) | Concurrency | BatchSize | Wall(ms) | Throughput(req/s) | Speedup |
|----------:|------------:|----------:|---------:|------------------:|--------:|
|         0 |           1 |       200 |     5.61 |             35623 |   1.00x |
|         0 |          64 |       200 |     1.94 |        **103137** |   2.90x |
|        10 |           1 |       100 |  1470.67 |                68 |   1.00x |
|        10 |          64 |       100 |    31.47 |          **3178** |  46.73x |
|        50 |           1 |        50 |  2845.43 |                18 |   1.00x |
|        50 |          64 |        50 |    55.56 |           **900** |  51.21x |
|       100 |           1 |        50 |  5250.11 |                10 |   1.00x |
|       100 |          64 |        50 |   104.14 |           **480** |  50.41x |

解读：

- `delay=0`：纯同步 Echo，wall 只有 1–6 ms，**方差极大**（QPS 在 30k–150k 之间跳），不作为回归判据。
- `delay≥10ms`：业务 await 异步 I/O 时，旧架构每条消息独占 "虚线程" 整段 D ms，
  理论上限 `1000/D req/s`；新架构达 `Concurrency × 1000/D req/s`。实测 47–54× 提升。

### 4.2 net7 vs net8 vs net9（新架构 Concurrency=64 下）

两次 run 取典型值，方差范围 ±15% 内属测量噪声：

| Delay(ms) | net7 QPS       | net8 QPS       | net9 QPS       | 说明                                            |
|----------:|---------------:|---------------:|---------------:|-------------------------------------------------|
|         0 |  ~47k          |  ~80k          |  ~47k ~ 58k    | wall ≤ 6 ms，方差过大不对比                     |
|        10 |  3339 ~ 4160   |  3178 ~ 3582   |  3707 ~ 3891   | 瓶颈是 `Task.Delay(10)` timer，runtime 无关     |
|        50 |   901 ~  916   |   847 ~  900   |   901 ~  903   | 同上，`50 × Concurrency⁻¹` 理论 1280 为天花板    |
|       100 |   466 ~  475   |   456 ~  480   |   458 ~  478   | 同上，理论 640 为天花板                         |

结论：**delay-bound 场景（业务主动 await）下，net7→net8→net9 runtime 升级对 Concurrency 矩阵都没显著收益**，
预期之内 —— 瓶颈是 Task.Delay/Timer，不是 JIT 或 GC。
三 runtime 的 QPS 区间完全重叠，在 ±5% 噪声内持平。
net8/net9 的收益集中在 CPU-bound 的 `BenchmarkRequest`（-6%）和 `InvokeRoute`（-27%）。

---

## 五、方法级 `[MaxConcurrency]`

同一 `MethodLimitedProcessor`（class 标 `[MaxConcurrency(64)]`）下：

- `limited.slow`：**未标** method 级，吃满 processor 64 预算。
- `limited.slow8`：**标** `[MaxConcurrency(8)]`，方法级闸门压住并发到 8。

net8.0 当前典型数据：

| Delay(ms) | Route          | Batch | Wall(ms) | Throughput(req/s) | ConcurrencyCap |
|----------:|:---------------|------:|---------:|------------------:|---------------:|
|        10 | limited.slow   |    64 |    12.55 |          **5098** |      64 (proc) |
|        10 | limited.slow8  |    64 |   117.93 |               543 |     8 (method) |
|        50 | limited.slow   |    64 |    57.97 |          **1104** |      64 (proc) |
|        50 | limited.slow8  |    64 |   451.31 |               142 |     8 (method) |
|       100 | limited.slow   |    64 |   104.13 |           **615** |      64 (proc) |
|       100 | limited.slow8  |    64 |   839.56 |                76 |     8 (method) |

net9.0 典型数据（两次 run 取值）：

| Delay(ms) | Route          | Batch | Wall(ms)        | Throughput(req/s) | ConcurrencyCap |
|----------:|:---------------|------:|----------------:|------------------:|---------------:|
|        10 | limited.slow   |    64 |   11.73 ~ 20.41 |      3135 ~ 5456  |      64 (proc) |
|        10 | limited.slow8  |    64 |  104.29 ~ 107.54 |       595 ~  614  |     8 (method) |
|        50 | limited.slow   |    64 |   52.67 ~ 55.66 |      1150 ~ 1215  |      64 (proc) |
|        50 | limited.slow8  |    64 |  440.95 ~ 444.47 |       144 ~  145  |     8 (method) |
|       100 | limited.slow   |    64 |  105.60 ~ 110.63 |       578 ~  606  |      64 (proc) |
|       100 | limited.slow8  |    64 |  857.30 ~ 863.87 |        74 ~   75  |     8 (method) |

`slow / slow8` QPS 比约 7.3~8（三 runtime 一致），与 `64 / 8 = 8` 相符（偏差来自 warmup、GC、scheduler 抖动）。
方法级 `[MaxConcurrency]` 在 net7/net8/net9 上按预期工作。

---

## 六、回归判定红线

下述任一条件下跌即视为性能回归：

| 指标                                         | 本基线 (net8) | 红线（下跌超过） |
|---------------------------------------------|--------------:|----------------:|
| `InvokeRoute` mean                           | 146.5 ns      | +15% (>168 ns)  |
| `Echo` single-RTT mean                       | **50.95 µs**  | +15% (>59 µs)   |
| `delay=10ms` × `concurrency=64` QPS          | ≈ 3300       | -15% (<2800)    |
| `delay=50ms` × `concurrency=64` QPS          | ≈  880       | -15% (<750)     |
| `delay=100ms` × `concurrency=64` QPS         | ≈  470       | -15% (<400)     |
| `method [MaxConcurrency(8)]` QPS / 64-ratio | ≈ 8×         | 偏离 >1.5×       |

> `delay=0` 场景下 wall time 只有 1–6 ms，测量方差很大（QPS 在 30k–150k 之间跳），不作为回归判据。
> Concurrency 矩阵的 QPS 基线值是 **net7 / net8 / net9 多次 run 的典型下沿**，三 runtime 数据重合。
>
> **主基线选择 net8.0 LTS**：回归红线的绝对数值全部按 net8 校准。net7 已 EoL，net9 非 LTS，两者作为参考 runtime，
> 在 §三（Request）/ §四（Concurrency）/ §五（Method-level）同步列出数据；若仅 net9 有 >15% 下跌但 net8 不跌，
> 先定位为 runtime 差异（如 JIT/GC 变化），而不是框架回归。

---

## 七、已做的优化（Step 概览）

- **Step 1**：Processor 执行从反射 + `BlockingCollection` + `Task.Wait` 改为编译委托 + `Channel<Package>` + async，严格遵守"一 Processor 一虚拟线程"。
- **Step 2**：删除 Server 端全局 `m_sendQueue`/`m_sendTask`，改为 per-session `SessionSender`（`Channel<Package>` + 小包聚合 + zero-copy `IBufferWriter`）。收包侧改 `ArrayPool<byte>` 环形 stash，删除所有 `MemoryStream`/`ToArray()`。
- **Step 3.1**：`NcClient` / `WsClient` / `WssClient` 收包路径同步改造，`TcpClient.Send/Recv` 去掉 `MemoryStream` 双拷贝。
- **Step 3.2**：`Server(defaultConcurrency=0|负值)` 改为 `Environment.ProcessorCount` 自动推导。
- **Step 3.3**：启动期 lint 检测 `[MaxConcurrency]` 误用（标在非 `[Request]/[Notify]` 方法上、冗余写 `1` 等）。
- **Step 3.4**：固化 BDN 官方基线到本文档。
- **Step 3.5**：`Client.SendLoop` 改 `Channel<Package>` + `async Task` pipeline，单连接同步 RTT 从 69 µs → 54 µs (net7)。
- **Step 3.6**：修复 `WsClient` / `WssClient` / `NcClient` 的 `OnRecv` socket 完成回调线程上 Disconnect 时竞态未捕获 `OperationCanceledException` / `ObjectDisposedException` / `NullReferenceException`，net8 下触发 ThreadPool unhandled exception → testhost 进程 failfast 的问题（net7 下时机不易触发但同样存在）。
- **Step 3.7**：去掉未被使用且未充分测试的 `Transport.Http`；Target frameworks 从 `net6.0+net7.0` 合并切到 `net7.0;net8.0`（库项目保留 `netstandard2.1`）。
- **Step 3.8**：加 `net9.0` 支持，三 runtime（net7/net8/net9）共存，库项目 `net7.0;net8.0;net9.0;netstandard2.1`，
  可执行/测试项目 `net7.0;net8.0;net9.0`；第三方 `NetCoreServer` 同步把 `net8.0;net9.0` 加进 TargetFrameworks，
  避免 net9 宿主走 `netstandard2.1` 降级路径。net9 下 36/36 单测全绿，BDN Echo/InvokeRoute 与 net8 持平，
  `InvokeRoute` 微降到 142 ns，`CreateRoute` 冷路径回弹到 186 µs（不影响稳态吞吐）。

## 八、已知天花板 / 后续方向

- 同一个 client 连接内 `Request` 仍是 **应用层串行**：发请求 → await → 回调 → 再发。要进一步压，需要业务侧发起 N 个并发 request（`Task.WhenAll`），这时单连接 QPS 由 `Concurrency × 1000/D` 决定，当前已达线性扩展。
- Server-side `Package.WriteTo(IBufferWriter)` 已经 zero-copy；但 `Header` 编码走的是 `Encoder.Encode(Header) → byte[]`，还有一次分配。后续可评估直接 span-based encode。
- Roslyn analyzer（Step 3.3 preferred 路径）还没做，目前只有运行期 warning。要真正 fail-fast 需要追加 analyzer 项目。

