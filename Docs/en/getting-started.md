# Getting Started

> 中文版: [getting-started.md](../zh/getting-started.md)

This page walks through three installation paths and a 5-minute "from zero to running echo" tour.

## Prerequisites

- .NET SDK 7.0 / 8.0 / 9.0 / 10.0 (recommend 8.0+)
- Node.js 18+ (if you need the TypeScript/JavaScript client)
- Optional: Unity 2021+ (`GoPlay.Client` targets `netstandard2.1` for this)

## Option 1: `dotnet new` Template (easiest, recommended)

```bash
dotnet new install GoPlay.Templates

dotnet new goplay-tcp -n MyGame        # TCP fast path (NetCoreServer)
# or
dotnet new goplay-ws  -n MyGame        # WebSocket (browser / mini-game)

cd MyGame
dotnet build
dotnet run --project Main -- start -h 127.0.0.1 -p 8888
```

See the template layout in [structure.md](./structure.md#projecttemplates).

## Option 2: Add NuGet Packages to an Existing Project

```bash
# Server side
dotnet add package GoPlay.Server
dotnet add package GoPlay.Core.Transport.NetCoreServer     # or Transport.Ws / Transport.Wss

# Client side
dotnet add package GoPlay.Client

# CLI (optional, used to generate client extensions)
dotnet tool install -g GoPlay.Tools
```

## Option 3: Clone and Run the Demo

```bash
git clone https://github.com/Jennal/GoPlay.Net.git
cd GoPlay.Net
dotnet run --project Frameworks/Demo/Demo.TcpServer    # or Demo.WsServer
```

Under `Demo.Common` you will find [TestProcessor.cs](../../Frameworks/Demo/Demo.Common/TestProcessor.cs), `ChatProcessor.cs` and `AirPlaneProcessor.cs` - three fully working processor samples that exercise Request / Notify / Push / Broadcast / Update.

## 5-Minute Echo Tour

Using the `goplay-tcp` template.

### 1) Scaffold

```bash
dotnet new install GoPlay.Templates
dotnet new goplay-tcp -n MyGame
cd MyGame
```

### 2) Write a Processor

Open `Processors.Logic/EchoProcessor.cs`; the template already generates:

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

- `[Processor("echo")]` mounts the class under the `echo` route namespace.
- `[Request("request")]` registers the method as route `echo.request`. The first parameter must be `Header`; the second is your Protobuf message.
- `Pushes` declares which routes this processor can push to the client.

### 3) Generate Client Extensions

The template ships `scripts/gen_ext.sh` that calls the `goplay` CLI to scan every processor and write `Client.Extension/ClientExtensions.fe.cs`:

```bash
cd scripts
bash gen_ext.sh
```

Generated extensions let the client call server processors like local functions:

```csharp
public static Task<(Status, PbString)> Echo_Request(this Client client, PbString data)
    => client.Request<PbString, PbString>("echo.request", data);
```

### 4) Write an End-to-End Test

The template includes `UnitTests/TestEcho.cs` ([TestEcho.cs](../../ProjectTemplates/GoPlay.Tcp.Template/UnitTests/TestEcho.cs)):

```csharp
[Test]
public async Task TestRequest()
{
    var (status, resp) = await _client.Echo_Request(new PbString { Value = "Hello" });
    Assert.AreEqual(StatusCode.Success, status.Code);
    Assert.AreEqual("Serv reply: Hello", resp.Value);
}
```

Run it:

```bash
dotnet test UnitTests/UnitTests.csproj
```

### 5) Start the Server

```bash
dotnet run --project Main -- start -h 127.0.0.1 -p 8888
```

With the server up, point any client (the templated C# client, Unity, or TypeScript `npm install goplay-ws`) at it - see [clients.md](./clients.md).

## What to Read Next

- Terminology and mental model: [concepts.md](./concepts.md)
- Official performance baseline & tuning tips: [performance.md](./performance.md)
- Actor model deep-dive: [processor-model.md](./processor-model.md)
- Swap Transport / Encoder: [transport-encoder.md](./transport-encoder.md)
- Automate codegen: [tools-codegen.md](./tools-codegen.md)
- Browser / mini-game clients: [clients.md](./clients.md)
- Filter / Session / graceful shutdown: [advanced.md](./advanced.md)
