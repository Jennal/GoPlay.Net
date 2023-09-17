export function strencode(str) {
    var byteArray = new ByteArray(str.length * 3);
    var offset = 0;
    for (var i = 0; i < str.length; i++) {
        var charCode = str.charCodeAt(i);
        var codes = null;
        if (charCode <= 0x7f) {
            codes = [charCode];
        } else if (charCode <= 0x7ff) {
            codes = [0xc0 | (charCode >> 6), 0x80 | (charCode & 0x3f)];
        } else {
            codes = [0xe0 | (charCode >> 12), 0x80 | ((charCode & 0xfc0) >> 6), 0x80 | (charCode & 0x3f)];
        }
        for (var j = 0; j < codes.length; j++) {
            byteArray.data[offset] = codes[j];
            ++offset;
        }
    }
    var _buffer = new ByteArray(offset);
    copyArray(_buffer, 0, byteArray, 0, offset);
    return _buffer;
};

export function strdecode(buffer) {
    var bytes = new ByteArray(buffer);
    var array = [];
    var offset = 0;
    var charCode = 0;
    var end = bytes.length;
    while (offset < end) {
        if (bytes.data[offset] < 128) {
            charCode = bytes.data[offset];
            offset += 1;
        } else if (bytes.data[offset] < 224) {
            charCode = ((bytes.data[offset] & 0x3f) << 6) + (bytes.data[offset + 1] & 0x3f);
            offset += 2;
        } else {
            charCode = ((bytes.data[offset] & 0x0f) << 12) + ((bytes.data[offset + 1] & 0x3f) << 6) + (bytes.data[offset + 2] & 0x3f);
            offset += 3;
        }
        array.push(charCode);
    }
    return String.fromCharCode.apply(null, array);
};

export function copyArray(dest, doffset, src, soffset, length) {
    // Uint8Array
    var result = dest;
    if (dest.length < (doffset + length)) {
        result = new ByteArray(doffset + length);
        result.woffset = dest.woffset;
        result.roffset = dest.roffset;
    }

    for (var i = 0; i < dest.length; i++) {
        result.data[i] = dest.data[i];
    }

    for (var index = 0; index < length; index++) {
        result.data[doffset++] = src.data[soffset++];
    }

    return result;
}

export class ByteArray {
    public woffset = 0;
    public roffset = 0;
    public data: Uint8Array;

    public constructor(data?: Uint8Array | ByteArray | number) {
        if (data instanceof Uint8Array) {
            this.data = data;
        } else if (data instanceof ByteArray) {
            this.data = new Uint8Array(data.buffer);
        } else {
            this.data = new Uint8Array(data as number);
        }
    }

    public get buffer (): ArrayBufferLike {
        return this.data.buffer;
    }

    public get length (): number {
        return this.data.length;
    }

    public slice (start, end) {
        return this.data.slice(start, end);
    }

    public writeUint8 (val) {
        this.data[this.woffset++] = val & 0xff;
        return this;
    }

    public writeUint16 (val) {
        this.data[this.woffset++] = val & 0xff;
        this.data[this.woffset++] = (val >> 8) & 0xff;
        return this;
    }

    public writeUint32 (val) {
        this.data[this.woffset++] = val & 0xff;
        this.data[this.woffset++] = (val >> 8) & 0xff;
        this.data[this.woffset++] = (val >> 16) & 0xff;
        this.data[this.woffset++] = (val >> 24) & 0xff;
        return this;
    }

    public writeString (val) {
        if (!val || val.length <= 0) return this;

        var bytes = strencode(val);
        // console.log(val, bytes, bytes.length); 
        var result = copyArray(this, this.woffset, bytes, 0, bytes.length);
        result.woffset = this.woffset + bytes.length;
        return result;
    }

    public writeBytes (data) {
        if (!data || !data.length) return this;

        var result = copyArray(this, this.woffset, data, 0, data.length);
        result.woffset = this.woffset + data.length;
        return result;
    }

    public hasReadSize (len) {
        return len <= this.length - this.roffset;
    }

    public readUint8 () {
        if (this.roffset + 1 > this.length) return undefined;

        var val = this.data[this.roffset] & 0xff;
        this.roffset += 1;
        return val;
    }

    public readUint16 () {
        var l = this.readUint8();
        var h = this.readUint8();
        if (h == undefined || l == undefined) return undefined;

        return h << 8 | l;
    }

    public readUint32 () {
        var l = this.readUint16();
        var h = this.readUint16();
        if (h == undefined || l == undefined) return undefined;

        return h << 16 | l;
    }

    public readBytes (len): ByteArray {
        if (!len || len <= 0) return undefined;

        if (this.roffset + len > this.length) return undefined;

        var bytes = this.slice(this.roffset, this.roffset + len);
        // console.log(bytes, bytes.length, len);
        this.roffset += len;
        return new ByteArray(bytes);
    }

    public readString (len) {
        var bytes = this.readBytes(len);
        if (bytes == undefined) return "";

        return strdecode(bytes);
    }
}