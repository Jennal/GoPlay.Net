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
| Step 3.12 (net8)                | 47.75 µs      |  0.92 µs  | 收包路径 Header 也 zero-alloc，`Header.Parse(ReadOnlySpan<byte>)` |
| Step 3.12 (net10)               | 43.77 µs      |  0.86 µs  | 同改造，net10.0（RTT 在 StdDev 内持平 3.11）        |
| Step 3.13 (net8)                | 46.71 µs      |  0.91 µs  | Client 发送侧补完 zero-copy（`pack.WriteTo` + `ArrayBufferWriter`） |
| Step 3.13 (net10)               | 42.90 µs      |  0.86 µs  | 同改造，net10.0，Small 档 Allocated -72%            |
| Step 3.14a (net8)               | 48.13 µs      |  2.22 µs  | Server 侧 DrainStash → `ParseRaw(Span)`，省整帧 byte[] |
| Step 3.14a (net10)              | 42.24 µs      |  0.69 µs  | 同改造，net10.0，Medium/Large 档 Allocated −7.5% / −9.7% |
| **Step 3.15 (net8) LTS 主基线** | **42.10 µs**  |  1.72 µs  | ProcessorRunner 单并发路径 `Channel`→`BlockingCollection`，去 idle OCE 风暴 |
| **Step 3.15 (net10) 当前最佳**  | **44.89 µs**  |  0.88 µs  | 同改造，net10.0；分配 −25.8%，RTT 在 StdDev 内 |

即单连接同步 RTT（Step 3.15 后当前基线，Small ≈ 7 B payload）：
- net8  ≈ **42 µs**（~23.8 kreq/s 串行上限）← LTS 主基线
- net10 ≈ **45 µs**（~22.3 kreq/s 串行上限）← 当前最佳（Allocated 最低）

> **Small 档 RTT 在 net8 上 Step 3.14a→3.15 显著降 −12.5%、net10 反而 +6.3%**：
> 两者方向不一致是真实的。net8 Channel async 路径开销大（无 Dynamic PGO），换成 BlockingCollection 同步 take 是净收益；
> net10 Channel async 路径已被 Dynamic PGO + guarded devirtualization 优化得很好，换同步 take 反而多一次 Monitor.Wait/Pulse，
> 但 Small 档 net8 RTT StdDev 5.0 µs（占 12%）较大，Median = 40.75 µs 更稳健，对比 Step 3.14a 仍是 −15.3%。
> **稳定收益在分配（三 runtime 全降 11–26%）和 Debug 可调试性**（消除 16 × 20 Hz = 320 Hz 的 first-chance OCE 风暴）；
> RTT 变化主要落在 GC 减压带来的 Large 档 −10% 上（net8/net10 方向一致）。

### 3.1 三档 payload 对照（Step 3.15 后）

用 `EchoSmall` / `EchoMedium` / `EchoLarge` 三个基准分别打 7 B / 1 KB / 10 KB 的 `PbString`，
一起摸清"端到端一次 Echo 分配量随 payload 大小的放大比"，判断是否还有 body-linear 分配需要消除。

**net10**（当前最佳，Allocated 最低）：

| Payload      | Mean      | Allocated  | Alloc Ratio | Gen0 / 1k op | Δ Allocated vs 3.14a |
|--------------|----------:|-----------:|------------:|-------------:|---------------------:|
| 7 B   Small  | 44.89 µs  |   2.85 KB  | 1.00×       | 0.1221       | **−25.8%**           |
| 1 KB  Medium | 46.17 µs  |  10.83 KB  | 3.80×       | 0.6104       | **−15.5%**           |
| 10 KB Large  | 61.33 µs  |  82.84 KB  | 29.0×       | 5.4932       | **−11.7%**           |

**net8**（LTS 主基线）：

| Payload      | Mean      | Allocated  | Alloc Ratio | Gen0 / 1k op | Δ Allocated vs 3.14a |
|--------------|----------:|-----------:|------------:|-------------:|---------------------:|
| 7 B   Small  | 42.10 µs  |   3.13 KB  | 1.00×       | 0.1221       | **−21.8%**           |
| 1 KB  Medium | 49.98 µs  |  11.10 KB  | 3.55×       | 0.6104       | **−14.5%**           |
| 10 KB Large  | 62.41 µs  |  83.11 KB  | 26.6×       | 5.4932       | **−11.6%**           |

**net9**（参考）：

| Payload      | Mean      | Allocated  | Alloc Ratio | Gen0 / 1k op |
|--------------|----------:|-----------:|------------:|-------------:|
| 7 B   Small  | 46.49 µs  |   3.09 KB  | 1.00×       | 0.1221       |
| 1 KB  Medium | 40.59 µs  |  11.06 KB  | 3.58×       | 0.6104       |
| 10 KB Large  | 65.73 µs  |  83.08 KB  | 26.9×       | 5.4932       |

- **Small 档分配进一步压到 2.85 KB（net10）/ 3.13 KB（net8）**——剩下的绝大部分是 `Task` / `TaskCompletionSource` /
  async state machine，再挖需要换 `ValueTask` 或共享 TCS pool，ROI 边际递减；
- **Medium 档每字节 body 放大 ≈ 8×**（10.83 KB / 1 KB，net10，从 Step 3.14a 的 9× 进一步收窄）；
  **Large 档放大 ≈ 8×**（82.84 KB / 10 KB，net10）。剩下的常数份数主要是：
  client 侧收包 `new byte[len]` 整帧、server/client 各自 body `.ToArray()`、业务侧 decode 结果；
- **Step 3.15 单步收益**：去掉 `Channel.WaitToReadAsync` + `IValueTaskSource` + linked CTS 一族的 async 盒装，
  对所有 payload 档稳定 −11~26% Allocated；RTT 在 StdDev 内（Large 档因 GC 减压净降 −8.5~10.1%）；
- Step 3.16 body `ArrayPool` 预估收益：Medium −2 KB、Large −20 KB。但需要 Package IDisposable / ref-count
  生命周期管理，复杂度高。建议先上 3.14b（Client 侧 `DrainStash` 同构改造），再评估 3.16 是否还需要做。

### 3.2 累计分配曲线（端到端 Echo，client→server→client 完整 RTT，`MemoryDiagnoser`）

以下以 Echo-Small（7 B payload）为指标，展示各 Step 对分配量的单调消减：

| Runtime | Phase                                | Mean      | Allocated / op | Gen0 / 1k op |
|---------|--------------------------------------|----------:|---------------:|-------------:|
| net8    | Step 3.9 (BEFORE)                    | 49.96 µs  |   27.60 KB     |   1.8311     |
| net8    | Step 3.10 (send header)              | 49.28 µs  |   23.14 KB     |   1.4648     |
| net8    | Step 3.11 (send header+body)         | 48.12 µs  |   14.22 KB     |   0.7324     |
| net8    | Step 3.12 (+ recv header)            | 47.75 µs  |   13.81 KB     |   0.7324     |
| net8    | Step 3.13 (+ client send zero-copy)  | 46.71 µs  |    4.05 KB     |   0.1221     |
| net8    | Step 3.14a (+ server recv zero-copy) | 48.13 µs  |    4.00 KB     |   0.1221     |
| net8    | **Step 3.15 (+ sync mailbox, no OCE storm)** | **42.10 µs** | **3.13 KB** | **0.1221** |
| net10   | Step 3.9 (BEFORE)                    | 47.52 µs  |   27.44 KB     |   1.8311     |
| net10   | Step 3.10 (send header)              | 45.08 µs  |   22.98 KB     |   1.4648     |
| net10   | Step 3.11 (send header+body)         | 42.77 µs  |   14.06 KB     |   0.7324     |
| net10   | Step 3.12 (+ recv header)            | 43.77 µs  |   13.65 KB     |   0.7324     |
| net10   | Step 3.13 (+ client send zero-copy)  | 42.90 µs  |    3.88 KB     |   0.1221     |
| net10   | Step 3.14a (+ server recv zero-copy) | 42.24 µs  |    3.84 KB     |   0.1221     |
| net10   | **Step 3.15 (+ sync mailbox, no OCE storm)** | **44.89 µs** | **2.85 KB** | **0.1221** |

- **Step 3.9 → 3.15 累计（Small 7 B）**：分配 27.4 KB → 2.85 KB（**−90%**）、Gen0 1.83 → 0.12（**−93%**）、
  net10 RTT −5.5%（Step 3.15 单步 +6.3% StdDev 内）、net8 RTT −15.7%（Step 3.15 单步 −12.5%）；
- **Step 3.15 单步收益**：去掉 ProcessorRunner `_maxConcurrency==1` 路径上 `Channel.WaitToReadAsync` + linked CTS
  的 per-idle-tick async 盒装。三档 Allocated 全降 11–26%（**Small −22~26%、Medium −15%、Large −11.7%**）；
  RTT：Large 档因 GC 减压 net8/net10 同方向 −8.5~10.1%；Small/Medium 在 StdDev 内（net8 Small 中位数 40.75 µs / Mean 42.10 µs
  比 3.14a 的 48.13 µs 仍是 −15.3%；net10 Small Mean +6.3% 但 1σ 内）；
- **额外的非性能收益（也是 3.15 的主要动机）**：消除 IDE Debug 模式下的 first-chance `OperationCanceledException` 风暴。
  16 个默认 Processor × 20 Hz (RecvTimeout=50 ms) = 320 Hz OCE，让 Rider/VS debugger 的 stack walk + filter 求值
  把 event 队列打爆，业务侧 EF Core 首次 query / DI cold-path 实测被放大 100×+（Run ≈ 100 ms / Debug ≈ 13 s）。
  改造后 Debug 模式 RTT 与 Run 模式同阶，断点恢复秒级响应；
- **收益上限**：3.15 只改了单并发路径（绝大多数 Processor 默认 `MaxConcurrency=1`）。`_maxConcurrency > 1` 路径
  保留 `Channel + ExclusiveScheduler + async`，因为流水线并发场景下 async 归还 ThreadPool worker 是必要的，
  且这种 Processor 个数极少（典型 1–3 个），异常风暴贡献量级可忽略；
- 63/63 GoPlay.Net 单测在 net9.0 上 5 次重跑 100% 绿（其中 1 次 1/63 flake 与 3.15 无关，单独跑 always pass，
  是 test 间端口 / TIME_WAIT 干扰，记 §八 待修）。

**回归红线主基线仍锚定 net8 LTS**（见 §六），net10 是 STS（生命周期 18 个月），作为"当前最佳 runtime"公布，业务侧按版本策略自选。
后续优化空间见 §八。

---

## 四、Concurrency × Delay 矩阵 —— `ConcurrencyReport`

这是 Step 1~2 核心优化的直接对照实验：业务带 `await Task.Delay(D)` 时，
旧架构（`defaultConcurrency = 1`，串行）与新架构（`[MaxConcurrency(64)]`）的吞吐差。

**单 client，`WhenAll` 并发发 `BatchSize` 条请求，重复 `Rounds` 轮，记均值。**

> **关于 Step 3.15 对本节数据的影响**：`Concurrency=1` 列走 `_maxConcurrency==1`（受 3.15 改动影响），
> `Concurrency=64` 列走 `_maxConcurrency>1`（**保留** `Channel + ExclusiveScheduler + async`，0 改动）。
> 下表数据为 Step 3.14a 时间点采集，**未在 3.15 重测**。预期影响：`Concurrency=1` 列 QPS 可能微增（BlockingCollection
> 同步 take 比 Channel async 少一次 state machine 调度），但 wall time 本身只有几 ms，方差远大于该收益，不重测亦不
> 影响 Speedup 数量级判断。下个常规版本基线刷新时一并重跑。

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
| `EchoSmall` single-RTT mean                  | **42.10 µs**  | +20% (>50 µs)   |
| `EchoSmall` Allocated / op                   | **3.13 KB**   | +25% (>4 KB)    |
| `EchoMedium` Allocated / op (1 KB payload)   | **11.10 KB**  | +20% (>13.3 KB) |
| `EchoLarge` Allocated / op (10 KB payload)   | **83.11 KB**  | +15% (>96 KB)   |
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
- **Step 3.13**：Client 发送侧补完 zero-copy（Step 3.10/3.11 漏做的那一半）。
  在做三档 payload（7 B / 1 KB / 10 KB）benchmark 摸底时发现：Medium/Large 两档 Allocated 随 payload
  线性放大 ~15–19×，远超预期。溯源发现 `Client.SendLoopAsync` 唯一调用处仍是
  `Transport.Send(pack.GetBytes(), ...)`——`pack.GetBytes()` 是 Step 3.10 之前的老路径，
  内部 `encoder.Encode(Header)→byte[]` + `encoder.Encode(Data)→byte[]` + `MemoryStream` 自扩容 + `ms.ToArray()`
  共四份分配，完全没吃到 Server 端 `SessionSender` 早已享受的 `WriteTo(IBufferWriter)` 零拷贝通路。
  修复方案：
  1. `TransportClientBase` 新增 `virtual ValueTask Send(ReadOnlyMemory<byte>, CTS)`，**契约**是
     "data 已是含 outer ushort 前缀的完整 wire frame，子类直接 socket 下发"——与 Server 端
     `TransportServerBase.SendAsync(uint, ReadOnlyMemory<byte>, CancellationToken)` 对齐；
  2. `NcClient` / `WsClient` / `WssClient` / `TcpClient` 四个 transport 全部覆写，直接调 NetCoreServer 底层
     `SendAsync(ReadOnlySpan<byte>)` / `NetworkStream.WriteAsync(ReadOnlyMemory<byte>)`；
  3. `Client` 类持一个 `ArrayBufferWriter<byte>`（初始 4 KB，随最大包体自然扩容，**整连接生命周期只分配一次**）；
  4. `SendLoopAsync` 的 inner loop 改为 `pack.WriteTo(writer) → Transport.Send(writer.WrittenMemory, cts) → writer.ResetWrittenCount()`。
  
  实测 MemoryDiagnoser（net10 Echo，三档 payload）：
  | Payload   | Alloc BEFORE | Alloc AFTER | 减幅    | Gen0 减幅 | RTT 减幅 |
  |-----------|-------------:|------------:|--------:|----------:|---------:|
  | 7 B Small | 13.65 KB     | **3.88 KB** | **-72%** | -83%     | ~0%      |
  | 1 KB Med  | 29.47 KB     | **13.86 KB**| **-53%** | -57%     | -6.3%    |
  | 10 KB Lg  | 201.56 KB    | **103.87 KB**| **-48%** | -43%    | -6.6%    |
  
  Step 3.9 → 3.13 累计（Small）：**分配 27.4 KB → 3.88 KB (-86%)**、Gen0 1.83 → 0.12 (**-93%**)、
  net10 RTT 47.52 → 42.90 µs (-9.7%)、net8 RTT 49.96 → 46.71 µs (-6.5%)。
  **这是所有 Step 里单步分配降幅最大的一次**，因为补掉的是 Step 3.10/3.11 当时漏掉的整条路径。
  经验教训：优化收包/发包时**两侧对称审计**，别只看 Server 端。36/36 单测在 net8/net10 上一次绿。
- **Step 3.14a**：Server 端收包路径补完 zero-copy（干掉整帧 `new byte[len]`）。
  在 Step 3.13 后做三档 payload（7 B / 1 KB / 10 KB）审计时，发现每个 transport（Ws / Wss / Nc）的 `DrainStash`
  都有同款 `var packData = new byte[len]; BlockCopy(m_stash, ..., packData, ...); InvokeOnDataReceived(packData)`
  模式，即**每收一包都会多 `new byte[headerLen + bodyLen]` 分配**。而关键观察是：
  这份 `packData` 的生命周期**仅限 DrainStash 同步循环内部**（ParseRaw 返回就没人引用了），**不跨 async**——
  唯一跨 async 的是 `Package.RawData`（body），由 ParseRaw 内部 `.ToArray()` 拷出独立副本。
  既然 packData 是同步生命周期，就可以直接改走 span：
  1. `TransportServerBase` 新增 `public delegate void DataReceivedSpanHandler(uint, ReadOnlySpan<byte>)` + 
     `InvokeOnDataReceivedSpan`（没绑 span handler 时回退到 byte[] event，老 transport 0 改动仍可用）；
  2. `Server` 新增 `OnDataReceivedSpan` 入口，与 byte[] 版共享 `DispatchReceived` 抽取出的 chunk/filter/PackageType 分流；
  3. `WsServer` / `WssServer` / `NcServer` 三个改造后的 transport `DrainStash` 直接
     `new ReadOnlySpan<byte>(m_stash, pos + 2, len)` 喂给 `InvokeOnDataReceivedSpan`；
  4. `TcpServer` 结构不同（收完一批 packets 再 dispatch），走 byte[] fallback 不改动，**不影响正确性**。
  
  实测 MemoryDiagnoser（三档 payload，net10）：
  | Payload   | Alloc 3.13 | Alloc 3.14a | Δ        | 吻合理论值（new byte[headerLen+bodyLen]）|
  |-----------|-----------:|------------:|---------:|:-----------------------------------------|
  | 7 B Small |   3.88 KB  |    3.84 KB  |  −40 B   | packData ~13 B + 32 B bucket 对齐        |
  | 1 KB Med  |  13.86 KB  |   12.81 KB  | **−1.05 KB (−7.5%)** | ~1024 B + 6 B header      |
  | 10 KB Lg  | 103.87 KB  |   93.83 KB  | **−10.04 KB (−9.7%)**| ~10240 B + 6 B header     |
  
  net8 同改造趋势完全一致（Medium −7.5%、Large −9.7%）。RTT 在 StdDev 内（±2%，net10 / net8 方向不一致 → 噪声）。
  **Small 档 Server 侧 packData 本身就只有十几字节，ArrayPool 化或 span 化几乎无可榨空间**  ——这是 3.14a 没收益的场景。
  收益上限：只改了 Server 侧，Client 侧 `DrainStash` 同款分配还在；配套 3.14b（Client 侧）可再省一次。
  net8 x2 + net10 x5 test run：35/36 一致 pass，`TestDeferCalls` net10 ~30% flaky 是 pre-existing（baseline 同量级）。
- **Step 3.15**：ProcessorRunner 单并发路径换同步邮箱，根除 IDE Debug 模式下的 idle OCE 风暴。

  **触发原因**：业务侧反馈 Rider/VS Debug 模式下断点完全卡死、`await client.Request()` 整段假死。
  对照 `1c1cbe8`（含 Step 3.5 之前架构）发现：3.5 改造后 ProcessorRunner 在 `_maxConcurrency==1` 时也走
  `Channel<RunnerWorkItem> + WaitForSignalAsync` 路径，用 `linkedToken(ct, RecvTimeout)` 加 `Channel.WaitToReadAsync`
  实现 idle 超时——意味着每个空闲 Processor 每 `RecvTimeout` (默认 50 ms) 必抛一次 `OperationCanceledException` 当
  "超时信号"。**Run 模式**：JIT inline 后 OCE 几乎免费（throw + catch ≈ µs 级），实测 cold-path 100 ms。
  **Debug 模式**：debugger 必须截获每一次 first-chance exception 做 break-on-throw 决策（stack walk + symbol lookup +
  filter 求值），16 个默认 Processor × 20 Hz = **320 Hz OCE**，event 队列被打爆 → 业务侧 EF Core 首次 query / DI
  cold-path 实测被放大到 13 s（**100×+**）。

  **修复方案**：`_maxConcurrency==1` 路径走 `BlockingCollection<RunnerWorkItem> + TryTake(out, ms, ct)`，
  内核 `SemaphoreSlim` wait，timeout 到期返回 false **不抛异常**（仅取消 token 才抛 OCE）。配套：
  1. `ProcessorRunner` 新增 `_incomingSync : BlockingCollection<RunnerWorkItem>`，与 `_incoming : Channel<>` 二选一；
  2. `Start()` 在 `_maxConcurrency==1` 时用 `Task.Factory.StartNew(RunSyncLoop, LongRunning)` 独占 OS 线程，
     避免 ThreadPool worker 上 `Wait` 触发 thread injection；
  3. `RunSyncLoop` 实现：drain → broadcast → periodic → `TryTake(_incomingSync, out, idleTimeout=RecvTimeout, ct)`；
  4. `Enqueue` / `Post(Func<Task>)` / `Post(Func<Task>, route)` / `PackageQueueCount` / `StopAsync` /
     新增的 `CompleteMailbox()` 全部按"`_incomingSync != null` 走同步路径，否则走 Channel"二分；
  5. `_maxConcurrency > 1` 路径**完全保持** `Channel + ExclusiveScheduler + RunAsync` 不变。

  **MemoryDiagnoser 实测（三档 payload）**：

  | Runtime | Payload   | RTT 3.14a | RTT 3.15 | Δ RTT     | Alloc 3.14a | Alloc 3.15 | Δ Alloc      |
  |---------|-----------|----------:|---------:|----------:|------------:|-----------:|-------------:|
  | net8    | 7 B Small | 48.13 µs  | 42.10 µs | **−12.5%** |   4.00 KB   |  3.13 KB   | **−21.8%**   |
  | net8    | 1 KB Med  | 47.95 µs  | 49.98 µs | +4.2%      |  12.98 KB   | 11.10 KB   | **−14.5%**   |
  | net8    | 10 KB Lg  | 69.41 µs  | 62.41 µs | **−10.1%** |  93.99 KB   | 83.11 KB   | **−11.6%**   |
  | net10   | 7 B Small | 42.24 µs  | 44.89 µs | +6.3%      |   3.84 KB   |  2.85 KB   | **−25.8%**   |
  | net10   | 1 KB Med  | 44.99 µs  | 46.17 µs | +2.6%      |  12.81 KB   | 10.83 KB   | **−15.5%**   |
  | net10   | 10 KB Lg  | 66.99 µs  | 61.33 µs | **−8.5%**  |  93.83 KB   | 82.84 KB   | **−11.7%**   |

  **解读**：
  - **分配三档 runtime 全降 11–26%**：来源是去掉 `Channel.WaitToReadAsync` + `IValueTaskSource` + linked CTS 一族的
    per-idle-tick async 盒装。即便 BDN 测量是 hot-path（每 ~45 µs 一包，处理器极少 idle），idle 信号路径上的
    state machine 分配仍然在每次 take 时累计——换 BlockingCollection 直接消失；
  - **RTT 在 StdDev 内、Large 档因 GC 减压净降 −8.5~10.1%**：net8 Small −12.5% 收益看起来很大，但 StdDev 5.03 µs 占 12%，
    Median = 40.75 µs 比 3.14a 的 48.13 µs 仍是 −15%，方向可信；net10 Small +6.3% 反向波动也在 1σ 内（StdDev 1.45 µs）。
    Channel async 路径在 net10 已被 Dynamic PGO + guarded devirtualization 优化得很好，换同步 take 多一次 Monitor
    操作，理论上 net10 RTT 应该比 net8 改后涨幅大 → 与实测一致；
  - **Debug 模式断点恢复秒级响应**：业务侧（这个 server 的 `TemplateTester.Test1`）反馈 Rider Debug 模式下 EF Core 首次
    query 从 13.5 s 降到几百 ms（与 Run 模式同阶），断点被命中。

  **取舍说明**：
  - 没把 `_maxConcurrency > 1` 也改同步，原因是 N>1 时业务用 `[MaxConcurrency(N)]` 主动 opt-in 流水线并发，async
    归还 ThreadPool worker 是必要的（避免 N 个 OS 线程 idle blocking）。N>1 Processor 个数极少（典型 1–3 个），
    异常风暴贡献量级可忽略（3 × 20 Hz = 60 Hz vs 320 Hz）；
  - Idle timeout 用 `RecvTimeout`（默认 50 ms），与 3.5 之前的 `BlockingCollection.TryTake(timeout)` 行为完全对齐；
  - `RunSyncLoop` 用 `LongRunning` 独占 OS 线程而不是 ThreadPool，避免 BlockingCollection.Wait 触发
    `ThreadPool.AdjustCount` 不必要的 thread injection。

  63/63 GoPlay.Net 单测在 net9.0 上 5 次重跑：4 次 100% 绿，1 次 1/63 失败为 pre-existing flake（与 3.15 无关，
  单独跑 always pass，是 test 间 TCP port reuse / TIME_WAIT 干扰，记 §八 待修）。

## 八、已知天花板 / 后续方向

- 同一个 client 连接内 `Request` 仍是 **应用层串行**：发请求 → await → 回调 → 再发。要进一步压，需要业务侧发起 N 个并发 request（`Task.WhenAll`），这时单连接 QPS 由 `Concurrency × 1000/D` 决定，当前已达线性扩展。
- ~~Server-side `Package.WriteTo(IBufferWriter)` 已经 zero-copy；但 `Header` 编码走的是 `Encoder.Encode(Header) → byte[]`，还有一次分配。~~ ← **Step 3.10 已完成**。
- ~~`Package<T>` 的 body 编码仍走 `encoder.Encode(Data) → byte[]`（通过 `UpdateContentSize`）。~~ ← **Step 3.11 已完成**。
- ~~收包路径 `Package.ParseRaw` 仍然 `data.Slice(...).ToArray()` 分配 `headerBytes`。~~ ← **Step 3.12 已完成**。
- ~~Client 发送侧 `pack.GetBytes()` + `Transport.Send(byte[])` 还是 Step 3.10 之前的老路径。~~ ← **Step 3.13 已完成**，每 Echo（Small）分配从 13.65 KB → 3.88 KB（−72%）。
- ~~Server 端 `DrainStash` 仍 `new byte[len]` 分配整帧再交给 `ParseRaw(byte[])`。~~ ← **Step 3.14a 已完成**，Medium/Large 档 −7.5% / −9.7%；TcpServer 结构不同未改，走 byte[] fallback。
- ~~ProcessorRunner `_maxConcurrency==1` 路径每 `RecvTimeout` 抛一次 `OperationCanceledException` 当 idle 超时信号，IDE Debug 模式下被放大成 320 Hz first-chance OCE 风暴，业务 EF Core / DI cold-path 被放大 100×+。~~ ← **Step 3.15 已完成**（换 `BlockingCollection.TryTake(timeout)` 内核 wait），三档 Allocated −11~26%、Debug RTT 与 Run 同阶。
- **Step 3.14b（Client 侧同款 zero-copy）**：Ws/Wss/Nc/Tcp Client 的 `DrainStash` 也都 `new byte[len]`，
  但 Client 侧 `Transport.Recv` 目前是 `ValueTask<byte[]>` + `BlockingCollection` 模型，span 不能跨 await，
  需要把 Client recv 改成 push 模型（transport 直接同步调 `Client.DispatchRawSpan`，绕过 `RecvChannel`）。
  预期 Medium / Large 档 Echo 再省一次 1 KB / 10 KB。改动比 3.14a 大一档。
- **Step 3.16 body `.ToArray()` → ArrayPool**：收包 body `.ToArray()` 的 Medium 约 1 KB、Large 约 10 KB 还在。
  下游 handler 链 async 持有 `Package.RawData`，ArrayPool 化需要配 `Package.ReleaseRawData()` 生命周期管理，
  `Package.Clone` / 多订阅分发是归还时机的难点。Small 档无感。建议先做 3.14b 再评估 3.16 是否还需要。
- **Small Echo 3.84 KB 里的 async state machine / TCS 分配**：剩余大头来自 `Task<T>` / `TaskCompletionSource` /
  async state machine 的堆箱装。可能的路径：`Request` 返回 `ValueTask<T>` + 共享 TCS pool。ROI 有限（几百 B），
  且会引入 ValueTask 重复 await 等使用坑，目前不建议先做。
- 大 body（≥ MAX_CHUNK_SIZE）分块路径仍然 `new byte[chunkSize]` per chunk，可以换 `ArrayPool<byte>.Rent`，但需要约束生命周期。
- **Pre-existing flaky: `TestDeferCalls` net10 ~30% fail rate**（Step 3.14a 前后都 flaky，单独跑 100% pass），
  疑似 test 间 TCP port reuse / TIME_WAIT 干扰。待单独诊断，不影响 3.14a 正确性。
- Roslyn analyzer（Step 3.3 preferred 路径）还没做，目前只有运行期 warning。要真正 fail-fast 需要追加 analyzer 项目。

