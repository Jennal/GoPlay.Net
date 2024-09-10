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
        if (goplay.connectTask) await goplay.connectTask.promise;
        if (!goplay.isConnected) return true;

        goplay.disconnectTask = new TaskCompletionSource<boolean>();
        if (goplay.ws.readyState <= 1) goplay.ws.close();
        else goplay.disconnectTask.result = true;
        goplay.ws = null;
        goplay.handShake = null;
        goplay.buffer = null;
        goplay.emit(Consts.Events.DISCONNECTED);
        return goplay.disconnectTask.promise;
    }

    public static send(pack: Package<any>) {
        goplay.emit(Consts.Events.BEFORE_SEND, pack);

        var packs = pack.split();
        for (var i = 0; i < packs.length; i++) {
            var p = packs[i];
            var data = p.encode();
            if (goplay.debug) console.log("Send: ", p, data);
            var buffer = new ByteArray(2 + data.length);
            buffer.writeUint16(data.length);
            buffer = buffer.writeBytes(data);
            // console.log("goplay.send: ", buffer.data);
            goplay.ws.send(buffer.data.buffer);
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
            if (pack.header.PackageInfo.ChunkCount > 1) return null;
        }

        goplay.emit(Consts.Events.BEFORE_RECV, pack);
        //TODO: remove read data from buffer
        return pack;
    }

    public static onopen(event: Event) {
        if (goplay.debug) console.log("onopen", event);
        goplay.sendHandshake();
    }

    public static onmessage(event: MessageEvent) {
        var data = new ByteArray(event.data);
        // if (goplay.debug) console.log("onmessage-1", event, data);

        if (!goplay.buffer) {
            goplay.buffer = data;
            goplay.buffer.woffset = data.length;
        } else {
            goplay.buffer = goplay.buffer.writeBytes(data);
        }
        // console.log("onmessage-2", goplay.buffer);

        var pack = goplay.recv();
        if (!pack) return;

        var header = pack.header;
        if (header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Ping && 
            header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Pong) {
            if (goplay.debug) console.log("Recv: ", header, data);
        }

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

    public static onerror(...args: any[]) {
        if (goplay.debug) console.log("onerror", args);
        goplay.emit(Consts.Events.ERROR, ...args);
    }

    public static onclose(event: Event) {
        if (goplay.debug) console.log("onclose", event);
        HeartBeat.stop();
        if (goplay.disconnectTask) goplay.disconnectTask.result = true;
        if (goplay.connectTask) goplay.connectTask.result = false;
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
        // console.log("onHandshake: ", goplay.handShake);

        HeartBeat.start(goplay.handShake.HeartBeatInterval);
        await goplay.emitAsync(Consts.Events.CONNECTED);

        goplay.connectTask.result = true;
        clearTimeout(goplay.connectTimeOutId);

        goplay.connectTask = null;
        goplay.connectTimeOutId = null;
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

    private static getCallbackKey (header: GoPlay.Core.Protocols.Header): string {
        return `${header.PackageInfo.Route}-${header.PackageInfo.Id}`;
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

    public static request<T, RT>(route: string, data: T, resultType?: {new(): RT}): Promise<any> {
        var task = new TaskCompletionSource<any>();
        var pack = Package.createFromData(goplay.getRouteEncoded(route), data, GoPlay.Core.Protocols.PackageType.Request, goplay.encodingType);
        var key = goplay.getCallbackKey(pack.header);
        goplay.requestMap[key] = resultType;
        // console.log("key: ", key);
        var timeOutId = setTimeout(function () {
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

        interval = interval || HeartBeat.interval;
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