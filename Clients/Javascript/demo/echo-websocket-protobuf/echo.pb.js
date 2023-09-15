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
            if (m.Value != null && m.hasOwnProperty("Value"))
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
                case 1:
                    m.Value = r.string();
                    break;
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

        return String;
    })();

    return proto;
})();