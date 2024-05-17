const encodeStatusCode = $root.GoPlay.Core.Protocols.StatusCode;
const encodePackageType = $root.GoPlay.Core.Protocols.PackageType;
const encodeEncodingType = $root.GoPlay.Core.Protocols.EncodingType;

(function (exports, ByteArray) {
    /* defines */
    var goplay = {};
    var info = {
        "ClientType": "GoPlay/Javascript",
        "ClientVersion": "0.1"
    };
    var events = {
        "CONNECTED"    : "__ON_CONNECTED",
        "DISCONNECTED" : "__ON_DISCONNECTED",
        "ERROR"        : "__ON_ERROR"
    };
    var defaults = {
        "encoding": encodeEncodingType.Protobuf,
    };
    var heartbeat = {
        "TIMEOUT": 3000,
        "INTERNAL": 15000,
        "MAX_TIMEOUT": 3
    };
    var request = {
        "TIMEOUT": 3000
    };
    var pkg = {
        "StatusCode": encodeStatusCode,
        "PackageType": encodePackageType
    }
    var ws = null; //websocket Object

    /* vvvvvv Utility Functions Start vvvvvv */
    function isProtobuf() {
        return defaults.encoding == encodeEncodingType.Protobuf;
    }

    /**
     * Inherit the emitter properties.
     *
     * @param {Object} obj
     * @param {Object} superCls
     * @return {Object}
     * @api private
     */
    function inherit(obj, superCls) {
        for (var key in superCls.prototype) {
            obj[key] = superCls.prototype[key];
        }
        return obj;
    }

    function strencode(str) {
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
                byteArray[offset] = codes[j];
                ++offset;
            }
        }
        var _buffer = new ByteArray(offset);
        copyArray(_buffer, 0, byteArray, 0, offset);
        return _buffer;
    };

    function strdecode(buffer) {
        var bytes = new ByteArray(buffer);
        var array = [];
        var offset = 0;
        var charCode = 0;
        var end = bytes.length;
        while (offset < end) {
            if (bytes[offset] < 128) {
                charCode = bytes[offset];
                offset += 1;
            } else if (bytes[offset] < 224) {
                charCode = ((bytes[offset] & 0x3f) << 6) + (bytes[offset + 1] & 0x3f);
                offset += 2;
            } else {
                charCode = ((bytes[offset] & 0x0f) << 12) + ((bytes[offset + 1] & 0x3f) << 6) + (bytes[offset + 2] & 0x3f);
                offset += 3;
            }
            array.push(charCode);
        }
        return String.fromCharCode.apply(null, array);
    };

    function copyArray(dest, doffset, src, soffset, length) {
        if ('function' === typeof src.copy) {
            // Buffer
            src.copy(dest, doffset, soffset, soffset + length);
            return dest;
        } else {
            // Uint8Array
            var result = dest;
            if (dest.length < (doffset + length)) {
                result = new ByteArray(doffset + length);
            }

            for (var i = 0; i < dest.length; i++) {
                result[i] = dest[i];
            }

            for (var index = 0; index < length; index++) {
                result[doffset++] = src[soffset++];
            }

            return result;
        }
    }

    function IdGen(max) {
        this.id = 0;
        this.max = max;
        return this;
    }

    IdGen.prototype.next = function () {
        if (this.id++ > this.max) {
            this.id = 0;
        }

        return this.id;
    }

    ByteArray.prototype.writeUint8 = function (val) {
        this.woffset = this.woffset || 0;
        this[this.woffset++] = val & 0xff;
        return this;
    }

    ByteArray.prototype.writeUint16 = function (val) {
        this.woffset = this.woffset || 0;
        this[this.woffset++] = (val >> 8) & 0xff;
        this[this.woffset++] = val & 0xff;
        return this;
    }

    ByteArray.prototype.writeUint32 = function (val) {
        this.woffset = this.woffset || 0;
        this[this.woffset++] = (val >> 24) & 0xff;
        this[this.woffset++] = (val >> 16) & 0xff;
        this[this.woffset++] = (val >> 8) & 0xff;
        this[this.woffset++] = val & 0xff;
        return this;
    }

    ByteArray.prototype.writeString = function (val) {
        if (!val || val.length <= 0) return this;

        this.woffset = this.woffset || 0;
        var bytes = strencode(val);
        // console.log(val, bytes, bytes.length); 
        var result = copyArray(this, this.woffset, bytes, 0, bytes.length);
        result.woffset = this.woffset + bytes.length;
        return result;
    }

    ByteArray.prototype.writeBytes = function (data) {
        if (!data || !data.length) return this;

        var result = copyArray(this, this.length, data, 0, data.length);
        result.woffset = this.woffset + data.length;
        return result;
    }

    ByteArray.prototype.hasReadSize = function (len) {
        this.roffset = this.roffset || 0;
        return len <= this.length - this.roffset;
    }

    ByteArray.prototype.readUint8 = function () {
        this.roffset = this.roffset || 0;
        if (this.roffset + 1 > this.length) return undefined;

        var val = this[this.roffset] & 0xff;
        this.roffset += 1;
        return val;
    }

    ByteArray.prototype.readUint16 = function () {
        var h = this.readUint8();
        var l = this.readUint8();
        if (h == undefined || l == undefined) return undefined;

        return h << 8 | l;
    }

    ByteArray.prototype.readUint32 = function () {
        var h = this.readUint16();
        var l = this.readUint16();
        if (h == undefined || l == undefined) return undefined;

        return h << 16 | l;
    }

    ByteArray.prototype.readBytes = function (len) {
        if (len <= 0) return undefined;

        this.roffset = this.roffset || 0;
        if (this.roffset + len > this.length) return undefined;

        var bytes = this.slice(this.roffset, this.roffset + len);
        // console.log(bytes, bytes.length, len);
        this.roffset += len;
        return bytes;
    }

    ByteArray.prototype.readString = function (len) {
        var bytes = this.readBytes(len);
        if (bytes == undefined) return "";

        return strdecode(bytes);
    }
    /* ^^^^^^ Utility Functions End ^^^^^^ */

    /* vvvvvv Encoder Start vvvvvv */
    var jsonEncoder = {
        "encode": function(obj) {
            if(obj == undefined) return obj;

            obj = JSON.stringify(obj);
            return strencode(obj);
        },
        "decode": function(buffer) {
            buffer = strdecode(buffer);
            return JSON.parse(buffer);
        }
    };

    var protobufEncoder = {
        "encode": function(obj) {
            if(obj == undefined) return obj;

            if (!obj.constructor || !obj.constructor.encode) throw new Error("not a protobuf object!");
            // console.log(obj.constructor.name, obj, t);
            return obj.constructor.encode(obj).finish();
        },
        "decode": function(buffer) {
            return buffer;
        }
    };

    function GetEncoder(encoding) {
        switch (encoding) {
            case encodeEncodingType.Json:
                return jsonEncoder;
            case encodeEncodingType.Protobuf:
                return protobufEncoder;
            default:
                console.log("should not be here!!", encoding);
                break;
        }
    }
    /* ^^^^^^ Encoder End ^^^^^^ */

    /* vvvvvv Header Start vvvvvv */
    function Header(type, encoding, id, status, contentSize, route) {
        this.type = type;
        this.encoding = encoding;
        this.id = id;
        this.status = status;
        this.contentSize = contentSize;
        this.route = route;

        return this;
    }

    function header_can_encode_route(header) {
        return header.type != pkg.PackageTypePush;
    }

    function header_size(header) {
        var size = 6; /* static size */
        if ( ! header_can_encode_route(header)) {
            size += (1 + header.route.length);
        } else {
            size += 2;
        }

        return size;
    }

    function header_encode(header) {
        var bytes = new ByteArray(header_size(header));

        bytes = bytes.writeUint8(header.type)
            .writeUint8(header.encoding)
            .writeUint8(header.id)
            .writeUint8(header.status)
            .writeUint16(header.contentSize);
        
        if ( ! header_can_encode_route(header)) {
            bytes = bytes.writeUint8(header.route.length)
                         .writeString(header.route);
        } else {
            var routeEncoded = goplay.getRouteEncoded(header.route);
            bytes = bytes.writeUint16(routeEncoded);
        }

        return bytes;
    }

    function header_decode(bytes) {
        var header = new Header();

        header.type = bytes.readUint8();
        header.encoding = bytes.readUint8();
        header.id = bytes.readUint8();
        header.status = bytes.readUint8();
        header.contentSize = bytes.readUint16();

        if ( ! header_can_encode_route(header)) {
            var routeLen = bytes.readUint8();
            // console.log(routeLen);
            header.route = bytes.readString(routeLen);
        } else {
            var routeEncoded = bytes.readUint16();
            header.route = goplay.getRoute(routeEncoded);
        }

        return header;
    }
    /* ^^^^^^ Header End ^^^^^^ */

    /* vvvvvv Event Emitter Start vvvvvv */
    function Emitter(obj) {
        if (obj) return inherit(obj, Emitter);
    }

    /**
     * Listen on the given `event` with `fn`.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    Emitter.prototype.on =
        Emitter.prototype.addListener =
        Emitter.prototype.addEventListener = function (event, fn) {
            this._callbacks = this._callbacks || {};
            (this._callbacks[event] = this._callbacks[event] || [])
            .push(fn);
            return this;
        };

    /**
     * Adds an `event` listener that will be invoked a single
     * time then automatically removed.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    Emitter.prototype.once = function (event, fn) {
        var self = this;
        this._callbacks = this._callbacks || {};

        function on() {
            self.off(event, on);
            fn.apply(this, arguments);
        }

        on.fn = fn;
        this.on(event, on);
        return this;
    };

    /**
     * Remove the given callback for `event` or all
     * registered callbacks.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    Emitter.prototype.off =
        Emitter.prototype.removeListener =
        Emitter.prototype.removeAllListeners =
        Emitter.prototype.removeEventListener = function (event, fn) {
            this._callbacks = this._callbacks || {};

            // all
            if (0 == arguments.length) {
                this._callbacks = {};
                return this;
            }

            // specific event
            var callbacks = this._callbacks[event];
            if (!callbacks) return this;

            // remove all handlers
            if (1 == arguments.length) {
                delete this._callbacks[event];
                return this;
            }

            // remove specific handler
            var cb;
            for (var i = 0; i < callbacks.length; i++) {
                cb = callbacks[i];
                if (cb === fn || cb.fn === fn) {
                    callbacks.splice(i, 1);
                    break;
                }
            }
            return this;
        };

    /**
     * Emit `event` with the given args.
     *
     * @param {String} event
     * @param {Mixed} ...
     * @return {Emitter}
     */
    Emitter.prototype.emit = function (event) {
        this._callbacks = this._callbacks || {};
        var args = [].slice.call(arguments, 1),
            callbacks = this._callbacks[event];

        if (callbacks) {
            callbacks = callbacks.slice(0);
            for (var i = 0, len = callbacks.length; i < len; ++i) {
                callbacks[i].apply(this, args);
            }
        }

        return this;
    };

    /**
     * Return array of callbacks for `event`.
     *
     * @param {String} event
     * @return {Array}
     * @api public
     */
    Emitter.prototype.listeners = function (event) {
        this._callbacks = this._callbacks || {};
        return this._callbacks[event] || [];
    };

    /**
     * Check if this emitter has `event` handlers.
     *
     * @param {String} event
     * @return {Boolean}
     * @api public
     */
    Emitter.prototype.hasListeners = function (event) {
        return !!this.listeners(event).length;
    };
    /* ^^^^^^^ Event Emitter End ^^^^^^^ */

    goplay.connect = function (host, port) {
        var url = "ws://" + host + ":" + port + "/ws";
        if (goplay.isConnected() && goplay.url == url) return;

        if (goplay.isConnected() && goplay.url != url) goplay.disconnect();

        goplay.url = url;
        ws = new WebSocket(url);
        ws.binaryType = 'arraybuffer';
        ws.onopen = goplay.onopen;
        ws.onmessage = goplay.onmessage;
        ws.onerror = goplay.onerror;
        ws.onclose = goplay.onclose;
    }

    goplay.disconnect = function () {
        if (!ws) return;

        if (ws.readyState <= 1) ws.close();
        ws = null;
        goplay.emit(events.DISCONNECTED);
    }

    goplay.isConnected = function () {
        if (!ws) return false;
        if (ws.readyState > 1) return false;

        return true;
    }

    goplay.send = function (header, data) {
        data = data || undefined;
        // data = strencode(data);
        // console.log("header:", header);
        // console.log("data:", data);
        // var strBuffer = strencode(data);
        header.contentSize = data ? data.length : 0;
        var bytes = header_encode(header);
        // console.log("header_encode: ", bytes);
        // console.log("header_decode: ", header_decode(bytes));
        // bytes = copyArray(bytes, bytes.length, data, 0, data.length);
        if (header.contentSize > 0) bytes = bytes.writeBytes(data);
        //console.log("send:", bytes);
        ws.send(bytes.buffer);
    }

    goplay.recv = function () {
        if (!goplay.buffer || !goplay.buffer.length) return null;

        var header = header_decode(goplay.buffer);
        if (!goplay.buffer.hasReadSize(header.contentSize)) {
            goplay.buffer.roffset -= header_size(header);
            return null;
        }

        var data = new ByteArray(0);
        if (header.contentSize > 0) data = goplay.buffer.readBytes(header.contentSize);

        var start = header_size(header) + header.contentSize;
        goplay.buffer = goplay.buffer.slice(start);

        return {
            "header": header,
            "data": data
        };
    }

    goplay.onopen = function (event) {
        console.log("onopen", event)
        goplay.sendHandshake();
    }

    goplay.onmessage = function (event) {
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
        var data = pack.data;
        if (header.contentSize > 0) {
            var encoder = GetEncoder(pack.header.encoding);
            data = encoder.decode(pack.data);
        }
        if (header.type != pkg.PackageTypePing && header.type != pkg.PackageTypePong) {
            console.log("Recv: ", header, data);
        }
        switch (header.type) {
            case pkg.PackageTypeResponse:
                goplay.onResponse(header, data);
                break;
            case pkg.PackageTypePush:
                goplay.onPush(header, data);
                break;
            case pkg.PackageTypePing:
                header.type = pkg.PackageTypePong;
                goplay.send(header);
                break;
            case pkg.PackageTypePong:
                goplay.onHeartbeat(header, data);
                break;
            case pkg.PackageTypeHankShakeResp:
                goplay.onHandshake(header, data);
                break;
            default:
                console.log("should not be here!!", header, data);
                break;
        }
    }

    goplay.onerror = function (event) {
        console.log("onerror", event);
        goplay.emit(events.ERROR, event);
    }

    goplay.onclose = function (event) {
        console.log("onclose", event);
        goplay.stopHeartbeat();
        goplay.disconnect();
    }

    goplay.getRouteEncoded = function (route) {
        if ( ! goplay.handshake) return 0;

        if (goplay.handshake.Routes.hasOwnProperty(route)) {
            return goplay.handshake.Routes[route];
        }
        
        return 0;
    }

    goplay.getRoute = function (routeEncoded) {
        if ( ! goplay.handshake) return "";

        for (var key in goplay.handshake.Routes) {
            if (goplay.handshake.Routes.hasOwnProperty(key)) {
                if (routeEncoded == goplay.handshake.Routes[key]) {
                    return key;
                }
            }
        }

        return "";
    }

    goplay.sendHandshake = function () {
        goplay.idGen = goplay.idGen || new IdGen(255);
        var id = goplay.idGen.next();
        var encoder = GetEncoder(defaults.encoding);
        var data = {
            "ClientType":    info.ClientType,
            "ClientVersion": info.ClientVersion,
            "DictMd5":       "md5"
        };
        if (isProtobuf()) data = pb.HandShakeClientData.create(data);
        data = encoder.encode(data);
        goplay.send(new Header(
            pkg.PackageTypeHankShakeReq,
            defaults.encoding,
            id,
            pkg.StatusCodeSuccess,
            0,
            ""
        ), data);
    }

    goplay.onHandshake = function (header, data) {
        if (data.IsReconnect) {
            return goplay.connect(data.ReconnectTo.Host, data.ReconnectTo.Port);
        }

        /*
        type HandShakeResponse struct {
            ServerVersion string            `protobuf:"bytes,1,opt,name=ServerVersion" json:"ServerVersion,omitempty"`
            Now           string            `protobuf:"bytes,2,opt,name=Now" json:"Now,omitempty"`
            HeartBeatRate int32             `protobuf:"varint,3,opt,name=HeartBeatRate" json:"HeartBeatRate,omitempty"`
            Routes        map[string]uint32 `protobuf:"bytes,4,rep,name=Routes" json:"Routes,omitempty" protobuf_key:"bytes,1,opt,name=key" protobuf_val:"varint,2,opt,name=value"`
            IsReconnect   bool              `protobuf:"varint,5,opt,name=IsReconnect" json:"IsReconnect,omitempty"`
            ReconnectTo   *HostPort         `protobuf:"bytes,6,opt,name=ReconnectTo" json:"ReconnectTo,omitempty"`
        }
         */
        if (isProtobuf()) data = pb.HandShakeResponse.decode(data);
        goplay.handshake = data;
        heartbeat.INTERNAL = data.HeartBeatRate;
        goplay.startHeartbeat();
        goplay.emit(events.CONNECTED);
    }

    goplay.startHeartbeat = function () {
        goplay.heartbeat = {
            idGen: new IdGen(255),
            intervalId: setInterval(function () {
                var id = goplay.heartbeat.idGen.next();
                goplay.send(new Header(pkg.PackageTypePing, defaults.encoding, id, pkg.StatusCodeSuccess, 0, ""));
                goplay.heartbeat.timeOutMap[id] = setTimeout(function () {
                    ++goplay.heartbeat.timeOutCount;
                    if (goplay.heartbeat.timeOutCount > heartbeat.MAX_TIMEOUT) {
                        console.log("heartbeat timeout count > " + heartbeat.MAX_TIMEOUT);
                        goplay.disconnect();
                    }
                }, heartbeat.TIMEOUT);
            }, heartbeat.INTERNAL),
            timeOutMap: {},
            timeOutCount: 0
        };
    }

    goplay.stopHeartbeat = function () {
        if (!goplay.heartbeat || !goplay.heartbeat.intervalId) return;
        clearInterval(goplay.heartbeat.intervalId);
        for (var key in goplay.heartbeat.timeOutMap) {
            if (!goplay.heartbeat.timeOutMap.hasOwnProperty(key)) continue;

            var timeOutId = goplay.heartbeat.timeOutMap[key];
            clearTimeout(timeOutId);
        }

        goplay.heartbeat = null;
    }

    goplay.onHeartbeat = function (header, data) {
        var timeOutId = goplay.heartbeat.timeOutMap[header.id];
        clearTimeout(timeOutId);
        goplay.heartbeat.timeOutCount = 0
    }

    goplay.onResponse = function (header, data) {
        console.log("key: ", goplay.getCallbackKey(header));
        console.log("onResponse: ", header, data);
        goplay.emit(goplay.getCallbackKey(header), header.status, data);
    }

    goplay.onPush = function (header, data) {
        goplay.emit(header.route, data);
    }

    goplay.getCallbackKey = function (header) {
        return header.route + "-" + header.id;
    }

    //arguments[4] => route, data, successCb, failCb
    //arguments[3] => route, successCb, failCb
    goplay.request = function () {
        goplay.idGen = goplay.idGen || new IdGen(255);
        var route, data, successCb, failCb;
        if(arguments.length == 4) {
            route = arguments[0];
            data = arguments[1];
            successCb = arguments[2];
            failCb = arguments[3];
        } else if(arguments.length == 3) {
            route = arguments[0];
            successCb = arguments[1];
            failCb = arguments[2];
        } else {
            throw new Error("arguments length not valid!");
        }

        var header = new Header(pkg.PackageTypeRequest, defaults.encoding, goplay.idGen.next(), pkg.StatusCodeSuccess, 0, route);
        var encoder = GetEncoder(header.encoding);
        data = encoder.encode(data);

        var key = goplay.getCallbackKey(header);
        // console.log("key: ", key);
        var timeOutId = setTimeout(function () {
            goplay.emit(key, pkg.StatusCodeTimeout, {
                "Code": pkg.StatusCodeTimeout,
                "Message": "request time out"
            });
        }, request.TIMEOUT);
        goplay.once(key, function (status, data) {
            clearTimeout(timeOutId);

            if (status == pkg.StatusCodeSuccess) {
                successCb(data);
            } else {
                if (isProtobuf()) data = pb.ErrorMessage.decode(data);
                failCb(data);
            }
        });

        goplay.send(header, data);
    }

    //arguments[2] => route, data
    //arguments[1] => route
    goplay.notify = function () {
        goplay.idGen = goplay.idGen || new IdGen(255);
        var route, data;
        if(arguments.length == 2) {
            route = arguments[0];
            data = arguments[1];
        } else if(arguments.length == 1) {
            route = arguments[0];
        } else {
            throw new Error("arguments length not valid!");
        }

        var header = new Header(pkg.PackageTypeNotify, defaults.encoding, goplay.idGen.next(), pkg.StatusCodeSuccess, 0, route);
        var encoder = GetEncoder(header.encoding);
        data = encoder.encode(data);

        goplay.send(header, data);
    }

    goplay = Emitter(goplay);
    goplay.info = info;
    goplay.pkg = pkg;
    goplay.events = events;
    goplay.defaults = defaults;

    exports.Header = Header;
    exports.Emitter = Emitter;
    exports.goplay = goplay;
})(
    typeof (window) == "undefined" ? module.exports : window,
    typeof (window) == "undefined" ? Buffer : Uint8Array
)