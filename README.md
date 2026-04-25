# GoPlay.Net

[![NuGet](https://img.shields.io/nuget/v/GoPlay.Server?label=GoPlay.Server)](https://www.nuget.org/packages/GoPlay.Server)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Client?label=GoPlay.Client)](https://www.nuget.org/packages/GoPlay.Client)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Tools?label=GoPlay.Tools)](https://www.nuget.org/packages/GoPlay.Tools)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Templates?label=GoPlay.Templates)](https://www.nuget.org/packages/GoPlay.Templates)

> 中文版: [README_zh.md](README_zh.md)

> **Write long-connection game servers like ASP.NET — slap an attribute on a function and clients call it like a local method.**

GoPlay.Net is a C# long-connection RPC framework for real-time game servers (and any app that needs push). It takes the route model from **Pomelo**, the Actor model from **ET / Orleans**, and the attribute-routing ergonomics from **ASP.NET Core**, and compresses them into a pragmatic C# stack with full client code generation.

The name "GoPlay" is historical: the first implementation was in Golang; the current production-quality port lives here in C#.

---

## Performance at a Glance

Numbers from the official BDN baseline (Intel Core i9-14900K / Windows 11, Step 3.15). Full report and regression tripwires in [Frameworks/Benchmark/README.md](Frameworks/Benchmark/README.md); distilled version in [Docs/en/02.performance.md](Docs/en/02.performance.md) · [Docs/zh/02.performance.md](Docs/zh/02.performance.md).

| Metric | net8 LTS | net10 STS |
|--------|---------:|----------:|
| Single-connection serial Echo RTT (7 B payload) | **42.10 µs** | 44.89 µs |
| Serial throughput ceiling (per connection) | **~23.8 kreq/s** | ~22.3 kreq/s |
| Route dispatch hot path (`InvokeRoute`) | 146.5 ns | **97.0 ns** |
| Allocation per end-to-end Echo RTT | 3.13 KB | **2.85 KB** |
| Gen0 / 1k ops | 0.1221 | **0.1221** |
| Concurrent Echo throughput (`MaxConcurrency=64`, CPU-bound) | — | **~103k req/s** |

- **Zero overhead under the IDE debugger**: Step 3.15 replaced the `Channel + WaitToReadAsync(linkedToken)` idle path on single-concurrency `ProcessorRunner`s with a `BlockingCollection.TryTake(timeout)` kernel wait. The old path threw 1 first-chance `OperationCanceledException` per `RecvTimeout` (50 ms) per processor — at the default 16 processors that's 320 Hz of OCE, which Rider/VS debuggers must intercept (stack walk + filter eval) and which can amplify cold-path business code (EF Core first query, DI first resolve, etc.) by 100×+ in **Debug** mode (measured 100 ms in Run vs 13 s in Debug). After 3.15, Debug RTT is on par with Run, and breakpoints respond in seconds, not minutes.
- **50× speedup** on delay-bound business via `[MaxConcurrency(N)]`: a `delay=10ms` workload jumps from 68 req/s (serial) to 3 178 req/s (N=64). See [02.performance.md](Docs/en/02.performance.md#concurrency-speedup-under-async-business-maxconcurrency64).
- **Zero-alloc hot path**: Header + Body both encode straight into the same `IBufferWriter<byte>` span; each connection allocates one `ArrayBufferWriter` for its entire lifetime.
- **O(1) route dispatch**: Routes are resolved to `uint` ids during handshake; runtime lookup is a single `Dictionary<uint, ProcessorRunner>` hop into a compiled delegate.
- **Compile-time safety, runtime speed**: Roslyn analyzers fail the build on illegal `[MaxConcurrency]` combos or cross-processor escape hatches; source generators emit `ProcessorRef<T>` extensions that read like local calls.
- **Four runtimes, one codebase**: net7 / net8 / net9 / net10 all pass 63/63 end-to-end tests; the client library additionally targets `netstandard2.1` for Unity.

> **Why net8 is now faster than net10 on serial RTT**: in Step 3.15 the `Channel` async signal path on hot processors was replaced with a synchronous `BlockingCollection` take. On net8 (no Dynamic PGO) this is a net win (−12.5% RTT, −22% allocation). On net10 the previous `Channel` path was already heavily PGO-optimized, so the synchronous take adds one Monitor.Wait/Pulse pair (+6.3% RTT) but still saves 26% allocation by removing the `IValueTaskSource` / linked-CTS boxing. Pick **net10** for lowest allocation / Gen0 pressure (and the fastest `InvokeRoute`); pick **net8 LTS** for lowest serial RTT and the longest support window.

### How it stacks up against other game-server stacks

> Third-party positioning by **Google Gemini 3.1 Pro** after reading the baseline. Numbers for the *other* stacks are Gemini's ballpark estimates, not official figures from those projects — treat the table as a rough radar chart, not a head-to-head benchmark.

| Stack / framework type | Typical RTT | Route dispatch cost | GC pressure / allocation | Overall take |
|------------------------|-------------|---------------------|--------------------------|--------------|
| Traditional C++ (Skynet / Muduo) | 20 µs – 50 µs | Extremely low (function pointers) | No GC (manual) | Performance yardstick, but painful ergonomics and leak-prone |
| Mainstream Go (Leaf / Pitaya) | 100 µs – 300 µs | Medium (reflection or interfaces) | Medium (Go GC) | Great concurrency and DX, but GC jitter at ultra-low-latency scale |
| Classic C# / Java (older frameworks) | 200 µs – 500 µs | Higher (heavy reflection) | High (frequent Gen0 / Gen1) | Rapid business development, but you scale out with iron |
| **GoPlay.Net (.NET 10)** | **42 µs** | **97 ns (extremely low)** | **Extremely low (pooled / Span)** | **C++-class performance with C# developer ergonomics** |

> Gemini's summary quote:
> *"Within the managed-language camp (C# / Java / Go) it has closed in on — and in several sub-metrics matched — the ceiling of highly tuned C++ frameworks."*
>
> Full per-metric breakdown: [Docs/en/02.performance.md#third-party-review-what-gemini-31-pro-said](Docs/en/02.performance.md#third-party-review-what-gemini-31-pro-said) · [Docs/zh/02.performance.md#第三方评价gemini-31-pro-读过基线后怎么说](Docs/zh/02.performance.md#第三方评价gemini-31-pro-读过基线后怎么说).

---

## Docs

- 中文文档入口：[Docs/zh](Docs/zh/01.getting-started.md)
- English docs: [Docs/en](Docs/en/01.getting-started.md)

| Topic | English | 中文 |
|-------|---------|------|
| Getting started | [en](Docs/en/01.getting-started.md) | [zh](Docs/zh/01.getting-started.md) |
| Performance | [en](Docs/en/02.performance.md) | [zh](Docs/zh/02.performance.md) |
| Core concepts | [en](Docs/en/03.concepts.md) | [zh](Docs/zh/03.concepts.md) |
| Processor-Actor model | [en](Docs/en/04.processor-model.md) | [zh](Docs/zh/04.processor-model.md) |
| Transport & Encoder | [en](Docs/en/05.transport-encoder.md) | [zh](Docs/zh/05.transport-encoder.md) |
| Tools & Code generation | [en](Docs/en/06.tools-codegen.md) | [zh](Docs/zh/06.tools-codegen.md) |
| Clients (C# / TS / JS) | [en](Docs/en/07.clients.md) | [zh](Docs/zh/07.clients.md) |
| Advanced topics | [en](Docs/en/08.advanced.md) | [zh](Docs/zh/08.advanced.md) |
| Repository structure | [en](Docs/en/09.structure.md) | [zh](Docs/zh/09.structure.md) |
| Wire protocol | [en](Docs/en/10.protocol.md) | [zh](Docs/zh/10.protocol.md) |

---

## Highlights

- **Attribute routing**: `[Processor("echo")]` + `[Request("request")]` → clients call `"echo.request"`. No boilerplate dispatcher.
- **Processor as Actor**: every processor owns a dedicated `ProcessorRunner` + FIFO mailbox; zero-lock business code by default, optional `[MaxConcurrency(N)]` when you need parallelism.
- **Safe cross-processor calls**: `Server.GetProcessor<T>()` + `[ProcessorApi]` + a Roslyn source generator → cross-processor invocation reads like a local method but runs on the target mailbox.
- **Pluggable transport**: TCP / NetCoreServer / WebSocket / Secure WebSocket out of the box; bring your own by implementing `TransportServerBase`.
- **Pluggable encoder**: Protobuf (default, zero-alloc hot path) and Json; swap by `EncodingType`.
- **Full client codegen**: the `goplay` CLI scans your processors and emits strongly-typed client extensions (C#, TypeScript, any target via [Liquid](https://shopify.github.io/liquid/) templates).
- **Multi-language clients**: C# (desktop + Unity), TypeScript (`goplay-ws`), vanilla JavaScript (build-free).
- **Testability**: end-to-end tests look like calling local methods (see [TestEcho.cs](ProjectTemplates/GoPlay.Tcp.Template/UnitTests/TestEcho.cs)).
- **Production details**: graceful shutdown with drain budget, request / heartbeat timeouts, Kick frames, broadcast with back-pressure, Roslyn analyzers that fail the build on unsafe cross-processor access.

---

## 30-Second Server

```csharp
[Processor("echo")]
public class EchoProcessor : ProcessorBase
{
    public override string[] Pushes => new[] { "echo.push" };

    [Request("request")]
    public PbString Request(Header header, PbString data)
        => new PbString { Value = $"Server reply: {data.Value}" };

    [Notify("notify")]
    public void Notify(Header header, PbString data)
        => Push("echo.push", header, new PbString { Value = $"Server push: {data.Value}" });
}

var server = new Server<NcServer>();
server.Register(new EchoProcessor());
server.Start("*", 8888);
```

## 30-Second Client

```csharp
var client = new Client<NcClient>();
await client.Connect("localhost", 8888);

// Strong-typed, generated from the server's [Request] signature
var (status, resp) = await client.Echo_Request(new PbString { Value = "Hello" });
// status.Code == StatusCode.Success
// resp.Value  == "Server reply: Hello"
```

Full walk-through: [Getting Started (EN)](Docs/en/01.getting-started.md) / [快速上手 (中文)](Docs/zh/01.getting-started.md).

---

## Install

**Fastest — templates**

```bash
dotnet new install GoPlay.Templates
dotnet new goplay-tcp -n MyGame      # or: dotnet new goplay-ws
```

**NuGet**

```bash
dotnet add package GoPlay.Server
dotnet add package GoPlay.Core.Transport.NetCoreServer   # or Transport.Ws / Transport.Wss
dotnet add package GoPlay.Client
dotnet tool install -g GoPlay.Tools                      # optional: goplay CLI
```

**TypeScript client**

```bash
npm install goplay-ws
```

## NuGet Packages

| Package | Purpose | Source |
|---------|---------|--------|
| [`GoPlay.Core`](https://www.nuget.org/packages/GoPlay.Core) | Protocol / Encoder / Transport base | [Frameworks/Core](Frameworks/Core) |
| [`GoPlay.Server`](https://www.nuget.org/packages/GoPlay.Server) | Server, ProcessorRunner, ProcessorRef | [Frameworks/Server](Frameworks/Server) |
| [`GoPlay.Client`](https://www.nuget.org/packages/GoPlay.Client) | C# client (net7+ and netstandard2.1 for Unity) | [Frameworks/Client](Frameworks/Client) |
| [`GoPlay.Core.Transport.NetCoreServer`](https://www.nuget.org/packages/GoPlay.Core.Transport.NetCoreServer) | TCP transport (recommended) | [Frameworks/Transport.NetCoreServer](Frameworks/Transport.NetCoreServer) |
| [`GoPlay.Core.Transport.Ws`](https://www.nuget.org/packages/GoPlay.Core.Transport.Ws) | WebSocket transport | [Frameworks/Transport.Ws](Frameworks/Transport.Ws) |
| [`GoPlay.Core.Transport.Wss`](https://www.nuget.org/packages/GoPlay.Core.Transport.Wss) | Secure WebSocket transport | [Frameworks/Transport.Wss](Frameworks/Transport.Wss) |
| [`GoPlay.Tools`](https://www.nuget.org/packages/GoPlay.Tools) | `goplay` CLI (extension/config/excel2proto) | [Tools/Main](Tools/Main) |
| [`GoPlay.Templates`](https://www.nuget.org/packages/GoPlay.Templates) | `dotnet new goplay-tcp / goplay-ws` | [ProjectTemplates](ProjectTemplates) |

---

## How It Compares

- **vs Pomelo**: same `processor.method` routing, Request / Notify / Push triad and route map in handshake — but in statically typed C#, with per-processor Actor isolation and client codegen.
- **vs ET**: both are C# with an Actor model; ET uses one Actor per Entity (usually per player), GoPlay uses one Actor per Processor (a feature module) — better for layered features like lobby / match / battle / DB.
- **vs ASP.NET Core**: same attribute-routed ergonomics, but long-connection with Push / Notify / Broadcast; processors are long-lived instances (not per-request DI scopes).
- **vs Orleans**: Processors are "long-lived strongly-typed grains with configurable per-Actor concurrency"; today single-node, cluster mode on the roadmap (`TO-DO.md`).

Full comparison: [03.concepts.md](Docs/en/03.concepts.md#comparison-with-related-frameworks).

---

## Repository Map

See [Docs/en/09.structure.md](Docs/en/09.structure.md) / [Docs/zh/09.structure.md](Docs/zh/09.structure.md). Short version:

```text
Frameworks/       C# framework body (Core / Server / Client / Transport*)
Clients/          TypeScript + pure-JS clients
Tools/            goplay CLI + Roslyn generator + analyzers
ProjectTemplates/ dotnet new goplay-tcp / goplay-ws templates
Docs/             en/ + zh/ topic docs (performance, concepts, protocol, ...)
```

---

## Thanks

Thanks to JetBrains for providing an open source license for GoPlay.Net.

[![Thanks to JetBrains to provide opensource license for GoPlay.Net](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://jb.gg/OpenSourceSupport)

## License

MIT. See [LICENSE](LICENSE).

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
