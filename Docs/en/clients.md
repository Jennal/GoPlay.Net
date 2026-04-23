# Clients

> 中文版: [clients.md](../zh/clients.md)

GoPlay ships three official clients: C# (desktop, Unity), TypeScript (Web / Node / mini-games), and JavaScript (build-free browser drop-in). All three follow the same wire protocol ([protocol.md](./protocol.md)); Handshake / Heartbeat / Request / Notify / Push semantics are identical.

## C# Client (`GoPlay.Client`)

### Install

```bash
dotnet add package GoPlay.Client
# plus a transport
dotnet add package GoPlay.Core.Transport.NetCoreServer   # or Transport.Ws / Transport.Wss
```

TFMs: `net7.0;net8.0;net9.0;net10.0;netstandard2.1` (the last one targets Unity).

### Usage

```csharp
using GoPlay;
using GoPlay.Core.Transport.NetCoreServer;
using GoPlay.Core.Protocols;

var client = new Client<NcClient>();
client.OnConnected    += () => Console.WriteLine("Connected");
client.OnDisconnected += () => Console.WriteLine("Disconnected");
client.OnError        += err => Console.WriteLine($"Err: {err}");
client.OnKicked       += reason => Console.WriteLine($"Kicked: {reason}");

await client.Connect("localhost", 8888);

// Request - wait for response
var (status, resp) = await client.Request<PbString, PbString>("echo.request",
                                                              new PbString { Value = "Hi" });

// Notify - fire-and-forget
client.Notify("echo.notify", new PbString { Value = "Hi" });

// Subscribe to push
client.AddListener<PbString>("echo.push", data =>
    Console.WriteLine($"push: {data.Value}"));

// Or await a single push
var pushed = await client.WaitFor<PbString>("echo.push");
```

If the server has been through `goplay extension`, you can use strongly-typed extensions:

```csharp
var (status, resp) = await client.Echo_Request(new PbString { Value = "Hi" });
client.Echo_Notify(new PbString { Value = "Hi" });
```

### Main Members

- `Connect(host, port, timeout?)` - performs the handshake and starts send/recv loops; returns `false` and raises `OnError` on failure.
- `Disconnect()` / `DisconnectAsync()`.
- `Request<T, TR>(route, data)` / `Request<TR>(route)` / `Request<T>(route, data)` / `Request(route)` - 4 overloads.
- `Notify<T>(route, data)` / `Notify(route)`.
- `AddListener<T>(route, Action<T>)` / `AddListenerOnce` / `RemoveListener` - push subscriptions.
- `WaitFor<T>(route)` - returns `Task<T>`, resolves on the next matching push.
- `MainThreadActionRunner` - see the Unity section below.
- Heartbeat stats: `PingAvg` / `PingMax` / `PingMin` / `PingCount`.

### Unity / Main-thread Callbacks

Unity only lets you touch UI / GameObjects on the main thread. The framework exposes `MainThreadActionRunner`:

```csharp
public interface IMainThreadActionRunner
{
    void Invoke(Action action);
}
```

The default implementation runs on the callback thread. In Unity plug in your own wrapper:

```csharp
client.MainThreadActionRunner = new UnityMainThreadActionRunner();
```

All `OnConnected` / `OnDisconnected` / `OnError` / `AddListener` callbacks then hop to the main thread.

### Reconnect

The client does not bake in an auto-reconnect policy, but the API makes it trivial:

```csharp
async Task Loop()
{
    while (!stop)
    {
        if (client.Status == Client.ClientStatus.Disconnected)
        {
            try { await client.Connect(host, port); }
            catch { /* ignore, retry */ }
        }
        await Task.Delay(1000);
    }
}
```

## TypeScript Client (`goplay-ws`)

WebSocket + Protobuf (protobufjs). Source in [Clients/Typescript/](../../Clients/Typescript/).

### Install

```bash
npm install goplay-ws
```

### Usage

```ts
import goplay from 'goplay-ws';
import { GoPlay } from 'goplay-ws/dist/pkg.pb';   // protocol types

goplay.on(goplay.Consts.Events.CONNECTED,    () => console.log('connected'));
goplay.on(goplay.Consts.Events.DISCONNECTED, () => console.log('disconnected'));
goplay.on(goplay.Consts.Events.ERROR,        err => console.log('err', err));
goplay.on(goplay.Consts.Events.KICKED,       reason => console.log('kicked', reason));

await goplay.connect('ws://127.0.0.1:8888');

// Request
const req = new GoPlay.Core.Protocols.PbString();
req.Value = 'hello';
const resp: any = await goplay.request(
    'echo.request', req,
    GoPlay.Core.Protocols.PbString,   // response type
);
console.log(resp.status.Code, resp.data.Value);

// Notify
goplay.notify('echo.notify', req);

// Push
goplay.onType('echo.push', GoPlay.Core.Protocols.PbString, (data: any) => {
    console.log('push', data.Value);
});
```

Full runnable example: [Clients/Typescript/unit_test/e2e/goplay.Request.test.ts](../../Clients/Typescript/unit_test/e2e/goplay.Request.test.ts).

### Key APIs (from [goplay.ts](../../Clients/Typescript/src/goplay.ts))

- `goplay.connect(url) / goplay.disconnect()`
- `goplay.request<T, RT>(route, data, resultType)`
- `goplay.notify<T>(route, data)`
- `goplay.onType<T>(event, type, fn)` / `goplay.onceType<T>(...)`
- `goplay.on / once / off / removeAllListeners` (built-in Emitter bus)
- Filter pipeline: `sendFilters` / `recvFilters` / `errorFilters` hooks

The TS client also implements **send-side backpressure**: `HIGH_WATERMARK = 1 MiB`; above it a `drainTimer` (every 16 ms) drains `ws.bufferedAmount` instead of blocking `send()`.

### Environment Notes

- **Browser**: `import goplay from 'goplay-ws'` uses `window.WebSocket`.
- **Node.js**: automatically falls back to the `ws` module.
- **Mini-games / WeChat**: engines vary; check `src/goplay.ts` WebSocket shim and swap as needed.

## JavaScript Client (build-free)

Files: [Clients/Javascript/goplay.client.js](../../Clients/Javascript/goplay.client.js) + [protobuf.min.js](../../Clients/Javascript/protobuf.min.js).

Ideal for "drop a few JS files into HTML and be done" scenarios - no npm / webpack / ts. Same semantics as the TypeScript version; API is slightly older (kept stable for compatibility).

```html
<script src="protobuf.min.js"></script>
<script src="long_umd_v5.2.3.js"></script>
<script src="pkg.pb.js"></script>
<script src="pb.helpers.js"></script>
<script src="goplay.client.js"></script>
<script>
    goplay.connect('ws://127.0.0.1:8888', async () => {
        const resp = await goplay.request('echo.request', { Value: 'hi' });
        console.log(resp);
    });
</script>
```

Browser demo: [Clients/Javascript/demo/](../../Clients/Javascript/demo/).

## Handshake Sequence (all clients)

```mermaid
sequenceDiagram
  participant C as Client
  participant T as Transport
  participant S as Server

  C->>T: ConnectAsync(host, port)
  T-->>C: connected
  C->>S: HankShakeReq { ClientVersion, ServerTag, AppKey }
  S-->>C: HankShakeResp { ServerVersion, HeartBeatInterval, Routes }
  C->>C: cache Routes map
  Note over C,S: Connected event; business can Request/Notify

  loop Every HeartBeatInterval
    C->>S: Ping { Id }
    S-->>C: Pong { Id }
  end

  alt server kicks
    S-->>C: Kick { Status.Message = "reason" }
    C->>C: OnKicked(reason) + Disconnect
  end
```

- Request / Response are correlated by `Id`, see [protocol.md](./protocol.md).
- The route-string-to-uint map is delivered once during handshake; business code can keep using strings (the client resolves via an O(1) lookup).
- A Pong arriving past the threshold raises `HeartbeatTimeoutException` and the client disconnects.
