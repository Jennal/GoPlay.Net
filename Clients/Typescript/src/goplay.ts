import * as protobuf from "protobufjs";
import { GoPlay } from './pkg.pb';
import { ByteArray } from './ByteArray';
import Emitter from './Emitter';
import TaskCompletionSource from './TaskCompletionSource';
import Package from './Package';
import Long from 'long';

protobuf.util.Long = Long;
protobuf.configure();

let WebSocket: typeof import('ws') | typeof window.WebSocket;
if (typeof window === 'undefined') {
    // We are in Node.js
    WebSocket = require('ws');
} else {
    // We are in a web browser
    WebSocket = window.WebSocket;
    window['$protobuf'] = protobuf;
    window['GoPlay'] = protobuf.roots.default['GoPlay'];
}

const Consts = {
    Info: {
        "ClientVersion" : "GoPlay/Javascript;0.1",
        "ServerTag"     : GoPlay.Core.Protocols.ServerTag.FrontEnd,
    },
    Events: {
        "CONNECTED"    : "__ON_CONNECTED",
        "DISCONNECTED" : "__ON_DISCONNECTED",
        "ERROR"        : "__ON_ERROR",
        "KICKED"       : "__ON_KICKED",
        "BEFORE_SEND"  : "__ON_BEFORE_SEND",
        "BEFORE_RECV"  : "__ON_BEFORE_RECV",
    },
    TimeOut: {
        "CONNECT"     : 3000,
        "HEARTBEAT"   : 3000,
        "MAX_TIMEOUT" : 3,
        "REQUEST"     : 3000
    }
};

export default class goplay {
    public static Consts = Consts;
    public static Core = GoPlay.Core;
    public static encodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;

    public static debug: boolean = false;

    private static ws;
    private static url: string;
    private static buffer: ByteArray;

    public static emitter: Emitter = new Emitter();

    private static connectTask: TaskCompletionSource<boolean>;
    private static connectTimeOutId;

    private static disconnectTask: TaskCompletionSource<boolean>;

    private static handShake: GoPlay.Core.Protocols.RespHandShake;
    private static requestMap = {};
    private static pushMap = {};
    private static chunkMap = {};

    // 发送队列（P2-14）：encode 完的 ArrayBuffer 先入队，drain 根据 ws.bufferedAmount 背压冲出。
    // 使用 ArrayBufferLike 以兼容 TS lib 变化后 Uint8Array.buffer 的类型。
    private static sendQueue: ArrayBufferLike[] = [];
    private static drainTimer: any = null;
    private static readonly HIGH_WATERMARK: number = 1 << 20;
    private static readonly DRAIN_RETRY_MS: number = 16;

    // Filter pipeline（P2-15）：返回 false 即阻断该包继续向下投递；接入 send/recv/onerror。
    private static sendFilters: Array<(p: Package<any>) => boolean> = [];
    private static recvFilters: Array<(p: Package<any>) => boolean> = [];
    private static errorFilters: Array<(err: any) => void> = [];
    
    public static emit(event: string, ...args: any[]) {
        return goplay.emitter.emit(event, ...args);
    }

    public static emitAsync(event: string, ...args: any[]) {
        return goplay.emitter.emitAsync(event, ...args);
    }

    public static on(event: string, fn: Function) {
        return goplay.emitter.on(event, fn);
    }

    public static off(...args: any[]) {
        return goplay.emitter.off(...args);
    }

    public static once(event: string, fn: Function) {
        return goplay.emitter.once(event, fn);
    }

    public static listeners(event: string) {
        return goplay.emitter.listeners(event);
    }

    public static hasListeners(event: string) {
        return goplay.emitter.hasListeners(event);
    }

    public static removeAllListeners() {
        return goplay.emitter.removeAllListeners();
    }

    public static get isConnected(): boolean {
        if (!goplay.ws) return false;
        if (goplay.ws.readyState > 1) return false;
        if (!goplay.handShake) return false;

        return true;
    }

    public static setTimeout(key: string, val: number) {
        Consts.TimeOut[key] = val;
    };

    public static setClientVersion(version: string) {
        Consts.Info.ClientVersion = version;
    }

    public static setServerTag(tag: GoPlay.Core.Protocols.ServerTag) {
        Consts.Info.ServerTag = tag;
    }

    public static async connect(url?: string): Promise<boolean> {
        url = url || goplay.url;
        if (!url) throw new Error("url is empty");
        if (goplay.isConnected && goplay.url == url) return true;
        if (goplay.isConnected && goplay.url != url) await goplay.disconnect();
        if (goplay.connectTask) return goplay.connectTask.promise;

        goplay.url = url;
        let ws = new WebSocket(url);
        ws.binaryType = "arraybuffer";
        ws.onopen = goplay.onopen;
        ws.onmessage = goplay.onmessage;
        ws.onerror = goplay.onerror;
        ws.onclose = goplay.onclose;
        goplay.ws = ws;

        goplay.connectTask = new TaskCompletionSource<boolean>();
        goplay.connectTimeOutId = setTimeout(() => {
            if (goplay.isConnected) return;
            if (!goplay.connectTask) return;

            goplay.connectTask.result = false;
            goplay.connectTask = null;
            goplay.connectTimeOutId = null;
        }, Consts.TimeOut.CONNECT);

        return goplay.connectTask.promise;
    }
    
    public static async disconnect(): Promise<boolean> {
        // P0-4：重入保护。重复调用 disconnect 会覆盖 disconnectTask，
        // 老 await 再也等不到 resolve。走这里就复用现有 Task。
        if (goplay.disconnectTask) return goplay.disconnectTask.promise;

        if (goplay.connectTask) await goplay.connectTask.promise;
        if (!goplay.isConnected) return true;

        // P0-5：主动 disconnect 时预先清理连接超时 timer。
        goplay.clearConnectTimeout();

        goplay.disconnectTask = new TaskCompletionSource<boolean>();
        if (goplay.ws.readyState <= 1) goplay.ws.close();
        else {
            goplay.disconnectTask.result = true;
            goplay.ws = null;
            goplay.handShake = null;
            goplay.buffer = null;
            goplay.broadcastNetworkError();
            goplay.emit(Consts.Events.DISCONNECTED);
        }
        return goplay.disconnectTask.promise;
    }

    public static send(pack: Package<any>) {
        // P2-15：阻断型 sendFilter。任一 filter 返回 false 则丢弃整包。
        for (let i = 0; i < goplay.sendFilters.length; i++) {
            if (goplay.sendFilters[i](pack) === false) return;
        }
        goplay.emit(Consts.Events.BEFORE_SEND, pack);

        var packs = pack.split();
        for (var i = 0; i < packs.length; i++) {
            var p = packs[i];
            var data = p.encode();
            if (goplay.debug) console.log("Send: ", p, data);
            var buffer = new ByteArray(2 + data.length);
            buffer.writeUint16(data.length);
            buffer = buffer.writeBytes(data);

            // P2-14：先入队再 drain，不直接冲 ws.send。
            // 握手前 / 断线重连中入队的包会在 onopen / onHandshake / onclose 里被统一处理。
            goplay.sendQueue.push(buffer.data.buffer);
        }
        goplay.drain();
    }

    /**
     * P2-14：发送队列消费循环。
     * 按 ws.bufferedAmount 水位背压：高于 HIGH_WATERMARK 则暂停，一帧（~16ms）后重试。
     * ws 未进入 OPEN 状态时直接返回，等 onopen / onHandshake 主动重新驱动。
     */
    private static drain() {
        if (goplay.drainTimer) {
            clearTimeout(goplay.drainTimer);
            goplay.drainTimer = null;
        }

        const ws = goplay.ws;
        if (!ws) return;
        if (ws.readyState !== 1 /* OPEN */) return;

        while (goplay.sendQueue.length > 0) {
            const bufferedAmount = typeof ws.bufferedAmount === 'number' ? ws.bufferedAmount : 0;
            if (bufferedAmount >= goplay.HIGH_WATERMARK) {
                goplay.drainTimer = setTimeout(goplay.drain, goplay.DRAIN_RETRY_MS);
                return;
            }

            const buf = goplay.sendQueue.shift();
            try {
                ws.send(buf);
            } catch (err) {
                console.error('[goplay] ws.send failed:', err);
                goplay.emit(Consts.Events.ERROR, err);
                return;
            }
        }
    }

    private static getChunkKey(pack: Package<any>): string {
        return `${pack.header.PackageInfo.Route}_${pack.header.PackageInfo.Id}_${pack.header.PackageInfo.ChunkCount}`;
    }

    private static resolveChunk(pack: Package<any>): Package<any> {
        var key = this.getChunkKey(pack);
        if (!goplay.chunkMap.hasOwnProperty(key)) {
            goplay.chunkMap[key] = [];
        }

        goplay.chunkMap[key].push(pack);
        if (goplay.chunkMap[key].length < pack.header.PackageInfo.ChunkCount) return pack;

        var packages = goplay.chunkMap[key];
        delete goplay.chunkMap[key];
        return Package.join(packages);
    }

    private static recv(): Package<any> {
        if (!goplay.buffer || !goplay.buffer.length) return null;

        if (!goplay.buffer.hasReadSize(2)) return null;
        var packSize = goplay.buffer.readUint16();

        if (!goplay.buffer.hasReadSize(packSize)) {
            goplay.buffer.roffset -= 2;
            return null;
        }

        var data = goplay.buffer.readBytes(packSize);
        var pack = Package.tryDecodeRaw(data);
        if (goplay.debug) console.log("Recv: ", pack);

        if (!pack) return null;
        if (pack.header.PackageInfo.ChunkCount > 1) {
            pack = goplay.resolveChunk(pack);
            // chunk 还没齐：当前包字节已消费，但没有可派发的整包；
            // 尾调自身继续读 buffer 里下一个完整包，避免外层 while 误以为读完。
            if (pack.header.PackageInfo.ChunkCount > 1) return goplay.recv();
        }

        goplay.emit(Consts.Events.BEFORE_RECV, pack);
        return pack;
    }

    /**
     * 消除 buffer 中 [0, roffset) 的已读区：
     * 长连接下若不 compact，buffer 单调增长最终触发 OOM。
     * 仅在外层派发循环结束时调用一次，避免每包 compact 的高频分配。
     */
    private static compactBuffer() {
        const buf = goplay.buffer;
        if (!buf) return;
        if (buf.roffset <= 0) return;

        if (buf.roffset >= buf.woffset) {
            goplay.buffer = null;
            return;
        }

        const remain = buf.data.subarray(buf.roffset, buf.woffset);
        const fresh = new ByteArray(remain.length);
        fresh.data.set(remain);
        fresh.woffset = remain.length;
        fresh.roffset = 0;
        goplay.buffer = fresh;
    }

    /**
     * 派发单个已解码包。由 onmessage 的 while 循环反复调用；
     * P2-15 的 recvFilters 阻断语义也挂在这里。
     */
    private static dispatchPack(pack: Package<any>) {
        for (let i = 0; i < goplay.recvFilters.length; i++) {
            if (goplay.recvFilters[i](pack) === false) return;
        }

        var header = pack.header;
        switch (header.PackageInfo.Type) {
            case GoPlay.Core.Protocols.PackageType.Response:
                goplay.onResponse(pack);
                break;
            case GoPlay.Core.Protocols.PackageType.Push:
                goplay.onPush(pack);
                break;
            case GoPlay.Core.Protocols.PackageType.Ping:
                pack.header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Pong;
                goplay.send(pack);
                break;
            case GoPlay.Core.Protocols.PackageType.Pong:
                goplay.onHeartbeat(pack);
                break;
            case GoPlay.Core.Protocols.PackageType.HankShakeResp:
                goplay.onHandshake(pack);
                break;
            case GoPlay.Core.Protocols.PackageType.Kick:
                goplay.onKick(pack);
                break;
            default:
                console.log("should not be here!!", pack);
                break;
        }
    }

    public static onopen(event: Event) {
        if (goplay.debug) console.log("onopen", event);
        goplay.sendHandshake();
    }

    public static onmessage(event: MessageEvent) {
        var data = new ByteArray(event.data);

        if (!goplay.buffer) {
            goplay.buffer = data;
            goplay.buffer.woffset = data.length;
        } else {
            goplay.buffer = goplay.buffer.writeBytes(data);
        }

        // 循环派发：单次 WS frame 内可能携带多个完整包（服务端聚合、chunk 合并之后的尾部包等）。
        // 之前的单次 recv() 会丢弃 buffer 里剩余的完整包直到下一次 frame 触发。
        let pack: Package<any>;
        while ((pack = goplay.recv()) != null) {
            if (goplay.debug &&
                pack.header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Ping &&
                pack.header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Pong) {
                console.log("Recv: ", pack.header);
            }
            goplay.dispatchPack(pack);
        }

        goplay.compactBuffer();
    }

    public static onerror(...args: any[]) {
        if (goplay.debug) console.log("onerror", args);

        // P2-15：errorFilter 观察者，对齐 C# `Client.ErrorFilter` 钩子；不中断 ERROR 事件派发。
        for (let i = 0; i < goplay.errorFilters.length; i++) {
            try {
                goplay.errorFilters[i](args[0]);
            } catch (err) {
                console.error('[goplay] errorFilter throw:', err);
            }
        }

        goplay.emit(Consts.Events.ERROR, ...args);
    }

    public static onclose(event: Event) {
        if (goplay.debug) console.log("onclose", event);
        HeartBeat.stop();
        // P0-5：统一清理连接超时 timer，避免快速 connect/disconnect 循环残留。
        goplay.clearConnectTimeout();
        if (goplay.disconnectTask) {
            goplay.disconnectTask.result = true;
            // 完成后置 null：避免残留已完成 Task 让下一次 disconnect() 直接返回 true。
            goplay.disconnectTask = null;
        }
        if (goplay.connectTask) {
            goplay.connectTask.result = false;
            goplay.connectTask = null;
        }

        // P2-14：清空待发队列 + 清 drain 定时器，避免在新连接上乱序回放。
        goplay.sendQueue.length = 0;
        if (goplay.drainTimer) {
            clearTimeout(goplay.drainTimer);
            goplay.drainTimer = null;
        }

        goplay.ws = null;
        goplay.handShake = null;
        goplay.buffer = null;

        // P0-3：对所有在飞 request 同步投递 NETWORK_ERROR，
        // 让业务不必等到 REQUEST 超时才感知断线。
        goplay.broadcastNetworkError();

        goplay.emit(Consts.Events.DISCONNECTED);
    }

    /**
     * P0-5：connect 阶段的 timeout 统一清理入口。onclose / onHandshake 成功 / disconnect 都调。
     */
    private static clearConnectTimeout() {
        if (goplay.connectTimeOutId) {
            clearTimeout(goplay.connectTimeOutId);
            goplay.connectTimeOutId = null;
        }
    }

    public static getRouteEncoded(route) {
        if ( ! goplay.handShake) return 0;

        if (goplay.handShake.Routes.hasOwnProperty(route)) {
            return goplay.handShake.Routes[route];
        }
        
        return 0;
    }

    public static getRoute(routeEncoded) {
        if ( ! goplay.handShake) return "";

        for (var key in goplay.handShake.Routes) {
            if (goplay.handShake.Routes.hasOwnProperty(key)) {
                if (routeEncoded == goplay.handShake.Routes[key]) {
                    return key;
                }
            }
        }

        return "";
    }

    private static sendHandshake() {
        var data = new GoPlay.Core.Protocols.ReqHankShake();

        data.ClientVersion = Consts.Info.ClientVersion;
        data.ServerTag = Consts.Info.ServerTag;
        
        var pack = Package.createFromData(0, data, GoPlay.Core.Protocols.PackageType.HankShakeReq, goplay.encodingType);
        goplay.send(pack);
    }

    private static async onHandshake(p: Package<any>) {
        let pack = p.decodeFromRaw(GoPlay.Core.Protocols.RespHandShake);
        goplay.handShake = pack.data;

        HeartBeat.start(goplay.handShake.HeartBeatInterval);
        await goplay.emitAsync(Consts.Events.CONNECTED);

        // P0-5：连接超时 timer 统一从 clearConnectTimeout 走。
        goplay.clearConnectTimeout();
        if (goplay.connectTask) {
            goplay.connectTask.result = true;
            goplay.connectTask = null;
        }

        // P2-14：握手完成才算真正可收发，驱动一次 drain 把握手前入队的包冲出。
        goplay.drain();
    }

    private static onHeartbeat (p: Package<any>) {
        HeartBeat.clearKey(p.header.PackageInfo.Id);
    }

    private static onResponse (p: Package<any>) {
        var key = goplay.getCallbackKey(p.header);
        // console.log("key: ", key);
        // console.log("onResponse: ", p);
        var type = goplay.requestMap[key];
        if (type) {
            delete goplay.requestMap[key];
            let pack = p.decodeFromRaw(type);
            goplay.emit(key, {
                status: p.header.Status,
                data: pack.data
            });
        } else {
            goplay.emit(key, {
                status: p.header.Status,
                data: p.rawData
            });
        }
    }

    private static onPush (p: Package<any>) {
        var key = goplay.getPushKey(p.header);
        // console.log("key: ", key);

        var type = goplay.pushMap[key];
        if (type) {
            let pack = p.decodeFromRaw(type);
            if (goplay.debug) console.log(`onPush[${key}]: `, pack);
            goplay.emit(key, pack.data);
        } else {
            if (goplay.debug) console.log(`onPush[${key}]: `, p);
            goplay.emit(key, p.rawData);
        }
    }

    private static onKick (p: Package<any>) {
        goplay.emit(Consts.Events.KICKED);
        goplay.disconnect();
    }

    /**
     * 断线时对所有在飞 request 统一投递 NETWORK_ERROR。
     * 对齐 C# `DisconnectAsync` 里遍历 `m_requestCallbacks` 广播 `NETWORK_ERROR` 的行为，
     * 让业务 `await request(...)` 在断线后立即失败而非等到 REQUEST 超时。
     */
    private static broadcastNetworkError() {
        const keys = Object.keys(goplay.requestMap);
        if (keys.length <= 0) return;

        for (let i = 0; i < keys.length; i++) {
            const key = keys[i];
            delete goplay.requestMap[key];

            const status = new GoPlay.Core.Protocols.Status();
            status.Code = GoPlay.Core.Protocols.StatusCode.Error;
            status.Message = "NETWORK_ERROR";
            goplay.emit(key, { status: status, data: null });
        }
    }

    private static getCallbackKey (header: GoPlay.Core.Protocols.Header): string {
        // 对齐 C# `Client.cs` m_requestCallbacks[packId]：仅用 Id 作键，
        // 避免依赖服务端在 Response 里回填原 Route 的隐形耦合。
        return `${header.PackageInfo.Id}`;
    }

    private static getPushKey (header: GoPlay.Core.Protocols.Header): string {
        return goplay.getRoute(header.PackageInfo.Route);
    }

    public static onType<T>(event: string, type: {new():T}, fn: Function) {
        if (goplay.pushMap[event] && goplay.pushMap[event] != type) throw new Error(`event ${event} already registered with type ${goplay.pushMap[event]}`);
        goplay.pushMap[event] = type;
        goplay.emitter.on(event, fn);
    }

    public static onceType<T>(event: string, type: {new():T}, fn: Function) {
        if (goplay.pushMap[event] && goplay.pushMap[event] != type) throw new Error(`event ${event} already registered with type ${goplay.pushMap[event]}`);
        goplay.pushMap[event] = type;
        goplay.emitter.once(event, fn);
    }

    /**
     * 语义对齐 C# `Client<T>.WaitFor<TD>(route)`：
     * 返回一个 Promise，在 event 首次触发时 resolve。
     * 传入 type 时通过 onceType 走 push 解码路径；省略 type 走原始 once（直接拿 emitter 的第一个参数）。
     */
    public static waitFor<T>(event: string, type?: {new(): T}): Promise<T> {
        return new Promise<T>(resolve => {
            if (type) {
                goplay.onceType(event, type, (data: T) => resolve(data));
            } else {
                goplay.once(event, (data: T) => resolve(data));
            }
        });
    }

    // ===== P2-15: Filter pipeline API =====
    // 语义对齐 C# `Client.Filterable.cs` 的最小可用子集：
    //  - sendFilter / recvFilter 返回 false 即阻断该包；
    //  - errorFilter 为观察者回调，不阻断 ERROR 事件派发。
    // 多 filter 按注册顺序依次执行，先返回 false 者即刻短路。

    public static addSendFilter(fn: (p: Package<any>) => boolean) {
        goplay.sendFilters.push(fn);
    }
    public static removeSendFilter(fn: (p: Package<any>) => boolean) {
        goplay.sendFilters = goplay.sendFilters.filter(f => f !== fn);
    }
    public static addRecvFilter(fn: (p: Package<any>) => boolean) {
        goplay.recvFilters.push(fn);
    }
    public static removeRecvFilter(fn: (p: Package<any>) => boolean) {
        goplay.recvFilters = goplay.recvFilters.filter(f => f !== fn);
    }
    public static addErrorFilter(fn: (err: any) => void) {
        goplay.errorFilters.push(fn);
    }
    public static removeErrorFilter(fn: (err: any) => void) {
        goplay.errorFilters = goplay.errorFilters.filter(f => f !== fn);
    }

    public static request<T, RT>(route: string, data: T, resultType?: {new(): RT}): Promise<any> {
        var task = new TaskCompletionSource<any>();
        var pack = Package.createFromData(goplay.getRouteEncoded(route), data, GoPlay.Core.Protocols.PackageType.Request, goplay.encodingType);
        var key = goplay.getCallbackKey(pack.header);
        goplay.requestMap[key] = resultType;
        // console.log("key: ", key);
        var timeOutId = setTimeout(function () {
            // 超时也要清 requestMap，否则迟到的 Response 会命中已消费的 type 条目
            // 造成 decode 后派发给无人监听的 emit（泄漏 + 潜在 decode 抛错）。
            delete goplay.requestMap[key];
            var status = new GoPlay.Core.Protocols.Status();
            status.Code = GoPlay.Core.Protocols.StatusCode.Timeout;
            status.Message = "request time out";
            let result = {
                status: status,
                data: null
            };
            goplay.emit(key, result);
        }, Consts.TimeOut.REQUEST);
        goplay.once(key, function (result) {
            clearTimeout(timeOutId);
            task.result = result;
        });

        goplay.send(pack);
        return task.promise;
    }

    public static notify<T>(route: string, data?: T) {
        var pack = Package.createFromData(goplay.getRouteEncoded(route), data, GoPlay.Core.Protocols.PackageType.Notify, goplay.encodingType);
        goplay.send(pack);
    }
}

class HeartBeat {
    public static interval = 10000;
    private static intervalId;
    private static timeOutMap = {};
    private static timeOutCount = 0;

    public static start(interval: number = 10000) {
        if (HeartBeat.intervalId) return;

        // 修 Bug 2：之前 setInterval 用的是静态属性 HeartBeat.interval（10000 写死），
        // 服务端 RespHandShake.HeartBeatInterval 被吞掉。现在将传入 interval 回写到静态属性。
        HeartBeat.interval = interval || HeartBeat.interval;
        HeartBeat.timeOutCount = 0;
        HeartBeat.intervalId = setInterval(() => {
            var pack = Package.createFromData(0, null, GoPlay.Core.Protocols.PackageType.Ping, goplay.encodingType);
            goplay.send(pack);
            HeartBeat.timeOutMap[pack.header.PackageInfo.Id] = setTimeout(() => {
                HeartBeat.timeOutCount++;
                if (HeartBeat.timeOutCount > Consts.TimeOut.MAX_TIMEOUT) {
                    console.log("heartbeat timeout count > " + Consts.TimeOut.MAX_TIMEOUT);
                    goplay.disconnect();
                }
            }, Consts.TimeOut.HEARTBEAT);
        }, HeartBeat.interval);
    }

    public static clearKey(key) {
        if (!HeartBeat.timeOutMap.hasOwnProperty(key)) return;

        var timeOutId = HeartBeat.timeOutMap[key];
        clearTimeout(timeOutId);
        delete HeartBeat.timeOutMap[key];
        // 修 Bug 3：任一 Pong 命中即清零"连续未响应"计数，避免长时间运行被累积误杀。
        HeartBeat.timeOutCount = 0;
    }

    public static stop() {
        if (!HeartBeat.intervalId) return;

        clearInterval(HeartBeat.intervalId);
        HeartBeat.intervalId = null;

        for (var key in HeartBeat.timeOutMap) {
            HeartBeat.clearKey(key);
        }
    }
}

//For Browser
if (typeof window !== 'undefined') {
    (window as any)['goplay'] = goplay;
}