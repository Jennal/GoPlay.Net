# goplay-ws

[![npm](https://img.shields.io/npm/v/goplay-ws.svg)](https://www.npmjs.com/package/goplay-ws)
[![license](https://img.shields.io/npm/l/goplay-ws.svg)](./LICENSE)

Browser / Node WebSocket client for [GoPlay.Net](https://github.com/jennal/goplay.net), a C#/.NET game server framework. Protobuf-based framing with request / push / heartbeat / chunk-split built in.

Semantics aligned with the C# `Client<T>` reference implementation: stateful reconnect, request-response with deterministic `Id` routing, backpressure-aware send queue, and send / recv / error filter pipelines.

## Install

```bash
npm install goplay-ws
```

Works in:
- **Browser**: uses the native `WebSocket`; ship through webpack / rollup / vite.
- **Node.js 18+**: uses [`ws`](https://www.npmjs.com/package/ws) transparently.

## Quick start

```ts
import goplay from 'goplay-ws';
import { GoPlay } from 'goplay-ws/dist/pkg.pb';

await goplay.connect('ws://localhost:8888');

// --- Request / response ---
const req = new GoPlay.Core.Protocols.PbString();
req.Value = 'hello';
const resp = await goplay.request(
  'test.echo',
  req,
  GoPlay.Core.Protocols.PbString,
);
console.log(resp.status.Code, resp.data.Value);

// --- Push subscription ---
goplay.onType('test.push', GoPlay.Core.Protocols.PbString, (data) => {
  console.log('push:', data.Value);
});

// --- Fire-and-forget notify ---
const n = new GoPlay.Core.Protocols.PbString();
n.Value = 'ping';
goplay.notify('test.notify', n);

// --- Promise-based one-shot wait ---
const data = await goplay.waitFor(
  'test.push',
  GoPlay.Core.Protocols.PbString,
);

// --- Lifecycle events ---
goplay.on('__ON_CONNECTED', () => console.log('connected'));
goplay.on('__ON_DISCONNECTED', () => console.log('disconnected'));
goplay.on('__ON_ERROR', (err) => console.error(err));
goplay.on('__ON_KICKED', () => console.warn('kicked'));

await goplay.disconnect();
```

## API surface

### Connection
- `goplay.connect(url?: string): Promise<boolean>` — idempotent; same-URL reconnect returns `true` fast, different URL auto-disconnects first. Uses `Consts.TimeOut.CONNECT` (default `3000ms`).
- `goplay.disconnect(): Promise<boolean>` — re-entrant: concurrent calls share the same pending task. Broadcasts `NETWORK_ERROR` to every in-flight `request`.
- `goplay.isConnected: boolean` — `true` only after a successful handshake.

### Messaging
- `goplay.request<T, RT>(route, data, resultType?): Promise<{ status, data }>` — server reply or timeout (`Consts.TimeOut.REQUEST`, default `3000ms`); callbacks keyed by packet `Id` to match the C# `Client.m_requestCallbacks` semantics.
- `goplay.notify<T>(route, data?)` — fire-and-forget.
- `goplay.onType<T>(event, type, fn)` / `goplay.onceType<T>(event, type, fn)` — subscribe to a Push route with decoded payload type.
- `goplay.waitFor<T>(event, type?): Promise<T>` — Promise wrapper around `once` / `onceType`. Mirrors C# `WaitFor<TD>`.

### Filter pipeline
Subset of the C# `Client.Filterable` API. Each filter runs in registration order; returning `false` blocks the packet for send/recv filters; error filters are observers (non-blocking).

```ts
goplay.addSendFilter((pack) => { /* ... return false to drop ... */ });
goplay.addRecvFilter((pack) => { /* ... */ });
goplay.addErrorFilter((err) => { /* ... */ });
goplay.removeSendFilter(fn);
goplay.removeRecvFilter(fn);
goplay.removeErrorFilter(fn);
```

### Tuning
```ts
goplay.setTimeout('CONNECT', 5000);
goplay.setTimeout('REQUEST', 5000);
goplay.setTimeout('HEARTBEAT', 3000);
goplay.setTimeout('MAX_TIMEOUT', 3); // consecutive heartbeat misses before disconnect
goplay.setClientVersion('my-app/1.0.0');
```

## Protocol compatibility

Wire format and protobuf contracts are generated from [Frameworks/Res/Proto3](https://github.com/jennal/goplay.net/tree/master/Frameworks/Res/Proto3) and kept in lock-step with the C# `Client` / `Server`. See `src/pkg.pb.d.ts` for the full surface.

## Development

```bash
# install deps
npm install

# regenerate protobuf bindings (requires pbjs/pbts on PATH)
bash scripts/build_proto.sh

# build dist/
npm run build

# unit tests (offline)
npm run test:unit

# full suite incl. live Demo.WsServer (requires .NET 7 SDK)
npm run test:e2e
```

## License

[MIT](./LICENSE)
