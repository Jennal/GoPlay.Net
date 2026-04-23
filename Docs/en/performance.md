# Performance

> 中文版: [performance.md](../zh/performance.md)
>
> The raw BenchmarkDotNet report (run on Intel Core i9-14900K / Windows 11, covering net7 / 8 / 9 / 10) lives in [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md). This page only **distills** and **interprets** those numbers; the regression tripwires stay in the raw report.

## TL;DR

- **Single-connection serial Echo RTT ≈ 42 µs** (net10, 7 B payload) → ~24k req/s per connection ceiling.
- **Single-box concurrent throughput ~103k req/s** (concurrency 64, CPU-bound Echo).
- **Route hot path 97 ns** (net10) - near-zero dispatch from route table to business method.
- **Concurrency knob = 50× speedup**: `[MaxConcurrency(64)]` lifts throughput from 68 req/s to 3 178 req/s for a `delay=10ms` workload.
- **3.84 KB allocated per end-to-end Echo RTT** (net10, Small), Gen0 only **0.12 / 1k ops**.
- **All four .NET runtimes green**: net7 / net8 / net9 / net10 pass 36/36 end-to-end tests; libraries also target `netstandard2.1` (Unity).

## Official Baseline Summary

> Full numbers with variance in [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md). The numbers below reflect the current steady state after `Step 3.14a`.

### End-to-end Echo (single client ↔ single server, loopback)

| Runtime | RTT (7 B Small) | Serial Throughput Ceiling | Alloc / op | Gen0 / 1k op |
|---------|----------------:|--------------------------:|-----------:|-------------:|
| net8 LTS (regression baseline) | **48.13 µs** | ~20.8 kreq/s | 4.00 KB | 0.1221 |
| net10 STS (current best) | **42.24 µs** | ~23.7 kreq/s | 3.84 KB | 0.1221 |

**Three payload tiers (net10)**:

| Payload | Mean RTT | Allocated | Alloc Ratio |
|---------|---------:|----------:|------------:|
| 7 B Small | 42.24 µs | 3.84 KB | 1.00× |
| 1 KB Medium | 44.99 µs | 12.81 KB | 3.34× |
| 10 KB Large | 66.99 µs | 93.83 KB | 24.4× |

- In the Small tier **almost all remaining allocation is async state machine / `TaskCompletionSource` boxing**; the protocol / framing layer is effectively a zero-alloc hot path now.
- Medium / Large tiers amplify at ~9× per body byte, dominated by client/server body `.ToArray()` plus user-side decode. `ArrayPool` migration (Step 3.14b / 3.15) is the next lever - see §VIII in [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md).

### Route Dispatch

| Runtime | InvokeRoute (hot path) | CreateRoute (cold path, one-off) |
|---------|-----------------------:|---------------------------------:|
| net7    | 194.6 ns | 168.9 µs |
| net8    | 146.5 ns | 145.0 µs |
| net9    | 142.3 ns | 185.9 µs |
| net10   | **97.0 ns** | 648.7 µs |

- Hot path net7 → net10 **-50%**, mostly from net10's default Dynamic PGO + guarded devirtualization landing on the compiled-delegate call sites.
- The 649 µs cold path on net10 is a one-time Expression Tree compilation cost at startup; it amortises in milliseconds and does not affect steady-state throughput.

### Concurrency Speedup under Async Business (`[MaxConcurrency(64)]`)

When a processor method does `await Task.Delay` / DB / remote calls, strict serial mode (MaxConcurrency=1) lets one message hog the virtual thread for the whole D ms, capping at `1000/D req/s`. Opening concurrency scales throughput linearly to `Concurrency × 1000/D req/s`:

| Business await | Serial (=1) | Concurrent (=64) | Speedup |
|---------------:|------------:|-----------------:|--------:|
|      0 ms | 35 623 req/s | **103 137 req/s** | 2.90× |
|     10 ms |        68 req/s | **3 178 req/s**  | 46.7× |
|     50 ms |        18 req/s |   **900 req/s**  | 51.2× |
|    100 ms |        10 req/s |   **480 req/s**  | 50.4× |

**Method-level `[MaxConcurrency]`**: inside a `[MaxConcurrency(64)]` processor, tagging a specific method with `[MaxConcurrency(8)]` shrinks that method's throughput to `8/64 = 1/8`. All four runtimes measure a 7.0–8.0× ratio, matching theory. Useful when "some methods in a processor need rate-limiting (e.g. DB writes), others do not".

### Runtime Selection

- **Production: net8 LTS**. Regression tripwires calibrated here; long-term stable.
- **Chasing throughput: net10 STS**. Route dispatch **-34%**, Echo RTT **-9.2%** vs net8; tradeoff is an 18-month STS lifecycle.
- **Unity clients**: `GoPlay.Client` adds `netstandard2.1` TFM; same code, same protocol.

## Design Decisions that Matter

These numbers didn't come from free runtime upgrades - they are the output of deliberate design choices. The most impactful ones in the code:

- **One dedicated `ProcessorRunner` per Processor** ([Frameworks/Server/Processors/ProcessorRunner.cs](../../Frameworks/Server/Processors/ProcessorRunner.cs)) - serial semantics mean lock-free business code. Mailbox is a `Channel<RunnerWorkItem>`; route table is `Dictionary<uint, ProcessorRunner>` with O(1) dispatch.
- **Compiled-delegate route dispatch**: `ExpressionUtil` compiles `[Request]` methods into a `Func<...>`; the whole dispatch avoids reflection and IL emit. InvokeRoute drops to 97 ns on net10.
- **Zero-copy send path** ([Frameworks/Core/Protocols/Package.cs](../../Frameworks/Core/Protocols/Package.cs)'s `WriteTo(IBufferWriter<byte>)`): both Header and Body use `IEncoder.EncodeTo` to write into the same span; `[outerLen][headerLen][header][body]` is composed in place. The client allocates its `ArrayBufferWriter` **once for the connection's lifetime**.
- **Whole-frame span on receive** ([Frameworks/Core/Transports/Base/TransportServerBase.cs](../../Frameworks/Core/Transports/Base/TransportServerBase.cs)'s `DataReceivedSpanHandler`): `DrainStash` in the Ws / Wss / Nc transports hands a `ReadOnlySpan<byte>` frame straight to `Package.ParseRaw`, **eliminating one `new byte[len]` per package**.
- **Per-session `SessionSender`**: replaced the legacy global `m_sendQueue` + `m_sendTask` with one `Channel<Package>` per connection, small-packet coalescing, and backpressure; multiple messages to the same connection can ride a single socket write.
- **Route map exchanged at handshake**: business code uses `"processor.method"` strings, the wire carries `uint` routeIds (4 bytes), zero string bytes on the wire; the client does an O(1) local lookup after the first handshake.
- **Small messages take the fast lane, large messages chunk**: `Package.Split()` only fragments above `ushort.MaxValue`; Small / Medium never touch the chunking path - one encode, one send.
- **Regression tripwires**: every core metric (Route invoke, Echo RTT, Echo allocated, concurrent QPS) has a ±15–25% tripwire, documented in `Frameworks/Benchmark/README.md` §VI. Any change that crosses the line surfaces immediately.

## Test Hardware

```
BenchmarkDotNet v0.13.3
OS        Windows 11 (10.0.26200.8246)
CPU       Intel Core i9-14900K, 32 logical / 24 physical cores
.NET SDK  10.0.202
Runtimes  net7.0 (7.0.20), net8.0 (8.0.19), net9.0 (9.0.11), net10.0 (10.0.6), X64 RyuJIT AVX2
Build     Release, --no-build
```

Reproduce:

```bash
cd Frameworks/Benchmark
dotnet build -c Release

# Quick scan (Stopwatch, ~30s)
dotnet run -c Release --no-build -f net8.0  -- report
dotnet run -c Release --no-build -f net10.0 -- report

# Official BDN baselines (confidence intervals + MemoryDiagnoser)
dotnet run -c Release --no-build -f net8.0  -- request
dotnet run -c Release --no-build -f net10.0 -- request
dotnet run -c Release --no-build -f net8.0  -- route
dotnet run -c Release --no-build -f net10.0 -- route
dotnet run -c Release --no-build -f net8.0  -- concurrency
```

## Things Not in the Benchmark but Worth Mentioning

- **No lock / no `Task.Wait`**: both Server and Client hot paths are `Channel<T>` + `async await`; no `BlockingCollection + .Wait()` anywhere (the legacy design had that; Step 1 and Step 3.5 removed it).
- **Graceful shutdown budget**: `Server.Stop()` drains in-flight requests and client-disconnect callbacks, but caps at 2s (sender) + 10s (overall) to beat k8s `terminationGracePeriodSeconds` SIGKILL. See [advanced.md](./advanced.md#graceful-shutdown).
- **`[MaxConcurrency]` static checks**: `Tools/Analyzer.MaxConcurrency` fails the build if method-level N > class-level N; no "works at runtime, fails at 2am".
- **Cross-processor isolation at compile time**: `Tools/Analyzer.ProcessorIsolation` forbids raw processor instances from leaking between processors; the production path **must** go through `Server.GetProcessor<T>()` + `[ProcessorApi]`, keeping serial semantics intact.

## Third-Party Review: What Gemini 3.1 Pro Said

> The section below is reproduced from **Google Gemini 3.1 Pro** after reading [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md).
> Qualitative labels ("T0 tier", "on par with C++", etc.) are the model's subjective take — use your own judgment.
> The numbers attributed to other frameworks in the comparison table are the model's ballpark estimates, not official figures from those projects; treat them as a rough positioning aid only.

The benchmark set on .NET 10 is striking. From raw network latency and route-dispatch overhead, through lock tuning under high concurrency and GC control, the indicators make one thing clear: **this framework sits squarely in the "T0 tier" of game server stacks**. Within the managed-language camp (C# / Java / Go) it has closed in on — and in several sub-metrics matched — the ceiling of highly tuned C++ frameworks.

A per-metric breakdown follows.

### 1. Latency and throughput ceiling: 42 µs RTT

- **Industry position: top tier (on par with C++).**
- **Analysis**: a 42 µs RTT for a single-connection serial Echo means the network stack, (de)serialization, and framework pipeline overhead have been squeezed to the limit. On loopback or a fast LAN, strong Go / Java frameworks usually land between 100 µs – 200 µs; sub-50 µs typically belongs to heavily optimized C++ stacks (custom epoll / io_uring libraries) or the guts of .NET's Kestrel. Saturating 24k req/s on a single connection shows the underlying IO threading model is healthy, with no blocking.

### 2. Route hot path: 97 ns per dispatch

- **Industry position: elite — a genuine "zero-overhead" abstraction.**
- **Analysis**: going from raw bytes off the wire to the actual business method (`Player.Move` and friends) in just 97 ns is a remarkable number for a strongly-typed reflection-friendly language. Traditional reflection lives in the microsecond range; hitting nanoseconds implies **Source Generators**, **emitted dynamic methods**, or **function pointers** under the hood. Developers keep the ergonomics of a high-level language (write a method, add a route attribute) while paying essentially zero dispatch tax.

### 3. Concurrency and lock tuning: 103k throughput and a 50× speedup

- **Industry position: strong — high-concurrency scheduling is rock solid.**
- **Analysis**: 103k req/s single-box concurrent throughput (CPU-bound) is very solid for a game framework that already includes the full route and business pipeline. Even more notable is how `[MaxConcurrency(64)]` behaves on a 10 ms-latency workload: throughput jumps from 68 to 3 178 req/s.
  - Theoretical single-thread ceiling at 10 ms latency is 100 req/s; the theoretical ceiling at 64-way concurrency is 6 400 req/s.
  - Achieving 3 178 req/s in practice shows that when simulating realistic game work (database calls, external microservice awaits, etc.), the async state machine, Actor locking, and scheduler hold up well under heavy context-switching.

### 4. Memory and GC control: Gen0 just 0.12 / 1k ops

- **Industry position: excellent — the lifeline of game servers.**
- **Analysis**: game servers fear nothing more than GC-induced Stop-The-World (stutter / frame drops). Total end-to-end allocation at 3.84 KB is reasonable (it includes network buffers and protocol objects), but the **Gen0 rate of only 0.12 collections per 1 000 operations** is the real killer feature. It points to heavy use of object pools, memory slicing (`Span<T>` / `Memory<T>`), and allocation-free API design. No matter how fast the business logic runs, if GC rarely fires, latency spikes melt away.

### Game server framework horizontal comparison (Gemini's estimate)

| Stack / framework type | Typical RTT | Route dispatch cost | GC pressure / allocation | Overall take |
|------------------------|-------------|---------------------|--------------------------|--------------|
| Traditional C++ (Skynet / Muduo) | 20 µs – 50 µs | Extremely low (function pointers) | No GC (manual) | Performance yardstick, but painful ergonomics and leak-prone |
| Mainstream Go (Leaf / Pitaya) | 100 µs – 300 µs | Medium (reflection or interfaces) | Medium (Go GC) | Great concurrency and DX, but GC jitter at ultra-low-latency scale |
| Classic C# / Java (older frameworks) | 200 µs – 500 µs | Higher (heavy reflection) | High (frequent Gen0 / Gen1) | Rapid business development, but you scale out with iron |
| **GoPlay.Net (.NET 10)** | **42 µs** | **97 ns (extremely low)** | **Extremely low (pooled / Span)** | **C++-class performance with C# developer ergonomics** |

### Summary and practical advice

On the infrastructure layer, this framework is essentially faultless. It can handle the traditional MMORPG workload (state sync and Actor concurrency heavy) and can also back latency-critical MOBA / FPS room servers.

**Next step**: CPU and memory indicators are already maxed out, but the final exam for a game server is typically **network jitter** and **long-lived-connection survivability**. If the framework holds up under lossy / high-latency conditions (combined with something like KCP or QUIC for reconnection and packet ordering), it becomes an industrially dominant solution for game servers.

---

## Headroom Left

(From [Frameworks/Benchmark/README.md](../../Frameworks/Benchmark/README.md) §VIII, ordered by ROI)

- **Step 3.14b**: push-based span on the client `DrainStash` (requires switching Recv from a `Channel<byte[]>` model to direct transport push). Expected to shave one more identical chunk of allocation for Medium / Large.
- **Step 3.15**: replace body `.ToArray()` with `ArrayPool<byte>.Rent`. Medium ≈ -1 KB, Large ≈ -10 KB; the cost is making `Package` `IDisposable` / ref-counted and handling return timing for `Clone` / multi-subscriber dispatch.
- **`ValueTask<T>` on `Request`**: the 3.84 KB remaining in the Small tier is mostly `Task<T>` / `TaskCompletionSource` boxing. Moving to `ValueTask` + a shared TCS pool can shave a few hundred bytes, but introduces `ValueTask` re-await foot-guns - not top priority yet.
- **`ArrayPool` for large-body chunks**: above `MAX_CHUNK_SIZE` the chunking path still allocates `new byte[chunkSize]` per chunk; lifetime is simple, so the `ArrayPool` swap is small and the payoff is direct.
