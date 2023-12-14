import { GoPlay } from "./pkg.pb";
import { getEncoder } from "./Encoder";
import { ByteArray } from "./ByteArray";
import IdGen from "./IdGen";

let idGen = new IdGen(255);
const MAX_CHUNK_SIZE = 65535 - 2048;

export default class Package<T> {
    public header: GoPlay.Core.Protocols.Header;
    public data: T;
    public rawData: ByteArray;

    constructor(header: GoPlay.Core.Protocols.Header, data: any, rawData: ByteArray | null) {
        this.header = header;
        this.data = data;
        this.rawData = rawData;
    }

    public updateContentSize() {
        let encoder = getEncoder(this.header.PackageInfo.EncodingType);
        if (!this.rawData && this.data) {
            this.rawData = encoder.encode(this.data);
        }
        this.header.PackageInfo.ContentSize = this.rawData?.length || 0;
    }

    public encode(): ByteArray {
        this.updateContentSize();
        let encoder = getEncoder(this.header.PackageInfo.EncodingType);
        let headerBytes = encoder.encode(this.header);
        
        let bytes = new ByteArray(2 + headerBytes.length + this.header.PackageInfo.ContentSize);
        bytes = bytes.writeUint16(headerBytes.length);
        bytes = bytes.writeBytes(headerBytes);
        bytes = bytes.writeBytes(this.rawData);
        
        return bytes;
    }

    public decodeFromRaw<T>(type: { new(): T }): Package<T> {
        if (!this.rawData) return new Package<T>(this.header, null, this.rawData);

        let encoder = getEncoder(this.header.PackageInfo.EncodingType);
        let data = encoder.decode<T>(type, this.rawData);
        return new Package<T>(this.header, data, this.rawData);
    }

    public split(): Package<any>[] {
        this.updateContentSize();

        if (this.header.PackageInfo.ContentSize <= MAX_CHUNK_SIZE)
        {
            return [this];
        }

        let arr = [];
        let header = JSON.parse(JSON.stringify(this.header)); //clone
        header.PackageInfo.ChunkCount = Math.ceil(header.PackageInfo.ContentSize / MAX_CHUNK_SIZE);
        let chunkSize = MAX_CHUNK_SIZE;
        for (let start=0, i=0; start < this.rawData.length; start += chunkSize, i++)
        {
            let size = Math.min(chunkSize, this.rawData.length - start);
            let chunk = this.rawData.slice(start, start + size);
            header.PackageInfo.ChunkIndex = i;
            header.PackageInfo.ContentSize = chunk.length;

            arr.push(Package.createRaw(GoPlay.Core.Protocols.Header.fromObject(header), new ByteArray(chunk)));
        }

        return arr;
    }

    public static join(packages: Package<any>[]): Package<any> {
        if (packages.length <= 0) return null;

        let header = GoPlay.Core.Protocols.Header.create(packages[0].header);
        let rawData = new ByteArray(packages.reduce((total, pkg) => total + pkg.rawData.length, 0));
        packages = packages.sort((a, b) => a.header.PackageInfo.ChunkIndex - b.header.PackageInfo.ChunkIndex);
        for (let i=0; i<packages.length; i++)
        {
            let pkg = packages[i];
            console.log(pkg.header.PackageInfo.ChunkIndex, i);
            if (pkg.header.PackageInfo.ChunkIndex != i)
            {
                return null;
            }
            rawData = rawData.writeBytes(pkg.rawData);
        }
        header.PackageInfo.ChunkCount = 1;
        header.PackageInfo.ChunkIndex = 0;
        header.PackageInfo.ContentSize = rawData.length;
        return Package.createRaw(header, rawData);
    }

    public static tryDecodeRaw(bytes: ByteArray): Package<any> {
        let encoder = getEncoder(GoPlay.Core.Protocols.EncodingType.Protobuf);
        
        if (!bytes.hasReadSize(2)) return null;
        let headerLength = bytes.readUint16();
        if (!bytes.hasReadSize(headerLength)) {
            bytes.roffset -= 2;
            return null;
        }
        let headerBytes = bytes.readBytes(headerLength);
        let header = encoder.decode(GoPlay.Core.Protocols.Header, headerBytes) as GoPlay.Core.Protocols.Header;
    
        if (!bytes.hasReadSize(header.PackageInfo.ContentSize)) {
            bytes.roffset -= (headerLength + 2);
            return null;
        }
        var dataBytes = bytes.readBytes(header.PackageInfo.ContentSize);
        
        return new Package(header, null, dataBytes);
    }

    public static decode<T>(type: { new(): T }, bytes: ByteArray): Package<T> {
        let encoder = getEncoder(GoPlay.Core.Protocols.EncodingType.Protobuf);
        
        let headerLength = bytes.readUint16();
        let headerBytes = bytes.readBytes(headerLength);
        let header = encoder.decode(GoPlay.Core.Protocols.Header, headerBytes) as GoPlay.Core.Protocols.Header;

        encoder = getEncoder(header.PackageInfo.EncodingType);
        var dataBytes = bytes.readBytes(header.PackageInfo.ContentSize);
        let data = encoder.decode(type, dataBytes);
        
        return new Package(header, data, dataBytes);
    }

    public static create<T>(header: GoPlay.Core.Protocols.Header, data: T): Package<T> {
        return new Package(header, data, null);
    }

    public static createRaw(header: GoPlay.Core.Protocols.Header, rawData: ByteArray): Package<any> {
        return new Package(header, null, rawData);
    }

    public static createFromData<T>(route: number, data: T, type: GoPlay.Core.Protocols.PackageType, encoding: GoPlay.Core.Protocols.EncodingType): Package<T>
    {
        let header = new GoPlay.Core.Protocols.Header();
        header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
        header.PackageInfo.Route = route;
        header.PackageInfo.EncodingType = encoding;
        header.PackageInfo.Type = type;
        header.PackageInfo.Id = idGen.next();
        return Package.create(header, data);
    }
}