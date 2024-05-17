var $Reader = $protobuf.Reader, $Writer = $protobuf.Writer, $util = $protobuf.util;

var $root = $protobuf.roots["default"] || ($protobuf.roots["default"] = {});

$root.proto = (function() {

    var proto = {};

    proto.String = (function() {

        function String(p) {
            if (p)
                for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                    if (p[ks[i]] != null)
                        this[ks[i]] = p[ks[i]];
        }

        String.prototype.Value = "";

        String.create = function create(properties) {
            return new String(properties);
        };

        String.encode = function encode(m, w) {
            if (!w)
                w = $Writer.create();
            if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                w.uint32(10).string(m.Value);
            return w;
        };

        String.encodeDelimited = function encodeDelimited(message, writer) {
            return this.encode(message, writer).ldelim();
        };

        String.decode = function decode(r, l) {
            if (!(r instanceof $Reader))
                r = $Reader.create(r);
            var c = l === undefined ? r.len : r.pos + l, m = new $root.proto.String();
            while (r.pos < c) {
                var t = r.uint32();
                switch (t >>> 3) {
                case 1: {
                        m.Value = r.string();
                        break;
                    }
                default:
                    r.skipType(t & 7);
                    break;
                }
            }
            return m;
        };

        String.decodeDelimited = function decodeDelimited(reader) {
            if (!(reader instanceof $Reader))
                reader = new $Reader(reader);
            return this.decode(reader, reader.uint32());
        };

        String.verify = function verify(m) {
            if (typeof m !== "object" || m === null)
                return "object expected";
            if (m.Value != null && m.hasOwnProperty("Value")) {
                if (!$util.isString(m.Value))
                    return "Value: string expected";
            }
            return null;
        };

        String.fromObject = function fromObject(d) {
            if (d instanceof $root.proto.String)
                return d;
            var m = new $root.proto.String();
            if (d.Value != null) {
                m.Value = String(d.Value);
            }
            return m;
        };

        String.toObject = function toObject(m, o) {
            if (!o)
                o = {};
            var d = {};
            if (o.defaults) {
                d.Value = "";
            }
            if (m.Value != null && m.hasOwnProperty("Value")) {
                d.Value = m.Value;
            }
            return d;
        };

        String.prototype.toJSON = function toJSON() {
            return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
        };

        String.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
            if (typeUrlPrefix === undefined) {
                typeUrlPrefix = "type.googleapis.com";
            }
            return typeUrlPrefix + "/proto.String";
        };

        return String;
    })();

    proto.Long = (function() {

        function Long(p) {
            if (p)
                for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                    if (p[ks[i]] != null)
                        this[ks[i]] = p[ks[i]];
        }

        Long.prototype.Value = $util.Long ? $util.Long.fromBits(0,0,false) : 0;

        Long.create = function create(properties) {
            return new Long(properties);
        };

        Long.encode = function encode(m, w) {
            if (!w)
                w = $Writer.create();
            if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                w.uint32(8).int64(m.Value);
            return w;
        };

        Long.encodeDelimited = function encodeDelimited(message, writer) {
            return this.encode(message, writer).ldelim();
        };

        Long.decode = function decode(r, l) {
            if (!(r instanceof $Reader))
                r = $Reader.create(r);
            var c = l === undefined ? r.len : r.pos + l, m = new $root.proto.Long();
            while (r.pos < c) {
                var t = r.uint32();
                switch (t >>> 3) {
                case 1: {
                        m.Value = r.int64();
                        break;
                    }
                default:
                    r.skipType(t & 7);
                    break;
                }
            }
            return m;
        };

        Long.decodeDelimited = function decodeDelimited(reader) {
            if (!(reader instanceof $Reader))
                reader = new $Reader(reader);
            return this.decode(reader, reader.uint32());
        };

        Long.verify = function verify(m) {
            if (typeof m !== "object" || m === null)
                return "object expected";
            if (m.Value != null && m.hasOwnProperty("Value")) {
                if (!$util.isInteger(m.Value) && !(m.Value && $util.isInteger(m.Value.low) && $util.isInteger(m.Value.high)))
                    return "Value: integer|Long expected";
            }
            return null;
        };

        Long.fromObject = function fromObject(d) {
            if (d instanceof $root.proto.Long)
                return d;
            var m = new $root.proto.Long();
            if (d.Value != null) {
                if ($util.Long)
                    (m.Value = $util.Long.fromValue(d.Value)).unsigned = false;
                else if (typeof d.Value === "string")
                    m.Value = parseInt(d.Value, 10);
                else if (typeof d.Value === "number")
                    m.Value = d.Value;
                else if (typeof d.Value === "object")
                    m.Value = new $util.LongBits(d.Value.low >>> 0, d.Value.high >>> 0).toNumber();
            }
            return m;
        };

        Long.toObject = function toObject(m, o) {
            if (!o)
                o = {};
            var d = {};
            if (o.defaults) {
                if ($util.Long) {
                    var n = new $util.Long(0, 0, false);
                    d.Value = o.longs === String ? n.toString() : o.longs === Number ? n.toNumber() : n;
                } else
                    d.Value = o.longs === String ? "0" : 0;
            }
            if (m.Value != null && m.hasOwnProperty("Value")) {
                if (typeof m.Value === "number")
                    d.Value = o.longs === String ? String(m.Value) : m.Value;
                else
                    d.Value = o.longs === String ? $util.Long.prototype.toString.call(m.Value) : o.longs === Number ? new $util.LongBits(m.Value.low >>> 0, m.Value.high >>> 0).toNumber() : m.Value;
            }
            return d;
        };

        Long.prototype.toJSON = function toJSON() {
            return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
        };

        Long.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
            if (typeUrlPrefix === undefined) {
                typeUrlPrefix = "type.googleapis.com";
            }
            return typeUrlPrefix + "/proto.Long";
        };

        return Long;
    })();

    return proto;
})();