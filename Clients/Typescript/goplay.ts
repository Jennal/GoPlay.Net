import { GoPlay } from './pkg';

const Consts = {
    Info: {
        "ClientType": "GoPlay/Javascript",
        "ClientVersion": "0.1"
    }
};

export default class goplay {
    private static ws: WebSocket;

    public static async connect(host: string, port: number) {
        goplay.ws = new WebSocket(`wss://${host}:${port}/ws`);

        
    }
}