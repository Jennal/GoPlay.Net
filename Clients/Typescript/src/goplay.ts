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
        "ClientType": "GoPlay/Javascript",
        "ClientVersion": "0.1"
    },
    Events: {
        "CONNECTED"    : "__ON_CONNECTED",
        "DISCONNECTED" : "__ON_DISCONNECTED",
        "ERROR"        : "__ON_ERROR",
        "KICKED"       : "__ON_KICKED"
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
    
    public static emit(event: string, ...args: any[]) {
        goplay.emitter.emit(event, ...args);
    }

    public static on(event: string, fn: Function) {
        goplay.emitter.on(event, fn);
    }

    public static off(...args: any[]) {
        goplay.emitter.off(...args);
    }

    public static once(event: string, fn: Function) {
        goplay.emitter.once(event, fn);
    }

    public static listeners(event: string) {
        return goplay.emitter.listeners(event);
    }

    public static hasListeners(event: string) {
        return goplay.emitter.hasListeners(event);
    }

    public static removeAllListeners() {
        goplay.emitter.removeAllListeners();
    }

    public static get isConnected(): boolean {
        if (!goplay.ws) return false;
        if (goplay.ws.readyState > 1) return false;

        return true;
    }

    public static async connect(url: string): Promise<boolean> {
        if (goplay.isConnected && goplay.url == url) return true;

        if (goplay.isConnected && goplay.url != url) await goplay.disconnect();

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
        }, Consts.TimeOut.CONNECT);

        return goplay.connectTask.promise;
    }
    
    public static async disconnect(): Promise<boolean> {
        if (!goplay.ws) return true;

        goplay.disconnectTask = new TaskCompletionSource<boolean>();
        if (goplay.ws.readyState <= 1) goplay.ws.close();
        else goplay.disconnectTask.result = true;
        goplay.ws = null;
        goplay.emit(Consts.Events.DISCONNECTED);
        return goplay.disconnectTask.promise;
    }

    public static send(pack: Package<any>) {
        var data = pack.encode();
        // console.log("Send: ", pack, data);
        var buffer = new ByteArray(2 + data.length);
        buffer.writeUint16(data.length);
        buffer = buffer.writeBytes(data);
        goplay.ws.send(buffer.data);
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
        // console.log("recv: ", pack);

        //TODO: remove read data from buffer
        return pack;
    }

    public static onopen(event: Event) {
        console.log("onopen", event)
        goplay.sendHandshake();
    }

    public static onmessage(event: MessageEvent) {
        var data = new ByteArray(event.data);
        // console.log("onmessage-1", event, data, goplay.buffer);

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
            // console.log("Recv: ", header, data);
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
        console.log("onerror", args);
        goplay.emit(Consts.Events.ERROR, ...args);
    }

    public static onclose(event: Event) {
        console.log("onclose", event);
        HeartBeat.stop();
        goplay.disconnectTask.result = true;
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
        data.ServerTag = GoPlay.Core.Protocols.ServerTag.FrontEnd;
        
        var pack = Package.createFromData(0, data, GoPlay.Core.Protocols.PackageType.HankShakeReq, goplay.encodingType);
        goplay.send(pack);
    }

    private static onHandshake(p: Package<any>) {
        let pack = p.decodeFromRaw(GoPlay.Core.Protocols.RespHandShake);
        goplay.handShake = pack.data;
        // console.log("onHandshake: ", goplay.handShake);

        HeartBeat.start(goplay.handShake.HeartBeatInterval);
        goplay.emit(Consts.Events.CONNECTED);

        goplay.connectTask.result = true;
        clearTimeout(goplay.connectTimeOutId);
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
            console.log(`onPush[${key}]: `, pack);
            goplay.emit(key, pack.data);
        } else {
            console.log(`onPush[${key}]: `, p);
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