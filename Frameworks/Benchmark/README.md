# GoPlay.Net Server 长连接框架 —— 官方性能基线

> 本文件固化当前优化后（Step 1~3）在 **Intel Core i9-14900K / Windows 11** 上的基线数据，
> 同时提供 **.NET 7 / .NET 8 / .NET 9 / .NET 10** 四个 runtime 的横向对比。
> 支持的 runtime：**net7.0 / net8.0 / net9.0 / net10.0**（已在四者上做端到端单测 + BDN 基线）。
> 作为后续回归的参考，数值显著下跌（> 15%）应当立即定位并修复。

---

## 一、环境

```
BenchmarkDotNet v0.13.3
OS           : Windows 11 (10.0.26200.8246)
CPU          : Intel Core i9-14900K, 1 CPU, 32 logical / 24 physical cores
.NET SDK     : 10.0.202
Runtimes 测过 : net7.0 (7.0.20), net8.0 (8.0.19), net9.0 (9.0.11), net10.0 (10.0.6), X64 RyuJIT AVX2
Build        : Release, `--no-build`
```

复现命令：

```bash
# 在 Frameworks/Benchmark/ 下
dotnet build -c Release

# 快速对比表（Stopwatch，~30s，用于回归扫描）
dotnet run -c Release --no-build -f net8.0  -- report    # LTS 主基线
dotnet run -c Release --no-build -f net10.0 -- report    # 当前最佳 runtime
dotnet run -c Release --no-build -f net9.0  -- report    # 参考 runtime

# 官方 BDN 基线（生成 CI 置信区间）
dotnet run -c Release --no-build -f net7.0  -- request   # net7 RTT
dotnet run -c Release --no-build -f net8.0  -- request   # net8 RTT
dotnet run -c Release --no-build -f net9.0  -- request   # net9 RTT
dotnet run -c Release --no-build -f net10.0 -- request   # net10 RTT
dotnet run -c Release --no-build -f net8.0  -- route
dotnet run -c Release --no-build -f net9.0  -- route
dotnet run -c Release --no-build -f net10.0 -- route
dotnet run -c Release --no-build -f net8.0  -- concurrency
```

---

## 二、Route 分发层 —— `BenchmarkRoute`

压 Route 分发的纯同步部分（零 I/O，零等待），用于观察启动期反射 vs 运行期编译委托的代价。

| Method      | Runtime  | Mean             | Error        | StdDev       | Δ vs net7    |
|-------------|:--------:|-----------------:|-------------:|-------------:|-------------:|
| CreateRoute | net7.0   |     168,963.4 ns |  1,020.70 ns |    954.76 ns |   baseline   |
| CreateRoute | net8.0   | **144,981.3 ns** |    598.85 ns |    500.07 ns |   **-14.2%** |
| CreateRoute | net9.0   |     185,910.8 ns |  3,497.61 ns |  3,271.67 ns |   **+10.0%** |
| CreateRoute | net10.0  |     648,675.4 ns |  2,644.69 ns |  2,473.84 ns |  **+283.9%** |
| InvokeRoute | net7.0   |         194.6 ns |      3.18 ns |      2.97 ns |   baseline   |
| InvokeRoute | net8.0   |         146.5 ns |      2.92 ns |      2.59 ns |   **-24.7%** |
| InvokeRoute | net9.0   |         142.3 ns |      2.86 ns |      5.96 ns |   **-26.9%** |
| InvokeRoute | net10.0  |      **97.0 ns** |      1.08 ns |      0.96 ns |   **-50.2%** |

- `CreateRoute`：包含反射解析 `[Request]` 标注 + 构建 `ExpressionUtil` 编译委托，一次性启动成本。
- `InvokeRoute`：**热路径基线**，编译委托调用 Echo 全链路。
- 升级 net8：两项都有明显提升，热路径 **-25%**，主要来自 net8 JIT 对 Expression Tree 编译委托的 dispatch 改进（devirtualization / inlining）。
- 再升 net9：**热路径 InvokeRoute 继续微降到 142 ns**（vs net8 再 -2.9%），符合 net9 对委托调用的分层编译/内联进一步优化预期；但
  **冷路径 CreateRoute 回弹到 186 µs**（vs net7 +10%、vs net8 +28%），这是 Expression Tree 首次编译 / R2R 剥离带来的启动时代价，
  运行期只付一次，**不影响稳态吞吐**。
- net10 再次大跃进：**热路径 InvokeRoute 骤降到 97 ns**（vs net7 **-50%**、vs net8 **-34%**、vs net9 **-32%**），
  主要来自 net10 的 Dynamic PGO 默认开启 + guarded devirtualization 对间接委托调用命中更高。
  **冷路径 CreateRoute 激增到 649 µs**（vs net8 **+348%**）—— Expression Tree 首编译在 net10 下更保守，
  把内联/R2R 的决策后置到 tiered compilation，**一次性成本**，运行几毫秒稳态后完全摊平，不进入回归红线。

---

## 三、端到端单次请求 —— `BenchmarkRequest`

同机 loopback，1 个 `NcClient` ↔ 1 个 `NcServer`，业务是立即返回的 Echo：

> **Benchmark 策略**：`BenchmarkRequest` 常驻开启 `[MemoryDiagnoser]`，因为 §六 回归红线里
> 有 "Echo Allocated / op" 这一条，不开就看不到分配量变化。代价是本 benchmark 跑时长
> +40–50%（≈ 15 s → 22 s），可接受。`BenchmarkConcurrency` / `BenchmarkRoute` 不开：
> Concurrency 每 invocation 跑几十上百 request，分配被 batch 链路的 per-batch 开销稀释，信噪比差；
> Route 纯委托调用本就是 0 alloc，信号密度低；两者开了只换来跑时间变长。

| 阶段 / Runtime                 | Mean          | StdDev    | 备注                                                 |
|--------------------------------|--------------:|----------:|------------------------------------------------------|
| Step 3.4 基线（net7）          | 69.08 µs      |      n/a  | Client.SendLoop 仍是 `BlockingCollection`+`.Wait()`  |
| Step 3.5 (net7)                | 54.30 µs      |  1.58 µs  | Client 换 `Channel<Package>` + `async await`，**-21%** |
| Step 3.5 (net8) LTS 主基线     | 50.95 µs      |  0.95 µs  | 同代码，runtime 升级至 .NET 8，再 **-6.2%**          |
| Step 3.5 (net9)                | 52.05 µs      |  1.40 µs  | 同代码，net9.0，vs net8 **+2.2%**（StdDev 内持平）   |
| Step 3.5 (net10)               | 46.28 µs      |  1.69 µs  | 同代码，net10.0，vs net8 **-9.2%**、vs net9 **-11.1%** |
| Step 3.10 (net8)                | 49.28 µs      |  0.82 µs  | Header 改 span-based encode，省 1 次 `byte[]` 分配 |
| Step 3.10 (net10)               | 45.08 µs      |  0.95 µs  | 同改造，net10.0，vs Step 3.5 再 **-2.6%**           |
| Step 3.11 (net8)                | 48.12 µs      |  0.34 µs  | body 也走 span-based encode，小消息热路径 0 byte[] 中间分配 |
| Step 3.11 (net10)               | 42.77 µs      |  0.27 µs  | 同改造，net10.0，vs Step 3.5 累计 **-7.6%**         |
| Step 3.12 (net8) LTS 主基线     | **47.75 µs**  |  0.92 µs  | 收包路径 Header 也 zero-alloc，`Header.Parse(ReadOnlySpan<byte>)` |
| **Step 3.12 (net10) 当前最佳**  | **43.77 µs**  |  0.86 µs  | 同改造，net10.0（RTT 在 StdDev 内持平 3.11）        |

即单连接同步 RTT（Step 3.12 后当前基线）：
- net7  ≈ **54 µs**（~18.4 kreq/s 串行上限）
- net8  ≈ **48 µs**（~20.9 kreq/s 串行上限）← LTS 主基线
- net10 ≈ **44 µs**（~22.9 kreq/s 串行上限）← 当前最佳

Step 3.10（发包 Header span-based zero-alloc）+ Step 3.11（发包 body 也 span-based）+ Step 3.12（收包 Header 也 span-based）
的收益，用 `MemoryDiagnoser` 量化（端到端 Echo，client→server→client 完整 RTT）：

| Runtime | Phase                         | Mean      | Allocated / op | Gen0 / 1k op |
|---------|-------------------------------|----------:|---------------:|-------------:|
| net8    | Step 3.9 (BEFORE)             | 49.96 µs  |   27.60 KB     |   1.8311     |
| net8    | Step 3.10 (send header)       | 49.28 µs  |   23.14 KB     |   1.4648     |
| net8    | Step 3.11 (send header+body)  | 48.12 µs  |   14.22 KB     |   0.7324     |
| net8    | **Step 3.12 (+ recv header)** | **47.75 µs** | **13.81 KB** | **0.7324** |
| net10   | Step 3.9 (BEFORE)             | 47.52 µs  |   27.44 KB     |   1.8311     |
| net10   | Step 3.10 (send header)       | 45.08 µs  |   22.98 KB     |   1.4648     |
| net10   | Step 3.11 (send header+body)  | 42.77 µs  |   14.06 KB     |   0.7324     |
| net10   | **Step 3.12 (+ recv header)** | **43.77 µs** | **13.65 KB** | **0.7324** |

- **Step 3.9 → 3.12 累计**：分配 27.4 KB → 13.65 KB（**-50%**）、Gen0 1.83 → 0.73 (**-60%**)、
  net10 RTT 47.52 → 43.77 µs（**-7.9%**）、net8 RTT 49.96 → 47.75 µs（**-4.4%**）；
- **Step 3.12 单步收益**：分配再 -2.9%（net10）/ -2.9%（net8），Gen0 持平（已到 1 次以内/op），
  RTT 在 StdDev 内持平。收益侧重 **GC 压力**，在高 QPS、长连接、大量订阅的 server 场景下放大更显著；
- 实现方式：`Header.Parse` 新增 `ReadOnlySpan<byte>` 重载，内部调用 `MessageParser.ParseFrom(ReadOnlySpan<byte>)`
  （Protobuf 3.15+ API，0 alloc 消费 span）；`Package.ParseRaw(ReadOnlySpan<byte>)` 去掉
  `headerBytes = data.Slice(...).ToArray()`，直接把 span 切片喂给 `Header.Parse`；
- body 侧仍然需要 `.ToArray()`（下游 handler 链 async 持有 `Package.RawData`，不能留 span 引用到 TCP 接收缓冲），
  后续可改 `ArrayPool<byte>.Rent` + 生命周期管理（§八）；
- `TestSplit` / 36/36 单测在 net8/net10 上一次绿。

**回归红线主基线仍锚定 net8 LTS**（见 §六），net10 是 STS（生命周期 18 个月），作为"当前最佳 runtime"公布，业务侧按版本策略自选。
后续优化空间见 §八。

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

### 4.2 net7 vs net8 vs net9 vs net10（新架构 Concurrency=64 下）

两次 run 取典型值，方差范围 ±15% 内属测量噪声：

| Delay(ms) | net7 QPS       | net8 QPS       | net9 QPS       | net10 QPS      | 说明                                            |
|----------:|---------------:|---------------:|---------------:|---------------:|-------------------------------------------------|
|         0 |  ~47k          |  ~80k          |  ~47k ~ 58k    |  ~34k ~ 138k   | wall ≤ 6 ms，方差过大不对比                     |
|        10 |  3339 ~ 4160   |  3178 ~ 3582   |  3707 ~ 3891   |  3152 ~ 3170   | 瓶颈是 `Task.Delay(10)` timer，runtime 无关     |
|        50 |   901 ~  916   |   847 ~  900   |   901 ~  903   |   792 ~  795   | 同上，`50 × Concurrency⁻¹` 理论 1280 为天花板    |
|       100 |   466 ~  475   |   456 ~  480   |   458 ~  478   |   449 ~  454   | 同上，理论 640 为天花板                         |

结论：**delay-bound 场景（业务主动 await）下，net7→net8→net9→net10 runtime 升级对 Concurrency 矩阵都没显著收益**，
预期之内 —— 瓶颈是 Task.Delay/Timer，不是 JIT 或 GC。
四 runtime 的 QPS 区间完全重叠，在 ±15% 噪声内持平。
net8/net9/net10 的收益集中在 CPU-bound 的 `BenchmarkRequest`（net10 -9.2%）和 `InvokeRoute`（net10 -34%）。

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

net10.0 典型数据（两次 run 取值）：

| Delay(ms) | Route          | Batch | Wall(ms)         | Throughput(req/s) | ConcurrencyCap |
|----------:|:---------------|------:|-----------------:|------------------:|---------------:|
|        10 | limited.slow   |    64 |   14.06 ~ 19.00  |      3368 ~ 4551  |      64 (proc) |
|        10 | limited.slow8  |    64 |  124.64 ~ 126.30 |       507 ~  513  |     8 (method) |
|        50 | limited.slow   |    64 |   62.46 ~ 63.48  |      1008 ~ 1025  |      64 (proc) |
|        50 | limited.slow8  |    64 |  498.04 ~ 501.36 |       128 ~  129  |     8 (method) |
|       100 | limited.slow   |    64 |  109.55 ~ 110.95 |       577 ~  584  |      64 (proc) |
|       100 | limited.slow8  |    64 |  871.76 ~ 886.63 |        72 ~   73  |     8 (method) |

`slow / slow8` QPS 比约 7.0~8（四 runtime 一致），与 `64 / 8 = 8` 相符（偏差来自 warmup、GC、scheduler 抖动）。
方法级 `[MaxConcurrency]` 在 net7/net8/net9/net10 上按预期工作。

---

## 六、回归判定红线

下述任一条件下跌即视为性能回归：

| 指标                                         | 本基线 (net8) | 红线（下跌超过） |
|---------------------------------------------|--------------:|----------------:|
| `InvokeRoute` mean                           | 146.5 ns      | +15% (>168 ns)  |
| `Echo` single-RTT mean                       | **47.75 µs**  | +15% (>55 µs)   |
| `Echo` Allocated / op                        | **13.81 KB**  | +15% (>16 KB)   |
| `delay=10ms` × `concurrency=64` QPS          | ≈ 3300       | -15% (<2800)    |
| `delay=50ms` × `concurrency=64` QPS          | ≈  880       | -15% (<750)     |
| `delay=100ms` × `concurrency=64` QPS         | ≈  470       | -15% (<400)     |
| `method [MaxConcurrency(8)]` QPS / 64-ratio | ≈ 8×         | 偏离 >1.5×       |

> `delay=0` 场景下 wall time 只有 1–6 ms，测量方差很大（QPS 在 30k–150k 之间跳），不作为回归判据。
> Concurrency 矩阵的 QPS 基线值是 **net7 / net8 / net9 / net10 多次 run 的典型下沿**，四 runtime 数据重合。
>
> **主基线选择 net8.0 LTS**：回归红线的绝对数值全部按 net8 校准。net7 已 EoL，net9 / net10 是 STS（18 个月），
> 三者作为参考 runtime，在 §三（Request）/ §四（Concurrency）/ §五（Method-level）同步列出数据；
> 若仅 net9 / net10 有 >15% 下跌但 net8 不跌，先定位为 runtime 差异（如 JIT/GC/Dynamic PGO 变化），而不是框架回归。
>
> **例外**：`CreateRoute` 冷路径在 net10 下 +348%（Expression Tree 首编译策略变化），只付一次，不纳入回归红线；
> 若 net10 下 `CreateRoute` 反而又回到 150 µs 量级，才是"意外"（JIT 策略被业务代码干扰）需要调查。

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
- **Step 3.9**：加 `net10.0` 支持，四 runtime（net7/net8/net9/net10）共存，库项目
  `net7.0;net8.0;net9.0;net10.0;netstandard2.1`，可执行/测试项目 `net7.0;net8.0;net9.0;net10.0`；
  `NetCoreServer` 同步加 `net10.0`。net10 下 36/36 单测一次绿，Echo RTT 从 net8 的 50.95 µs 降到
  **46.28 µs（-9.2%）**，`InvokeRoute` 从 146 ns 降到 **97 ns（-34%）**，主要来自 net10 Dynamic PGO 默认
  开启 + guarded devirtualization 对 Channel / async state machine 的命中。`CreateRoute` 冷路径反弹到 649 µs，
  属首编译策略变化，一次性成本，不影响稳态。net10 定为"当前最佳 runtime"，**但回归红线仍锚定 net8 LTS**。
- **Step 3.10**：`Package.WriteTo` 的 Header 编码改 span-based zero-alloc。
  `IEncoder` 接口新增 `GetEncodedSize<T>` / `EncodeTo<T>(T, Memory<byte>)`，`ProtobufEncoder` 走
  `IMessage.CalculateSize()` + `MessageExtensions.WriteTo(IBufferWriter<byte>)` via ThreadStatic
  `MemoryBufferWriter`，把 Header wire bytes 直接刷进 `IBufferWriter` 申请的同一块 span；
  `Package.WriteFrame` 改为 `writer.GetMemory(totalLen)` 一次预占 + 原地写 `[outerLen][headerLen][header][body]`。
  `JsonEncoder` 走 `Encoding.UTF8.GetBytes(string, Span<byte>)` fallback。
  实测 MemoryDiagnoser（net10 Echo）：Allocated **27.44 KB → 22.98 KB（-16%）**、Gen0 **1.83 → 1.46 /1k op（-20%）**，
  RTT **47.52 → 45.08 µs（-5.1%，显著）**；net8 同改造 49.96 → 49.28 µs（StdDev 内方向一致），Allocated -16%。
  36/36 单测在 net8/net10 上一次绿。
- **Step 3.11**：`Package<T>` 的 body 编码也走 zero-alloc 路径。
  `Package` 新增 `protected virtual int GetBodyEncodedSize()`（base 返回 RawData?.Length），
  `Package<T>` 覆盖为 `RawData ?? encoder.GetEncodedSize(Data)`（Protobuf 走 `CalculateSize()`，0 alloc）。
  `Split()` 用 `GetBodyEncodedSize` 决定是否分块：**小消息热路径不再预编码 body**（保持 `RawData = null`），
  分块大消息才落盘 `RawData`。`Package<T>.WriteTo` 在 `RawData == null` 时通过新加的
  `WriteFrameWithDataBody<TData>` 让 Header 与 Data 都用 `IEncoder.EncodeTo` 直接写到 `IBufferWriter`
  申请的同一块连续 span，全程 0 byte[] 中间分配（Protobuf 路径）。
  实测 MemoryDiagnoser（net10 Echo）：Allocated **22.98 KB → 14.06 KB（-39%）**、Gen0 **1.46 → 0.73 /1k op（-50%）**，
  RTT **45.08 → 42.77 µs（-5.1%）**；net8 同改造 49.28 → 48.12 µs（-2.4%），Allocated -38.5%。
  Step 3.9 → 3.11 累计：分配 27.4 KB → 14.06 KB（**-49%**）、Gen0 1.83 → 0.73（**-60%**）、net10 RTT -10%。
  36/36 单测在 net8/net10 上一次绿（含 `TestSplit` 大消息分块场景）。
- **Step 3.12**：收包路径 `Header` 也走 zero-alloc。
  `Header.Parse` 新增 `ReadOnlySpan<byte>` 重载，内部调用 Google.Protobuf 3.15+ 的
  `MessageParser.ParseFrom(ReadOnlySpan<byte>)`（0 alloc 消费 span）。
  `Package.ParseRaw(ReadOnlySpan<byte>)` 去掉 `headerBytes = data.Slice(...).ToArray()`，
  直接把 span 切片喂给新的 `Header.Parse` span 重载。热路径上每次 Echo 收包省 1 次 header 字节分配。
  实测 MemoryDiagnoser（net10 Echo）：Allocated **14.06 KB → 13.65 KB（-2.9%）**、
  net8 14.22 KB → 13.81 KB（-2.9%），RTT 在 StdDev 内持平。
  Step 3.9 → 3.12 累计：分配 27.4 KB → 13.65 KB（**-50%**）、Gen0 1.83 → 0.73（**-60%**）、
  net10 RTT -7.9%、net8 RTT -4.4%。收益偏向 GC 压力，高 QPS / 多订阅场景放大。
  body 侧仍需 `.ToArray()`（async 持有约束），下一步用 `ArrayPool` 改造（见 §八）。
  36/36 单测在 net8/net10 上一次绿。

## 八、已知天花板 / 后续方向

- 同一个 client 连接内 `Request` 仍是 **应用层串行**：发请求 → await → 回调 → 再发。要进一步压，需要业务侧发起 N 个并发 request（`Task.WhenAll`），这时单连接 QPS 由 `Concurrency × 1000/D` 决定，当前已达线性扩展。
- ~~Server-side `Package.WriteTo(IBufferWriter)` 已经 zero-copy；但 `Header` 编码走的是 `Encoder.Encode(Header) → byte[]`，还有一次分配。后续可评估直接 span-based encode。~~ ← **Step 3.10 已完成**，Header 走 span-based zero-alloc，Echo 分配 -16%、Gen0 -20%。
- ~~`Package<T>` 的 body 编码仍走 `encoder.Encode(Data) → byte[]`（通过 `UpdateContentSize`）。~~
  ← **Step 3.11 已完成**，body 同样 span-based zero-alloc，Echo 累计分配 -49%、Gen0 -60%、net10 RTT -10%。
- ~~收包路径 `Package.ParseRaw` 仍然 `data.Slice(...).ToArray()` 分配 `headerBytes`。~~
  ← **Step 3.12 已完成**，`Header.Parse(ReadOnlySpan<byte>)` 直接消费 span，Echo 分配再 -2.9%。
- 收包路径 `body = data.Slice(...).ToArray()` 仍然分配。下游 handler 链 async 持有 `Package.RawData`，
  不能留 span 引用到 TCP 接收缓冲，真正做零分配需要改成 `ArrayPool<byte>.Rent` + `Package.ReleaseRawData()`
  生命周期管理（handler 完成后归还），风险点是 `Package.Clone()` 与多订阅分发时的归还时机。
  预期 Echo 再省 ~300–500 B/op、Gen0 继续下探。
- 大 body（≥ MAX_CHUNK_SIZE）分块路径仍然 `new byte[chunkSize]` per chunk，可以换 `ArrayPool<byte>.Rent`，但需要约束生命周期。
- Roslyn analyzer（Step 3.3 preferred 路径）还没做，目前只有运行期 warning。要真正 fail-fast 需要追加 analyzer 项目。

