# GoPlay.Net Server 长连接框架 —— 官方性能基线

> 本文件固化当前优化后（Step 1~3）在 **Intel Core i9-14900K / Windows 11 / .NET 7** 上的基线数据。
> 作为后续回归的参考，数值显著下跌（> 15%）应当立即定位并修复。

---

## 一、环境

```
BenchmarkDotNet v0.13.3
OS           : Windows 11 (10.0.26200.8246)
CPU          : Intel Core i9-14900K, 1 CPU, 32 logical / 24 physical cores
.NET SDK     : 9.0.308
Host Runtime : .NET 7.0.20 (7.0.2024.26716), X64 RyuJIT AVX2
Target       : net7.0, Release, `--no-build`
```

复现命令：

```bash
# 在 Frameworks/Benchmark/ 下
dotnet build -c Release

# 快速对比表（Stopwatch，~30s，用于回归扫描）
dotnet run -c Release --no-build -- report

# 官方 BDN 基线（生成 CI 置信区间）
dotnet run -c Release --no-build -- route        # Route 编译委托 vs 反射
dotnet run -c Release --no-build -- request      # 端到端空业务 Echo
dotnet run -c Release --no-build -- concurrency  # Concurrency × Delay 矩阵
```

---

## 二、Route 分发层 —— `BenchmarkRoute`

压 Route 分发的纯同步部分（零 I/O，零等待），用于观察启动期反射 vs 运行期编译委托的代价。

| Method      | Mean         | Error       | StdDev      | Ratio  |
|-------------|-------------:|------------:|------------:|-------:|
| CreateRoute | 172,075.3 ns | 1,598.52 ns | 1,417.05 ns | 877.35 |
| InvokeRoute | **194.6 ns** |     3.79 ns |     4.92 ns |   1.00 |

- `CreateRoute`：包含反射解析 `[Request]` 标注 + 构建 `ExpressionUtil` 编译委托，一次性启动成本。
- `InvokeRoute`：**热路径基线**，编译委托调用 Echo 全链路 ~195 ns。

---

## 三、端到端单次请求 —— `BenchmarkRequest`

同机 loopback，1 个 `NcClient` ↔ 1 个 `NcServer`，业务是立即返回的 Echo：

| 阶段               | Mean         | 备注                                       |
|-------------------|-------------:|-------------------------------------------|
| Step 3.4 基线       | 69.08 µs     | Client.SendLoop 仍是 `BlockingCollection` + `.Wait()` |
| **Step 3.5 当前**   | **51.86 µs** | Client 换 `Channel<Package>` + `async await`，**-25%** |

即单连接同步 RTT ~52 µs（约 19.3 kreq/s 串行上限）。
进一步压榨需要改造 client → server 的握手/分包层面，或做 pipelining（等下一轮）。

---

## 四、Concurrency × Delay 矩阵 —— `ConcurrencyReport`

这是 Step 1~2 核心优化的直接对照实验：业务带 `await Task.Delay(D)` 时，
旧架构（`defaultConcurrency = 1`，串行）与新架构（`[MaxConcurrency(64)]`）的吞吐差。

**单 client，`WhenAll` 并发发 `BatchSize` 条请求，重复 `Rounds` 轮，记均值：**

| Delay(ms) | Concurrency | BatchSize | Wall(ms) | Throughput(req/s) | Speedup |
|----------:|------------:|----------:|---------:|------------------:|--------:|
|         0 |           1 |       200 |     2.02 |             99246 |   1.00x |
|         0 |          64 |       200 |     1.31 |        **152655** |   1.54x |
|        10 |           1 |       100 |  1645.26 |                61 |   1.00x |
|        10 |          64 |       100 |    32.16 |          **3110** |  51.16x |
|        50 |           1 |        50 |  2907.33 |                17 |   1.00x |
|        50 |          64 |        50 |    53.85 |           **929** |  53.99x |
|       100 |           1 |        50 |  5387.95 |                 9 |   1.00x |
|       100 |          64 |        50 |   113.73 |           **440** |  47.38x |

解读：

- `delay=0`：纯同步 Echo，新架构通过 Channel + 编译委托仍有 1.5× 提升，与全局 send 队列去除有关。
- `delay≥10ms`：业务 await 异步 I/O 时，旧架构每条消息独占 "虚线程" 整段 D ms，
  理论上限 `1000/D req/s`；新架构达 `Concurrency × 1000/D req/s`。实测 47–54× 提升。

---

## 五、方法级 `[MaxConcurrency]`

同一 `MethodLimitedProcessor`（class 标 `[MaxConcurrency(64)]`）下：

- `limited.slow`：**未标** method 级，吃满 processor 64 预算。
- `limited.slow8`：**标** `[MaxConcurrency(8)]`，方法级闸门压住并发到 8。

| Delay(ms) | Route          | Batch | Wall(ms) | Throughput(req/s) | ConcurrencyCap |
|----------:|:---------------|------:|---------:|------------------:|---------------:|
|        10 | limited.slow   |    64 |    17.27 |          **3706** |      64 (proc) |
|        10 | limited.slow8  |    64 |   125.88 |               508 |     8 (method) |
|        50 | limited.slow   |    64 |    59.20 |          **1081** |      64 (proc) |
|        50 | limited.slow8  |    64 |   447.69 |               143 |     8 (method) |
|       100 | limited.slow   |    64 |   104.84 |           **610** |      64 (proc) |
|       100 | limited.slow8  |    64 |   854.74 |                75 |     8 (method) |

`slow / slow8` QPS 比约 7.3~8，与 `64 / 8 = 8` 相符（偏差来自 warmup、GC、scheduler 抖动）。
方法级 `[MaxConcurrency]` 按预期工作。

---

## 六、回归判定红线

下述任一条件下跌即视为性能回归：

| 指标                                         | 本基线       | 红线（下跌超过） |
|---------------------------------------------|-------------:|----------------:|
| `InvokeRoute` mean                           | 194.6 ns     | +15% (>223 ns)  |
| `Echo` single-RTT mean                       | **51.86 µs** | +15% (>60 µs)   |
| `delay=10ms` × `concurrency=64` QPS          | 3110         | -15% (<2644)    |
| `delay=50ms` × `concurrency=64` QPS          |  929         | -15% (<790)     |
| `delay=100ms` × `concurrency=64` QPS         |  440         | -15% (<374)     |
| `method [MaxConcurrency(8)]` QPS / 64-ratio | ≈ 8×         | 偏离 >1.5×       |

> `delay=0` 场景下 wall time 只有 1–5 ms，测量方差很大（QPS 在 40k–150k 之间跳），不作为回归判据。

---

## 七、已做的优化（Step 概览）

- **Step 1**：Processor 执行从反射 + `BlockingCollection` + `Task.Wait` 改为编译委托 + `Channel<Package>` + async，严格遵守"一 Processor 一虚拟线程"。
- **Step 2**：删除 Server 端全局 `m_sendQueue`/`m_sendTask`，改为 per-session `SessionSender`（`Channel<Package>` + 小包聚合 + zero-copy `IBufferWriter`）。收包侧改 `ArrayPool<byte>` 环形 stash，删除所有 `MemoryStream`/`ToArray()`。
- **Step 3.1**：`NcClient` / `WsClient` / `WssClient` 收包路径同步改造，`TcpClient.Send/Recv` 去掉 `MemoryStream` 双拷贝。
- **Step 3.2**：`Server(defaultConcurrency=0|负值)` 改为 `Environment.ProcessorCount` 自动推导。
- **Step 3.3**：启动期 lint 检测 `[MaxConcurrency]` 误用（标在非 `[Request]/[Notify]` 方法上、冗余写 `1` 等）。
- **Step 3.4**：固化 BDN 官方基线到本文档。
- **Step 3.5**：`Client.SendLoop` 改 `Channel<Package>` + `async Task` pipeline，单连接同步 RTT 从 69 µs → 52 µs。

## 八、已知天花板 / 后续方向

- 同一个 client 连接内 `Request` 仍是 **应用层串行**：发请求 → await → 回调 → 再发。要进一步压，需要业务侧发起 N 个并发 request（`Task.WhenAll`），这时单连接 QPS 由 `Concurrency × 1000/D` 决定，当前已达线性扩展。
- Server-side `Package.WriteTo(IBufferWriter)` 已经 zero-copy；但 `Header` 编码走的是 `Encoder.Encode(Header) → byte[]`，还有一次分配。后续可评估直接 span-based encode。
- Roslyn analyzer（Step 3.3 preferred 路径）还没做，目前只有运行期 warning。要真正 fail-fast 需要追加 analyzer 项目。

