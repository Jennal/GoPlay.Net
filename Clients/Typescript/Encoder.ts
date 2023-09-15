import { ByteArray } from "./ByteArray";
import { GoPlay } from "./pkg";

export interface IEncoder {
    encode(obj: any): ByteArray;
    decode<T>(type: { new(): T }, bytes: ByteArray): T;
}

export class ProtobufEncoder implements IEncoder {
    encode(obj: any): ByteArray {
        if(obj == undefined) return obj;

        if (!obj.constructor || !obj.constructor.encode) throw new Error("not a protobuf object!");
        // console.log(obj.constructor.name, obj, t);
        return obj.constructor.encode(obj).finish();
    }
    decode<T>(type: { new(): T }, bytes: ByteArray): T {
        if (!type.prototype.decode) throw new Error("not a protobuf type!");
        return type.prototype.decode(bytes);
    }
}

const protobufEncoder = new ProtobufEncoder();

export function getEncoder(type: GoPlay.Core.Protocols.EncodingType): IEncoder {
    switch (type) {
        case GoPlay.Core.Protocols.EncodingType.Protobuf:
            return protobufEncoder;
        default:
            throw new Error(`not supported encoding type: ${type}`);
    }
}