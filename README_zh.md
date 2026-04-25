# GoPlay.Net

[![NuGet](https://img.shields.io/nuget/v/GoPlay.Server?label=GoPlay.Server)](https://www.nuget.org/packages/GoPlay.Server)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Client?label=GoPlay.Client)](https://www.nuget.org/packages/GoPlay.Client)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Tools?label=GoPlay.Tools)](https://www.nuget.org/packages/GoPlay.Tools)
[![NuGet](https://img.shields.io/nuget/v/GoPlay.Templates?label=GoPlay.Templates)](https://www.nuget.org/packages/GoPlay.Templates)

> English version: [README.md](README.md)

> **像写 ASP.NET 一样写长连接游戏服务器 —— 给函数打个 Attribute，客户端就能像调本地方法一样调它。**

GoPlay.Net 是一个面向实时游戏服务器（以及任何需要主动推送的应用）的 C# 长连接 RPC 框架。它借鉴了 **Pomelo** 的 Route 模型、**ET / Orleans** 的 Actor 模型，以及 **ASP.NET Core** 的 Attribute 路由人体工学，压缩成一套务实的 C# 技术栈，并自带完整的客户端代码生成。

"GoPlay" 这个名字是历史遗留：最早的一版用 Golang 写的；目前生产级的移植版在这里、用 C#。

---

## 性能一眼看

数据来自官方 BDN 基线（Intel Core i9-14900K / Windows 11, Step 3.15）。完整报表与回归红线见 [Frameworks/Benchmark/README.md](Frameworks/Benchmark/README.md)；精简版见 [Docs/en/02.performance.md](Docs/en/02.performance.md) · [Docs/zh/02.performance.md](Docs/zh/02.performance.md)。

| 指标 | net8 LTS | net10 STS |
|------|---------:|----------:|
| 单连接串行 Echo RTT（7 B payload） | **42.10 µs** | 44.89 µs |
| 串行吞吐上限（单连接） | **~23.8 kreq/s** | ~22.3 kreq/s |
| Route 分发热路径（`InvokeRoute`） | 146.5 ns | **97.0 ns** |
| 端到端 Echo 每次分配 | 3.13 KB | **2.85 KB** |
| Gen0 / 1k ops | 0.1221 | **0.1221** |
| 并发 Echo 吞吐（`MaxConcurrency=64`，CPU-bound） | — | **~103k req/s** |

- **IDE Debug 模式零额外开销**：Step 3.15 把单并发 `ProcessorRunner` 的 idle 等待路径从 `Channel + WaitToReadAsync(linkedToken)` 换成 `BlockingCollection.TryTake(timeout)` 的内核 wait。旧路径每个 Processor 每 `RecvTimeout`（默认 50 ms）会抛一次 first-chance `OperationCanceledException` 当超时信号——按默认 16 个 Processor 算就是 320 Hz 的 OCE，Rider/VS debugger 必须截获每一个（stack walk + filter 求值），把业务侧 cold-path 代码（EF Core 首次 query、DI 首次解析等）在 **Debug** 模式下放大 100×+（实测 Run 100 ms / Debug 13 s）。3.15 之后 Debug RTT 与 Run 同阶，断点恢复秒级响应而不是几分钟。
- **延迟型业务 50× 加速**：靠 `[MaxConcurrency(N)]`，`delay=10ms` 的负载从串行 68 req/s 抬到 3 178 req/s（N=64）。见 [02.performance.md](Docs/zh/02.performance.md)。
- **热路径零分配**：Header + Body 都直接编码到同一个 `IBufferWriter<byte>` span 里；每条连接整个生命周期只分配一个 `ArrayBufferWriter`。
- **O(1) 路由分发**：Route 在握手时就解析成 `uint` id；运行时只是一次 `Dictionary<uint, ProcessorRunner>` 查表后进入编译好的 delegate。
- **编译期安全 + 运行期速度**：Roslyn 分析器在非法的 `[MaxConcurrency]` 组合或跨 Processor 逃逸上直接让编译失败；Source Generator 生成的 `ProcessorRef<T>` 扩展读起来就像本地调用。
- **四个运行时、一份代码**：net7 / net8 / net9 / net10 全部跑绿 63/63 端到端测试；客户端库额外支持 `netstandard2.1`（Unity）。

> **为什么 net8 现在串行 RTT 反超 net10**：Step 3.15 把 hot processor 的 `Channel` async 信号路径换成同步 `BlockingCollection` take。net8 没有 Dynamic PGO，是净收益（RTT −12.5%，分配 −22%）；net10 上原来的 `Channel` 路径已经被 PGO 深度优化，换同步 take 多了一对 Monitor.Wait/Pulse（RTT +6.3%），但仍省下 26% 分配（去掉 `IValueTaskSource` / linked-CTS 的盒装）。**选 net10**：分配 / Gen0 压力最低、`InvokeRoute` 最快；**选 net8 LTS**：串行 RTT 最低、生命周期最长。

### 和其他游戏服务器栈相比

> 以下定位由 **Google Gemini 3.1 Pro** 读过基线后给出。*其他*栈的数字是 Gemini 的粗估，不是各项目的官方数据 —— 这张表只当雷达图看，不是正面对撞的 benchmark。

| 栈 / 框架类型 | 典型 RTT | Route 分发代价 | GC 压力 / 分配 | 总体评价 |
|----------------|----------|-----------------|------------------|----------|
| 传统 C++（Skynet / Muduo） | 20 µs – 50 µs | 极低（函数指针） | 无 GC（手动） | 性能标尺，但手感痛苦、容易漏内存 |
| 主流 Go（Leaf / Pitaya） | 100 µs – 300 µs | 中（反射或接口） | 中（Go GC） | 并发与 DX 都好，但超低延迟尺度上 GC 抖动明显 |
| 老牌 C# / Java 框架 | 200 µs – 500 µs | 高（重反射） | 高（频繁 Gen0 / Gen1） | 业务开发快，但要靠加机器堆上去 |
| **GoPlay.Net（.NET 10）** | **42 µs** | **97 ns（极低）** | **极低（池化 / Span）** | **C++ 级性能 + C# 级手感** |

> Gemini 的一句话总结：
> *"在 managed-language 阵营（C# / Java / Go）里，它已经逼近 —— 而且在若干子指标上追平了 —— 深度调优 C++ 框架的上限。"*
>
> 完整逐项分析：[Docs/en/02.performance.md#third-party-review-what-gemini-31-pro-said](Docs/en/02.performance.md#third-party-review-what-gemini-31-pro-said) · [Docs/zh/02.performance.md#第三方评价gemini-31-pro-读过基线后怎么说](Docs/zh/02.performance.md#第三方评价gemini-31-pro-读过基线后怎么说)。

---

## 文档

- 中文文档入口：[Docs/zh](Docs/zh/01.getting-started.md)
- English docs: [Docs/en](Docs/en/01.getting-started.md)

| 主题 | English | 中文 |
|------|---------|------|
| 快速上手 | [en](Docs/en/01.getting-started.md) | [zh](Docs/zh/01.getting-started.md) |
| 性能 | [en](Docs/en/02.performance.md) | [zh](Docs/zh/02.performance.md) |
| 核心概念 | [en](Docs/en/03.concepts.md) | [zh](Docs/zh/03.concepts.md) |
| Processor-Actor 模型 | [en](Docs/en/04.processor-model.md) | [zh](Docs/zh/04.processor-model.md) |
| Transport 与 Encoder | [en](Docs/en/05.transport-encoder.md) | [zh](Docs/zh/05.transport-encoder.md) |
| 工具与代码生成 | [en](Docs/en/06.tools-codegen.md) | [zh](Docs/zh/06.tools-codegen.md) |
| 客户端（C# / TS / JS） | [en](Docs/en/07.clients.md) | [zh](Docs/zh/07.clients.md) |
| 进阶话题 | [en](Docs/en/08.advanced.md) | [zh](Docs/zh/08.advanced.md) |
| 仓库结构 | [en](Docs/en/09.structure.md) | [zh](Docs/zh/09.structure.md) |
| Wire Protocol | [en](Docs/en/10.protocol.md) | [zh](Docs/zh/10.protocol.md) |

---

## 亮点

- **Attribute 路由**：`[Processor("echo")]` + `[Request("request")]` → 客户端直接调 `"echo.request"`。没有样板 dispatcher。
- **Processor 即 Actor**：每个 Processor 独占一个 `ProcessorRunner` + FIFO 邮箱；业务代码默认零锁，需要并行时再上 `[MaxConcurrency(N)]`。
- **安全的跨 Processor 调用**：`Server.GetProcessor<T>()` + `[ProcessorApi]` + Roslyn Source Generator → 跨 Processor 调用写起来像本地方法，但实际跑在目标邮箱上。
- **可插拔 Transport**：开箱即用的 TCP / NetCoreServer / WebSocket / Secure WebSocket；也可以自己实现 `TransportServerBase`。
- **可插拔 Encoder**：Protobuf（默认，零分配热路径）和 Json；靠 `EncodingType` 切换。
- **完整客户端代码生成**：`goplay` CLI 扫描你的 Processor，生成强类型的客户端扩展（C#、TypeScript，或任何目标 —— 通过 [Liquid](https://shopify.github.io/liquid/) 模板）。
- **多语言客户端**：C#（桌面 + Unity）、TypeScript（`goplay-ws`）、纯 JavaScript（免构建）。
- **可测试性**：端到端测试写起来就像调本地方法（见 [TestEcho.cs](ProjectTemplates/GoPlay.Tcp.Template/UnitTests/TestEcho.cs)）。
- **生产细节**：带 drain 预算的优雅停机、请求 / 心跳超时、Kick 帧、带背压的广播、在不安全的跨 Processor 访问上直接让编译失败的 Roslyn 分析器。

---

## 30 秒服务器

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

## 30 秒客户端

```csharp
var client = new Client<NcClient>();
await client.Connect("localhost", 8888);

// 强类型，由服务端 [Request] 签名生成
var (status, resp) = await client.Echo_Request(new PbString { Value = "Hello" });
// status.Code == StatusCode.Success
// resp.Value  == "Server reply: Hello"
```

完整流程：[Getting Started (EN)](Docs/en/01.getting-started.md) / [快速上手 (中文)](Docs/zh/01.getting-started.md)。

---

## 安装

**最快 —— 模板**

```bash
dotnet new install GoPlay.Templates
dotnet new goplay-tcp -n MyGame      # 或：dotnet new goplay-ws
```

**NuGet**

```bash
dotnet add package GoPlay.Server
dotnet add package GoPlay.Core.Transport.NetCoreServer   # 或 Transport.Ws / Transport.Wss
dotnet add package GoPlay.Client
dotnet tool install -g GoPlay.Tools                      # 可选：goplay CLI
```

**TypeScript 客户端**

```bash
npm install goplay-ws
```

## NuGet 包

| 包 | 用途 | 源码 |
|----|------|------|
| [`GoPlay.Core`](https://www.nuget.org/packages/GoPlay.Core) | Protocol / Encoder / Transport 基类 | [Frameworks/Core](Frameworks/Core) |
| [`GoPlay.Server`](https://www.nuget.org/packages/GoPlay.Server) | Server、ProcessorRunner、ProcessorRef | [Frameworks/Server](Frameworks/Server) |
| [`GoPlay.Client`](https://www.nuget.org/packages/GoPlay.Client) | C# 客户端（net7+ 与 netstandard2.1 for Unity） | [Frameworks/Client](Frameworks/Client) |
| [`GoPlay.Core.Transport.NetCoreServer`](https://www.nuget.org/packages/GoPlay.Core.Transport.NetCoreServer) | TCP 传输（推荐） | [Frameworks/Transport.NetCoreServer](Frameworks/Transport.NetCoreServer) |
| [`GoPlay.Core.Transport.Ws`](https://www.nuget.org/packages/GoPlay.Core.Transport.Ws) | WebSocket 传输 | [Frameworks/Transport.Ws](Frameworks/Transport.Ws) |
| [`GoPlay.Core.Transport.Wss`](https://www.nuget.org/packages/GoPlay.Core.Transport.Wss) | Secure WebSocket 传输 | [Frameworks/Transport.Wss](Frameworks/Transport.Wss) |
| [`GoPlay.Tools`](https://www.nuget.org/packages/GoPlay.Tools) | `goplay` CLI（extension/config/excel2proto） | [Tools/Main](Tools/Main) |
| [`GoPlay.Templates`](https://www.nuget.org/packages/GoPlay.Templates) | `dotnet new goplay-tcp / goplay-ws` | [ProjectTemplates](ProjectTemplates) |

---

## 和同类框架怎么比

- **vs Pomelo**：一样的 `processor.method` 路由、Request / Notify / Push 三件套、握手里下发 route 表 —— 但在静态类型的 C# 上跑，每个 Processor 独立 Actor 隔离，还自带客户端代码生成。
- **vs ET**：都是 C# + Actor 模型；ET 是一个 Actor 对应一个 Entity（通常一个玩家），GoPlay 是一个 Actor 对应一个 Processor（一个功能模块）—— 更适合 lobby / match / battle / DB 这种按功能分层的结构。
- **vs ASP.NET Core**：同样的 Attribute 路由手感，但是长连接，带 Push / Notify / Broadcast；Processor 是长期存活的实例，不是 per-request 的 DI scope。
- **vs Orleans**：Processor 是"长期存活、强类型、每 Actor 并发度可配"的 grain；目前单节点，集群模式在路线图上（`TO-DO.md`）。

完整对比：[03.concepts.md](Docs/zh/03.concepts.md#与相关框架对比)。

---

## 仓库地图

见 [Docs/en/09.structure.md](Docs/en/09.structure.md) / [Docs/zh/09.structure.md](Docs/zh/09.structure.md)。简版：

```text
Frameworks/       C# 框架主体（Core / Server / Client / Transport*）
Clients/          TypeScript + 纯 JS 客户端
Tools/            goplay CLI + Roslyn 生成器 + 分析器
ProjectTemplates/ dotnet new goplay-tcp / goplay-ws 模板
Docs/             en/ + zh/ 主题文档（performance、concepts、protocol……）
```

---

## 致谢

感谢 JetBrains 为 GoPlay.Net 提供开源 License。

[![Thanks to JetBrains to provide opensource license for GoPlay.Net](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://jb.gg/OpenSourceSupport)

## License

MIT。见 [LICENSE](LICENSE)。

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
