# 快速上手

> English version: [getting-started.md](../en/getting-started.md)

本文给出三条安装路径，以及一份"从 0 到跑通 echo"的 5 分钟流水。

## 你需要先准备

- .NET SDK 7.0 / 8.0 / 9.0 / 10.0 任一（推荐 8.0 及以上）
- Node.js 18+（如果要用 TypeScript/JavaScript 客户端）
- 可选：Unity 2021+（`GoPlay.Client` 额外支持 `netstandard2.1`）

## 安装方式一：用 `dotnet new` 模板（最省心，推荐）

```bash
dotnet new install GoPlay.Templates

dotnet new goplay-tcp -n MyGame        # TCP 高性能版本（基于 NetCoreServer）
# 或
dotnet new goplay-ws  -n MyGame        # WebSocket 版本（浏览器 / 小游戏）

cd MyGame
dotnet build
dotnet run --project Main -- start -h 127.0.0.1 -p 8888
```

模板解开后的目录结构说明见 [structure.md](./structure.md#projecttemplates) 的模板一节。

## 安装方式二：直接引用 NuGet

适合往已有项目里增量接入：

```bash
# 服务端
dotnet add package GoPlay.Server
dotnet add package GoPlay.Core.Transport.NetCoreServer     # 或 Transport.Ws / Transport.Wss

# 客户端
dotnet add package GoPlay.Client

# CLI（可选，用于生成客户端扩展）
dotnet tool install -g GoPlay.Tools
```

## 安装方式三：克隆源码跑 Demo

```bash
git clone https://github.com/Jennal/GoPlay.Net.git
cd GoPlay.Net
dotnet run --project Frameworks/Demo/Demo.TcpServer    # 或 Demo.WsServer
```

`Demo.Common` 下的 [TestProcessor.cs](../../Frameworks/Demo/Demo.Common/TestProcessor.cs)、`ChatProcessor.cs`、`AirPlaneProcessor.cs` 是三份完整的业务 Processor 样例，覆盖 Request / Notify / Push / Broadcast / Update 各场景。

## 5 分钟走通一个 Echo

下面以 `goplay-tcp` 模板为例。

### 1）创建项目

```bash
dotnet new install GoPlay.Templates
dotnet new goplay-tcp -n MyGame
cd MyGame
```

### 2）写一个 Processor

打开 `Processors.Logic/EchoProcessor.cs`，模板已经生成，关键片段：

```csharp
[Processor("echo")]
public class EchoProcessor : ProcessorBase
{
    public override string[] Pushes => new[] { "echo.push" };

    [Request("request")]
    public PbString Request(Header header, PbString data)
    {
        return new PbString { Value = $"Serv reply: {data.Value}" };
    }

    [Notify("notify")]
    public void Notify(Header header, PbString data)
    {
        Push("echo.push", header, new PbString { Value = $"Serv push: {data.Value}" });
    }
}
```

- `[Processor("echo")]` 把这个类挂到名为 `echo` 的 route 命名空间下。
- `[Request("request")]` 把方法注册为 route `echo.request`，入参第一个必须是 `Header`，第二个是 Protobuf 消息。
- `Pushes` 声明了这个 Processor 能向客户端推送哪些 route。

### 3）生成客户端扩展

模板下的 `scripts/gen_ext.sh` 会调用 `goplay` CLI 扫描所有 Processor，产出 `Client.Extension/ClientExtensions.fe.cs`：

```bash
cd scripts
bash gen_ext.sh
```

生成的扩展让客户端像调用本地函数一样调 Processor：

```csharp
// 生成的扩展方法，由 Tools/Generator.Extension 产出
public static Task<(Status, PbString)> Echo_Request(this Client client, PbString data)
    => client.Request<PbString, PbString>("echo.request", data);
```

### 4）写一个端到端测试

模板已自带 `UnitTests/TestEcho.cs`（[TestEcho.cs](../../ProjectTemplates/GoPlay.Tcp.Template/UnitTests/TestEcho.cs)）：

```csharp
[Test]
public async Task TestRequest()
{
    var (status, resp) = await _client.Echo_Request(new PbString { Value = "Hello" });
    Assert.AreEqual(StatusCode.Success, status.Code);
    Assert.AreEqual("Serv reply: Hello", resp.Value);
}
```

运行：

```bash
dotnet test UnitTests/UnitTests.csproj
```

### 5）启动服务器

```bash
dotnet run --project Main -- start -h 127.0.0.1 -p 8888
```

服务器起来后，可以用模板自带客户端、自己的 Unity 客户端、TypeScript 客户端（`npm install goplay-ws`）连上它，见 [clients.md](./clients.md)。

## 接下来读什么

- 想理解每个概念的含义：[concepts.md](./concepts.md)
- 想看官方性能基线与调优建议：[performance.md](./performance.md)
- 想深入 Actor 模型：[processor-model.md](./processor-model.md)
- 想替换 Transport 或 Encoder：[transport-encoder.md](./transport-encoder.md)
- 想自动化代码生成：[tools-codegen.md](./tools-codegen.md)
- 浏览器/小程序接入：[clients.md](./clients.md)
- Filter / Session / 优雅停机等进阶话题：[advanced.md](./advanced.md)
