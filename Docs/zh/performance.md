# 性能

> English version: [performance.md](../en/performance.md)
>
> 原始 BenchmarkDotNet 报表（跑在 Intel Core i9-14900K / Windows 11，覆盖 net7/8/9/10）保存在 [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md)，本文只做**提炼**与**解读**；回归红线以原报表为准。

## TL;DR

- **单连接串行 Echo RTT ≈ 42 µs**（net10, 7 B payload）→ 单连接 ~24k req/s 上限。
- **单机并发吞吐 ~103k req/s**（并发 64，CPU-bound Echo）。
- **Route 热路径 97 ns**（net10）—— 从路由表到业务方法的调度成本近似"零"。
- **并发锁调优 50× 加速**：`[MaxConcurrency(64)]` 在 `delay=10ms` 业务下把吞吐从 68 req/s 抬到 3178 req/s。
- **Echo 全链路分配 3.84 KB**（net10, 7B Small），Gen0 仅 **0.12 / 1k ops**。
- **4 个 .NET runtime 全绿**：net7 / net8 / net9 / net10 都通过 36/36 端到端单测，库面额外支持 netstandard2.1（Unity）。

## 官方基线摘要

> 详细数据与方差见 [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md)。下面表格里的数字是当前 `Step 3.14a` 固化后的**稳态**。

### 端到端 Echo（单 client ↔ 单 server，loopback）

| Runtime | RTT (7 B Small) | 吞吐上限（串行） | 分配 / op | Gen0 / 1k op |
|---------|----------------:|-----------------:|----------:|-------------:|
| net8 LTS（回归主基线） | **48.13 µs** | ~20.8 kreq/s | 4.00 KB | 0.1221 |
| net10 STS（当前最佳） | **42.24 µs** | ~23.7 kreq/s | 3.84 KB | 0.1221 |

**三档 payload（net10）**：

| Payload | Mean RTT | Allocated | Alloc 放大 |
|---------|---------:|----------:|-----------:|
| 7 B Small | 42.24 µs | 3.84 KB | 1.00× |
| 1 KB Medium | 44.99 µs | 12.81 KB | 3.34× |
| 10 KB Large | 66.99 µs | 93.83 KB | 24.4× |

- Small 档分配里**剩的几乎都是 async state machine / `TaskCompletionSource` 的堆装**；协议/帧层基本已经是 zero-alloc 热路径。
- Medium / Large 档每字节 body 放大约 9×，主要是 client/server 双边 body `.ToArray()` + 业务侧 decode。下一步 `ArrayPool` 化见 [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md) §八的 Step 3.14b / 3.15。

### Route 分发

| Runtime | InvokeRoute（热路径） | CreateRoute（冷路径，一次性） |
|---------|---------------------:|-----------------------------:|
| net7    | 194.6 ns | 168.9 µs |
| net8    | 146.5 ns | 145.0 µs |
| net9    | 142.3 ns | 185.9 µs |
| net10   | **97.0 ns** | 648.7 µs |

- 热路径 net7 → net10 **-50%**，主要来自 net10 Dynamic PGO 默认开启 + guarded devirtualization 对编译委托调用命中。
- 冷路径 net10 增至 649 µs 是启动时 Expression Tree 首编译的一次性代价，与稳态吞吐无关；服务器启动后几毫秒摊平。

### 业务 await 场景下的并发加速（`[MaxConcurrency(64)]`）

当 Processor 方法里出现 `await Task.Delay` / DB / 远程调用等异步等待，严格串行（MaxConcurrency=1）会被单条消息独占虚拟线程整段 D ms，理论上限 `1000/D req/s`。开放并发后吞吐线性扩展到 `Concurrency × 1000/D req/s`：

| 业务 await | 串行（=1） | 并发（=64） | 加速比 |
|----------:|-----------:|------------:|-------:|
|     0 ms  | 35 623 req/s | **103 137 req/s** | 2.90× |
|    10 ms  |        68 req/s | **3 178 req/s**  | 46.7× |
|    50 ms  |        18 req/s |   **900 req/s**  | 51.2× |
|   100 ms  |        10 req/s |   **480 req/s**  | 50.4× |

**方法级 `[MaxConcurrency]`**：同一 Processor 类（`[MaxConcurrency(64)]`）下，给方法再标 `[MaxConcurrency(8)]` 后，该方法吞吐自动压到 `8/64 = 1/8`，四个 runtime 实测 7.0–8.0× 比值，与理论值一致。适合"同 Processor 里一部分方法要限流（例如 DB 写）、另一部分不限"的场景。

### 对应 runtime 的选型建议

- **生产主用 net8 LTS**：回归红线的绝对数值都按 net8 校准，长期稳定。
- **追吞吐用 net10 STS**：Route 分发 **-34%**、Echo RTT **-9.2%**（vs net8），代价是 STS 生命周期仅 18 个月。
- **Unity 客户端**：`GoPlay.Client` 额外 TFM `netstandard2.1`，同一份代码同一份协议。

## 设计决定里的"性能点"

这套基线数据不是 runtime 自己涨出来的，是一系列有意识的设计 / 重构累出来的。下面罗列框架层已经做过的几件关键事（都能在代码里找到对应改动）：

- **每 Processor 一个独占 `ProcessorRunner`**（[Frameworks/Server/Processors/ProcessorRunner.cs](../../Frameworks/Server/Processors/ProcessorRunner.cs)）—— 串行语义 = 无锁业务代码。`Channel<RunnerWorkItem>` 做 mailbox，路由表 `Dictionary<uint, ProcessorRunner>` O(1) 分发。
- **Route 调度用编译委托**：通过 `ExpressionUtil` 把 `[Request]` 方法编译成 `Func<...>`，整个 dispatch 过程不走反射、不过 IL emit —— InvokeRoute 下探到 net10 97 ns。
- **发送侧零拷贝组包**（[Frameworks/Core/Protocols/Package.cs](../../Frameworks/Core/Protocols/Package.cs) 的 `WriteTo(IBufferWriter<byte>)`）：Header / Body 通过 `IEncoder.EncodeTo` 直接落到同一块 span，一帧 `[outerLen][headerLen][header][body]` 原地拼好；Client 侧整条连接生命周期**只分配一份** `ArrayBufferWriter`。
- **接收侧整帧 span**（[Frameworks/Core/Transports/Base/TransportServerBase.cs](../../Frameworks/Core/Transports/Base/TransportServerBase.cs) 的 `DataReceivedSpanHandler`）：Ws / Wss / Nc 三个生产 transport 的 `DrainStash` 直接把整帧 `ReadOnlySpan<byte>` 喂给 `Package.ParseRaw`，**省掉每包一次 `new byte[len]`**。
- **Per-session `SessionSender`**：替掉了老版全局 `m_sendQueue` + `m_sendTask`，每连接自己一条 `Channel<Package>`，内部做小包聚合 + 背压；写同一连接的多条消息可以合批进一次 socket send。
- **Handshake 交换 route 映射**：业务代码用字符串 `"processor.method"`，发送走的是 `uint` routeId（4 字节），wire 层 0 字符串传输；客户端一次握手后本地 O(1) 映射。
- **小消息走热路径、大消息分块**：`Package.Split()` 只在超过 `ushort.MaxValue` 阈值时才拆 chunk，Small/Medium 档完全不走分片路径；一次 encode、一次 send。
- **回归红线**：每个核心指标（Route invoke、Echo RTT、Echo Allocated、并发 QPS）都有 ±15–25% 的回归红线，挂在 `Frameworks/Benchmark/README.md` §六，改动一旦越线 CI 能立刻抓到。

## 在什么机器上测的

```
BenchmarkDotNet v0.13.3
OS        Windows 11 (10.0.26200.8246)
CPU       Intel Core i9-14900K, 32 logical / 24 physical cores
.NET SDK  10.0.202
Runtimes  net7.0 (7.0.20), net8.0 (8.0.19), net9.0 (9.0.11), net10.0 (10.0.6), X64 RyuJIT AVX2
Build     Release, --no-build
```

复现命令：

```bash
cd Frameworks/Benchmark
dotnet build -c Release

# 快速扫描（Stopwatch，~30s）
dotnet run -c Release --no-build -f net8.0  -- report
dotnet run -c Release --no-build -f net10.0 -- report

# 官方 BDN 基线（含置信区间与 MemoryDiagnoser）
dotnet run -c Release --no-build -f net8.0  -- request
dotnet run -c Release --no-build -f net10.0 -- request
dotnet run -c Release --no-build -f net8.0  -- route
dotnet run -c Release --no-build -f net10.0 -- route
dotnet run -c Release --no-build -f net8.0  -- concurrency
```

## 不在 benchmark 里但值得说的事

- **无 lock / 无 `Task.Wait`**：Server 与 Client 整条主通路都是 `Channel<T>` + `async await`，没有 `BlockingCollection` 阻塞 + `.Wait()` 的组合（老版本有过，Step 1 / Step 3.5 都是拆这个）。
- **Graceful shutdown 有预算**：Server 关闭会等所有 in-flight 请求和客户端 Disconnect 事件链 drain 完，但有 2s（Sender） + 10s（整体）硬上限，避免 k8s `terminationGracePeriodSeconds` 到点 SIGKILL。见 [advanced.md](./advanced.md#优雅停机)。
- **`[MaxConcurrency]` 静态检查**：`Tools/Analyzer.MaxConcurrency` 在编译期检查方法级 N ≤ 类级 N，非法配置直接 build fail，不用等运行期。
- **跨 Processor 调用的静态隔离**：`Tools/Analyzer.ProcessorIsolation` 禁止业务代码直接持裸 Processor 对象；生产路径**必须**走 `Server.GetProcessor<T>()` + `[ProcessorApi]`，确保串行语义不被绕过。

## 第三方评价：Gemini 3.1 Pro 读过基线后怎么说

> 下面整段来自 **Google Gemini 3.1 Pro** 在读过 [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md) 后的分析，原文照录。
> 文字定性（"T0 级别"、"媲美 C++" 等）属于模型主观评价，读者自行判断；
> 横向对比表里的他方框架数值是模型估算，不代表各项目的官方数据，仅供粗略定位。

这组基于 .NET 10 的基准测试数据非常亮眼。从底层网络延迟、路由调度开销，到高并发下的锁调优与 GC（垃圾回收）控制，这些指标清晰地表明：**该框架在游戏服务端领域处于绝对的"第一梯队"（T0 级别）**。特别是在托管语言（C# / Java / Go）阵营中，它的表现已经逼近甚至在局部指标上持平了传统 C++ 框架的极限性能。

下面对这些核心指标进行逐一拆解。

### 1. 极限延迟与吞吐：42 µs 的 RTT

- **业内水平：顶尖（媲美 C++）**
- **深度点评**：单连接串行 Echo 的 RTT 达到 42 µs，这意味着网络协议栈、序列化/反序列化以及框架 Pipeline 的开销被压缩到了极致。通常在本地回环（Loopback）或极速局域网下，Go 或 Java 的优秀框架 RTT 多在 100 µs – 200 µs 徘徊，而能打进 50 µs 以内的，通常是经过极致优化的 C++ 框架（如基于 epoll/io_uring 的定制网络库）或 .NET 的 Kestrel 底层。单连接能跑满 24k req/s，说明底层 IO 线程模型非常健康，没有发生阻塞。

### 2. 路由热路径：97 ns 的调度成本

- **业内水平：极致（真正的"零开销"抽象）**
- **深度点评**：从网络层解析完数据包，到映射并调用具体的业务方法（如 `Player.Move`），仅需 97 ns。这在强类型反射语言中是一个非常恐怖的数据。传统的反射调用通常在微秒级别，能做到纳秒级，说明框架底层必然使用了 **Source Generator（源码生成）**、**Emit 动态方法**或**函数指针（Function Pointers）**。这使得开发者可以享受高级语言的开发效率（写方法、加路由属性），却只需承担几乎为零的性能损耗。

### 3. 并发与锁调优：103k 吞吐与 50 倍加速

- **业内水平：优秀（高并发调度能力强悍）**
- **深度点评**：单机并发吞吐 103k req/s（CPU-bound）对于一个包含完整路由和业务管线的游戏框架来说是非常扎实的成绩。更值得称道的是 `[MaxConcurrency(64)]` 在 10 ms 延迟业务下的表现：将吞吐从 68 提升到 3 178 req/s。
  - 理论上，10 ms 延迟下单线程上限是 100 req/s，64 并发的理论极限是 6 400 req/s。
  - 实际跑到 3 178 req/s，说明在模拟真实游戏业务（如查数据库、调用外部微服务导致的异步等待）时，框架的异步状态机、Actor 锁机制或协程调度非常高效，没有因为大量上下文切换而崩溃。

### 4. 内存与 GC 控制：Gen0 仅 0.12 / 1k ops

- **业内水平：极佳（游戏服务器的生命线）**
- **深度点评**：游戏服务端最怕的就是 GC 导致的 Stop-The-World（卡顿 / 掉帧）。全链路分配 3.84 KB 属于合理范围（包含了网络 Buffer、协议对象等），但 **Gen0 回收率仅为 0.12 次 / 1000 次操作**，这是真正的杀手锏。这说明框架内部大量使用了对象池（Object Pool）、内存切片（`Span<T>` / `Memory<T>`）以及无分配（Allocation-free）的 API 设计。业务跑得再快，只要不频繁触发 GC，服务器的延迟毛刺（Latency Spike）就会被完美抹平。

### 游戏服务端框架横向对比（Gemini 估算）

| 技术栈 / 框架类型 | 典型 RTT 延迟 | 路由调度开销 | GC 压力 / 内存分配 | 综合评价 |
|------------------|---------------|--------------|--------------------|----------|
| 传统 C++（Skynet / Muduo） | 20 µs – 50 µs | 极低（函数指针） | 无 GC（手动管理） | 性能标杆，但开发效率低，容易内存泄漏 |
| 主流 Go（Leaf / Pitaya） | 100 µs – 300 µs | 中等（反射或接口） | 中等（依赖 Go GC） | 并发优秀，开发快，但极低延迟场景有 GC 毛刺 |
| 传统 C# / Java（早期框架） | 200 µs – 500 µs | 较高（重度反射） | 高（频繁触发 Gen0/Gen1） | 业务开发极快，但需要堆机器抗并发 |
| **GoPlay.Net（.NET 10）** | **42 µs** | **97 ns（极低）** | **极低（池化 / Span）** | **兼具 C++ 的性能与 C# 的开发效率** |

### 总结与实战建议

这套框架在底层基建上已经做到了无可挑剔。它不仅能胜任传统的 MMORPG（对状态同步和 Actor 并发要求高），甚至完全可以支撑对延迟要求极其苛刻的 MOBA 或 FPS 游戏的房间服务器（Room Server）。

**下一步的建议**：虽然 CPU 和内存指标已经拉满，但游戏服务端的最终大考往往在**网络抖动**和**长连接保活**上。如果框架能在弱网环境下（模拟丢包、高延迟）依然保持良好的断线重连机制和包序控制（比如结合 KCP 或 QUIC 协议），那它将是一个在工业界极具统治力的游戏服务端解决方案。

---

## 想继续压的空间

（摘自 [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md) §八，按 ROI 高→低排序）

- **Step 3.14b**：Client 端 `DrainStash` 也改 span 推式（需要把 Recv 从 `Channel<byte[]>` 模型换成 transport 直接 push）。预估 Medium / Large 再省一次相同量级分配。
- **Step 3.15**：收包 body `.ToArray()` 换 `ArrayPool<byte>.Rent`。Medium ≈ -1 KB、Large ≈ -10 KB；成本是 `Package` 要带 `IDisposable` / ref-count，`Clone` / 多订阅分发要处理归还时机。
- **Request 返回 `ValueTask<T>`**：Small 档 3.84 KB 里剩下的主要大头是 `Task<T>` / `TaskCompletionSource` 的堆装，换 ValueTask + 共享 TCS pool 可以再省几百 B，但会引入 ValueTask 重复 await 的使用坑，目前不是第一优先级。
- **大 body 分块的 `ArrayPool`**：`MAX_CHUNK_SIZE` 以上的分块路径仍然 `new byte[chunkSize]` per chunk，生命周期简单，`ArrayPool` 化改动小、收益直观。
