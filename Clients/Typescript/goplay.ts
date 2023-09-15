import { GoPlay } from './pkg';

const Consts = {
    Info: {
        "ClientType": "GoPlay/Javascript",
        "ClientVersion": "0.1"
    }
};

export default class goplay {
    private static ws: WebSocket;
    private static isConnected: boolean;
    private static url: string;
    private static buffer: Uint8Array;

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
    }
    
    public static disconnect() {
        
    }

    public static onopen(event: Event) {

    }

    public static onmessage(event: Event) {

    }

    public static onerror(event: Event) {

    }

    public static onclose(event: Event) {

    }
}