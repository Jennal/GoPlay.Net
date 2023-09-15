import { GoPlay } from './pkg';
import { ByteArray } from './ByteArray';
import { getEncoder } from './Encoder';
import Emitter from './Emitter';
import TaskCompletionSource from './TaskCompletionSource';
import IdGen from './IdGen';
import Package from './Package';

let WebSocket: typeof import('ws') | typeof window.WebSocket;
if (typeof window === 'undefined') {
  // We are in Node.js
  WebSocket = require('ws');
} else {
  // We are in a web browser
  WebSocket = window.WebSocket;
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
    public static encodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;

    private static ws;
    private static url: string;
    private static buffer: ByteArray;

    private static emitter: Emitter = new Emitter();
    private static emit = goplay.emitter.emit;
    private static on = goplay.emitter.on;
    private static off = goplay.emitter.off;
    private static once = goplay.emitter.once;

    private static connectTask: TaskCompletionSource<boolean>;
    private static connectTimeOutId;

    private static handShake: GoPlay.Core.Protocols.RespHandShake;
    private static requestMap = {};
    private static pushMap = {};
    
    public static get isConnected(): boolean {
        if (!goplay.ws) return false;
        if (goplay.ws.readyState > 1) return false;

        return true;
    }

    public static async connect(host: string, port: number): Promise<boolean> {
        let url = `wss://${host}:${port}/ws`;
        if (goplay.isConnected && goplay.url == url) return;

        if (goplay.isConnected && goplay.url != url) goplay.disconnect();

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
    
    public static disconnect() {
        if (!goplay.ws) return;

        if (goplay.ws.readyState <= 1) goplay.ws.close();
        goplay.ws = null;
        goplay.emit(Consts.Events.DISCONNECTED);
    }

    public static send(pack: Package<any>) {
        goplay.ws.send(pack.encode());
    }

    private static recv(): Package<any> {
        if (!goplay.buffer || !goplay.buffer.length) return null;

        var pack = Package.tryDecodeRaw(goplay.buffer);
        return pack;
    }

    public static onopen(event: Event) {
        console.log("onopen", event)
        goplay.sendHandshake();
    }

    public static onmessage(event: MessageEvent) {
        var data = new ByteArray(event.data);
        // console.log("onmessage", event, data);

        if (!goplay.buffer) {
            goplay.buffer = data;
        } else {
            goplay.buffer = goplay.buffer.writeBytes(data);
        }

        var pack = goplay.recv();
        if (!pack) return;

        var header = pack.header;
        if (header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Ping && 
            header.PackageInfo.Type != GoPlay.Core.Protocols.PackageType.Pong) {
            console.log("Recv: ", header, data);
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

    public static onerror(event: Event) {
        console.log("onerror", event);
        goplay.emit(Consts.Events.ERROR, event);
    }

    public static onclose(event: Event) {
        console.log("onclose", event);
        HeartBeat.stop();
        goplay.disconnect();
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
        console.log("key: ", key);
        console.log("onResponse: ", p);
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
        var key = goplay.getCallbackKey(p.header);
        console.log("key: ", key);
        console.log("onPush: ", p);

        var type = goplay.pushMap[key];
        if (type) {
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

    private static onKick (p: Package<any>) {
        goplay.emit(Consts.Events.KICKED);
        goplay.disconnect();
    }

    private static getCallbackKey (header: GoPlay.Core.Protocols.Header): string {
        return `${header.PackageInfo.Route}-${header.PackageInfo.Id}`;
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