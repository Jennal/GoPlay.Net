var $Reader = $protobuf.Reader, $Writer = $protobuf.Writer, $util = $protobuf.util;

var $root = $protobuf.roots["default"] || ($protobuf.roots["default"] = {});

$root.GoPlay = (function() {

    var GoPlay = {};

    GoPlay.Core = (function() {

        var Core = {};

        Core.Protocols = (function() {

            var Protocols = {};

            Protocols.PbAny = (function() {

                function PbAny(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbAny.prototype.Value = null;

                PbAny.create = function create(properties) {
                    return new PbAny(properties);
                };

                PbAny.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        $root.google.protobuf.Any.encode(m.Value, w.uint32(10).fork()).ldelim();
                    return w;
                };

                PbAny.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbAny.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbAny();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Value = $root.google.protobuf.Any.decode(r, r.uint32());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbAny.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbAny.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        {
                            var e = $root.google.protobuf.Any.verify(m.Value);
                            if (e)
                                return "Value." + e;
                        }
                    }
                    return null;
                };

                PbAny.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbAny)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbAny();
                    if (d.Value != null) {
                        if (typeof d.Value !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.PbAny.Value: object expected");
                        m.Value = $root.google.protobuf.Any.fromObject(d.Value);
                    }
                    return m;
                };

                PbAny.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Value = null;
                    }
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        d.Value = $root.google.protobuf.Any.toObject(m.Value, o);
                    }
                    return d;
                };

                PbAny.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbAny.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbAny";
                };

                return PbAny;
            })();

            Protocols.PbTime = (function() {

                function PbTime(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbTime.prototype.Value = null;

                PbTime.create = function create(properties) {
                    return new PbTime(properties);
                };

                PbTime.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        $root.google.protobuf.Timestamp.encode(m.Value, w.uint32(10).fork()).ldelim();
                    return w;
                };

                PbTime.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbTime.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbTime();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Value = $root.google.protobuf.Timestamp.decode(r, r.uint32());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbTime.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbTime.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        {
                            var e = $root.google.protobuf.Timestamp.verify(m.Value);
                            if (e)
                                return "Value." + e;
                        }
                    }
                    return null;
                };

                PbTime.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbTime)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbTime();
                    if (d.Value != null) {
                        if (typeof d.Value !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.PbTime.Value: object expected");
                        m.Value = $root.google.protobuf.Timestamp.fromObject(d.Value);
                    }
                    return m;
                };

                PbTime.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Value = null;
                    }
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        d.Value = $root.google.protobuf.Timestamp.toObject(m.Value, o);
                    }
                    return d;
                };

                PbTime.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbTime.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbTime";
                };

                return PbTime;
            })();

            Protocols.PbString = (function() {

                function PbString(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbString.prototype.Value = "";

                PbString.create = function create(properties) {
                    return new PbString(properties);
                };

                PbString.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        w.uint32(10).string(m.Value);
                    return w;
                };

                PbString.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbString.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbString();
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

                PbString.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbString.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!$util.isString(m.Value))
                            return "Value: string expected";
                    }
                    return null;
                };

                PbString.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbString)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbString();
                    if (d.Value != null) {
                        m.Value = String(d.Value);
                    }
                    return m;
                };

                PbString.toObject = function toObject(m, o) {
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

                PbString.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbString.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbString";
                };

                return PbString;
            })();

            Protocols.PbInt = (function() {

                function PbInt(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbInt.prototype.Value = 0;

                PbInt.create = function create(properties) {
                    return new PbInt(properties);
                };

                PbInt.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        w.uint32(8).int32(m.Value);
                    return w;
                };

                PbInt.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbInt.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbInt();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Value = r.int32();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbInt.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbInt.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!$util.isInteger(m.Value))
                            return "Value: integer expected";
                    }
                    return null;
                };

                PbInt.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbInt)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbInt();
                    if (d.Value != null) {
                        m.Value = d.Value | 0;
                    }
                    return m;
                };

                PbInt.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Value = 0;
                    }
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        d.Value = m.Value;
                    }
                    return d;
                };

                PbInt.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbInt.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbInt";
                };

                return PbInt;
            })();

            Protocols.PbLong = (function() {

                function PbLong(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbLong.prototype.Value = $util.Long ? $util.Long.fromBits(0,0,false) : 0;

                PbLong.create = function create(properties) {
                    return new PbLong(properties);
                };

                PbLong.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        w.uint32(8).int64(m.Value);
                    return w;
                };

                PbLong.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbLong.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbLong();
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

                PbLong.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbLong.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!$util.isInteger(m.Value) && !(m.Value && $util.isInteger(m.Value.low) && $util.isInteger(m.Value.high)))
                            return "Value: integer|Long expected";
                    }
                    return null;
                };

                PbLong.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbLong)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbLong();
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

                PbLong.toObject = function toObject(m, o) {
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

                PbLong.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbLong.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbLong";
                };

                return PbLong;
            })();

            Protocols.PbFloat = (function() {

                function PbFloat(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbFloat.prototype.Value = 0;

                PbFloat.create = function create(properties) {
                    return new PbFloat(properties);
                };

                PbFloat.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        w.uint32(13).float(m.Value);
                    return w;
                };

                PbFloat.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbFloat.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbFloat();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Value = r.float();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbFloat.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbFloat.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (typeof m.Value !== "number")
                            return "Value: number expected";
                    }
                    return null;
                };

                PbFloat.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbFloat)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbFloat();
                    if (d.Value != null) {
                        m.Value = Number(d.Value);
                    }
                    return m;
                };

                PbFloat.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Value = 0;
                    }
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        d.Value = o.json && !isFinite(m.Value) ? String(m.Value) : m.Value;
                    }
                    return d;
                };

                PbFloat.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbFloat.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbFloat";
                };

                return PbFloat;
            })();

            Protocols.PbBool = (function() {

                function PbBool(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbBool.prototype.Value = false;

                PbBool.create = function create(properties) {
                    return new PbBool(properties);
                };

                PbBool.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && Object.hasOwnProperty.call(m, "Value"))
                        w.uint32(8).bool(m.Value);
                    return w;
                };

                PbBool.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbBool.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbBool();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Value = r.bool();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbBool.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbBool.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (typeof m.Value !== "boolean")
                            return "Value: boolean expected";
                    }
                    return null;
                };

                PbBool.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbBool)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbBool();
                    if (d.Value != null) {
                        m.Value = Boolean(d.Value);
                    }
                    return m;
                };

                PbBool.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Value = false;
                    }
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        d.Value = m.Value;
                    }
                    return d;
                };

                PbBool.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbBool.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbBool";
                };

                return PbBool;
            })();

            Protocols.PbStringArray = (function() {

                function PbStringArray(p) {
                    this.Value = [];
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbStringArray.prototype.Value = $util.emptyArray;

                PbStringArray.create = function create(properties) {
                    return new PbStringArray(properties);
                };

                PbStringArray.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && m.Value.length) {
                        for (var i = 0; i < m.Value.length; ++i)
                            w.uint32(10).string(m.Value[i]);
                    }
                    return w;
                };

                PbStringArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbStringArray.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbStringArray();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                if (!(m.Value && m.Value.length))
                                    m.Value = [];
                                m.Value.push(r.string());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbStringArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbStringArray.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!Array.isArray(m.Value))
                            return "Value: array expected";
                        for (var i = 0; i < m.Value.length; ++i) {
                            if (!$util.isString(m.Value[i]))
                                return "Value: string[] expected";
                        }
                    }
                    return null;
                };

                PbStringArray.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbStringArray)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbStringArray();
                    if (d.Value) {
                        if (!Array.isArray(d.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbStringArray.Value: array expected");
                        m.Value = [];
                        for (var i = 0; i < d.Value.length; ++i) {
                            m.Value[i] = String(d.Value[i]);
                        }
                    }
                    return m;
                };

                PbStringArray.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.arrays || o.defaults) {
                        d.Value = [];
                    }
                    if (m.Value && m.Value.length) {
                        d.Value = [];
                        for (var j = 0; j < m.Value.length; ++j) {
                            d.Value[j] = m.Value[j];
                        }
                    }
                    return d;
                };

                PbStringArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbStringArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbStringArray";
                };

                return PbStringArray;
            })();

            Protocols.PbIntArray = (function() {

                function PbIntArray(p) {
                    this.Value = [];
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbIntArray.prototype.Value = $util.emptyArray;

                PbIntArray.create = function create(properties) {
                    return new PbIntArray(properties);
                };

                PbIntArray.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && m.Value.length) {
                        w.uint32(10).fork();
                        for (var i = 0; i < m.Value.length; ++i)
                            w.int32(m.Value[i]);
                        w.ldelim();
                    }
                    return w;
                };

                PbIntArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbIntArray.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbIntArray();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                if (!(m.Value && m.Value.length))
                                    m.Value = [];
                                if ((t & 7) === 2) {
                                    var c2 = r.uint32() + r.pos;
                                    while (r.pos < c2)
                                        m.Value.push(r.int32());
                                } else
                                    m.Value.push(r.int32());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbIntArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbIntArray.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!Array.isArray(m.Value))
                            return "Value: array expected";
                        for (var i = 0; i < m.Value.length; ++i) {
                            if (!$util.isInteger(m.Value[i]))
                                return "Value: integer[] expected";
                        }
                    }
                    return null;
                };

                PbIntArray.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbIntArray)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbIntArray();
                    if (d.Value) {
                        if (!Array.isArray(d.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbIntArray.Value: array expected");
                        m.Value = [];
                        for (var i = 0; i < d.Value.length; ++i) {
                            m.Value[i] = d.Value[i] | 0;
                        }
                    }
                    return m;
                };

                PbIntArray.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.arrays || o.defaults) {
                        d.Value = [];
                    }
                    if (m.Value && m.Value.length) {
                        d.Value = [];
                        for (var j = 0; j < m.Value.length; ++j) {
                            d.Value[j] = m.Value[j];
                        }
                    }
                    return d;
                };

                PbIntArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbIntArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbIntArray";
                };

                return PbIntArray;
            })();

            Protocols.PbFloatArray = (function() {

                function PbFloatArray(p) {
                    this.Value = [];
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbFloatArray.prototype.Value = $util.emptyArray;

                PbFloatArray.create = function create(properties) {
                    return new PbFloatArray(properties);
                };

                PbFloatArray.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && m.Value.length) {
                        w.uint32(10).fork();
                        for (var i = 0; i < m.Value.length; ++i)
                            w.float(m.Value[i]);
                        w.ldelim();
                    }
                    return w;
                };

                PbFloatArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbFloatArray.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbFloatArray();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                if (!(m.Value && m.Value.length))
                                    m.Value = [];
                                if ((t & 7) === 2) {
                                    var c2 = r.uint32() + r.pos;
                                    while (r.pos < c2)
                                        m.Value.push(r.float());
                                } else
                                    m.Value.push(r.float());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbFloatArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbFloatArray.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!Array.isArray(m.Value))
                            return "Value: array expected";
                        for (var i = 0; i < m.Value.length; ++i) {
                            if (typeof m.Value[i] !== "number")
                                return "Value: number[] expected";
                        }
                    }
                    return null;
                };

                PbFloatArray.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbFloatArray)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbFloatArray();
                    if (d.Value) {
                        if (!Array.isArray(d.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbFloatArray.Value: array expected");
                        m.Value = [];
                        for (var i = 0; i < d.Value.length; ++i) {
                            m.Value[i] = Number(d.Value[i]);
                        }
                    }
                    return m;
                };

                PbFloatArray.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.arrays || o.defaults) {
                        d.Value = [];
                    }
                    if (m.Value && m.Value.length) {
                        d.Value = [];
                        for (var j = 0; j < m.Value.length; ++j) {
                            d.Value[j] = o.json && !isFinite(m.Value[j]) ? String(m.Value[j]) : m.Value[j];
                        }
                    }
                    return d;
                };

                PbFloatArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbFloatArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbFloatArray";
                };

                return PbFloatArray;
            })();

            Protocols.PbBoolArray = (function() {

                function PbBoolArray(p) {
                    this.Value = [];
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PbBoolArray.prototype.Value = $util.emptyArray;

                PbBoolArray.create = function create(properties) {
                    return new PbBoolArray(properties);
                };

                PbBoolArray.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Value != null && m.Value.length) {
                        w.uint32(10).fork();
                        for (var i = 0; i < m.Value.length; ++i)
                            w.bool(m.Value[i]);
                        w.ldelim();
                    }
                    return w;
                };

                PbBoolArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PbBoolArray.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PbBoolArray();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                if (!(m.Value && m.Value.length))
                                    m.Value = [];
                                if ((t & 7) === 2) {
                                    var c2 = r.uint32() + r.pos;
                                    while (r.pos < c2)
                                        m.Value.push(r.bool());
                                } else
                                    m.Value.push(r.bool());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PbBoolArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PbBoolArray.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Value != null && m.hasOwnProperty("Value")) {
                        if (!Array.isArray(m.Value))
                            return "Value: array expected";
                        for (var i = 0; i < m.Value.length; ++i) {
                            if (typeof m.Value[i] !== "boolean")
                                return "Value: boolean[] expected";
                        }
                    }
                    return null;
                };

                PbBoolArray.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PbBoolArray)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PbBoolArray();
                    if (d.Value) {
                        if (!Array.isArray(d.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbBoolArray.Value: array expected");
                        m.Value = [];
                        for (var i = 0; i < d.Value.length; ++i) {
                            m.Value[i] = Boolean(d.Value[i]);
                        }
                    }
                    return m;
                };

                PbBoolArray.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.arrays || o.defaults) {
                        d.Value = [];
                    }
                    if (m.Value && m.Value.length) {
                        d.Value = [];
                        for (var j = 0; j < m.Value.length; ++j) {
                            d.Value[j] = m.Value[j];
                        }
                    }
                    return d;
                };

                PbBoolArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PbBoolArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbBoolArray";
                };

                return PbBoolArray;
            })();

            Protocols.StatusCode = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Success"] = 0;
                values[valuesById[1] = "Failed"] = 1;
                values[valuesById[2] = "Error"] = 2;
                values[valuesById[3] = "Timeout"] = 3;
                return values;
            })();

            Protocols.PackageType = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "HankShakeReq"] = 0;
                values[valuesById[1] = "HankShakeResp"] = 1;
                values[valuesById[2] = "Ping"] = 2;
                values[valuesById[3] = "Pong"] = 3;
                values[valuesById[4] = "Notify"] = 4;
                values[valuesById[5] = "Request"] = 5;
                values[valuesById[6] = "Response"] = 6;
                values[valuesById[7] = "Push"] = 7;
                values[valuesById[8] = "Kick"] = 8;
                return values;
            })();

            Protocols.EncodingType = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Protobuf"] = 0;
                values[valuesById[1] = "Json"] = 1;
                return values;
            })();

            Protocols.ServerTag = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Empty"] = 0;
                values[valuesById[1] = "FrontEnd"] = 1;
                values[valuesById[2] = "BackEnd"] = 2;
                values[valuesById[3] = "All"] = 3;
                return values;
            })();

            Protocols.Status = (function() {

                function Status(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                Status.prototype.Code = 0;
                Status.prototype.Message = "";

                Status.create = function create(properties) {
                    return new Status(properties);
                };

                Status.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Code != null && Object.hasOwnProperty.call(m, "Code"))
                        w.uint32(8).int32(m.Code);
                    if (m.Message != null && Object.hasOwnProperty.call(m, "Message"))
                        w.uint32(18).string(m.Message);
                    return w;
                };

                Status.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                Status.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.Status();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Code = r.int32();
                                break;
                            }
                        case 2: {
                                m.Message = r.string();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                Status.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                Status.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Code != null && m.hasOwnProperty("Code")) {
                        switch (m.Code) {
                        default:
                            return "Code: enum value expected";
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            break;
                        }
                    }
                    if (m.Message != null && m.hasOwnProperty("Message")) {
                        if (!$util.isString(m.Message))
                            return "Message: string expected";
                    }
                    return null;
                };

                Status.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.Status)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.Status();
                    switch (d.Code) {
                    default:
                        if (typeof d.Code === "number") {
                            m.Code = d.Code;
                            break;
                        }
                        break;
                    case "Success":
                    case 0:
                        m.Code = 0;
                        break;
                    case "Failed":
                    case 1:
                        m.Code = 1;
                        break;
                    case "Error":
                    case 2:
                        m.Code = 2;
                        break;
                    case "Timeout":
                    case 3:
                        m.Code = 3;
                        break;
                    }
                    if (d.Message != null) {
                        m.Message = String(d.Message);
                    }
                    return m;
                };

                Status.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Code = o.enums === String ? "Success" : 0;
                        d.Message = "";
                    }
                    if (m.Code != null && m.hasOwnProperty("Code")) {
                        d.Code = o.enums === String ? $root.GoPlay.Core.Protocols.StatusCode[m.Code] === undefined ? m.Code : $root.GoPlay.Core.Protocols.StatusCode[m.Code] : m.Code;
                    }
                    if (m.Message != null && m.hasOwnProperty("Message")) {
                        d.Message = m.Message;
                    }
                    return d;
                };

                Status.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                Status.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Status";
                };

                return Status;
            })();

            Protocols.Session = (function() {

                function Session(p) {
                    this.Values = {};
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                Session.prototype.Guid = "";
                Session.prototype.Values = $util.emptyObject;

                Session.create = function create(properties) {
                    return new Session(properties);
                };

                Session.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Guid != null && Object.hasOwnProperty.call(m, "Guid"))
                        w.uint32(10).string(m.Guid);
                    if (m.Values != null && Object.hasOwnProperty.call(m, "Values")) {
                        for (var ks = Object.keys(m.Values), i = 0; i < ks.length; ++i) {
                            w.uint32(18).fork().uint32(10).string(ks[i]);
                            $root.google.protobuf.Any.encode(m.Values[ks[i]], w.uint32(18).fork()).ldelim().ldelim();
                        }
                    }
                    return w;
                };

                Session.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                Session.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.Session(), k, value;
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Guid = r.string();
                                break;
                            }
                        case 2: {
                                if (m.Values === $util.emptyObject)
                                    m.Values = {};
                                var c2 = r.uint32() + r.pos;
                                k = "";
                                value = null;
                                while (r.pos < c2) {
                                    var tag2 = r.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        k = r.string();
                                        break;
                                    case 2:
                                        value = $root.google.protobuf.Any.decode(r, r.uint32());
                                        break;
                                    default:
                                        r.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                m.Values[k] = value;
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                Session.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                Session.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Guid != null && m.hasOwnProperty("Guid")) {
                        if (!$util.isString(m.Guid))
                            return "Guid: string expected";
                    }
                    if (m.Values != null && m.hasOwnProperty("Values")) {
                        if (!$util.isObject(m.Values))
                            return "Values: object expected";
                        var k = Object.keys(m.Values);
                        for (var i = 0; i < k.length; ++i) {
                            {
                                var e = $root.google.protobuf.Any.verify(m.Values[k[i]]);
                                if (e)
                                    return "Values." + e;
                            }
                        }
                    }
                    return null;
                };

                Session.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.Session)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.Session();
                    if (d.Guid != null) {
                        m.Guid = String(d.Guid);
                    }
                    if (d.Values) {
                        if (typeof d.Values !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Session.Values: object expected");
                        m.Values = {};
                        for (var ks = Object.keys(d.Values), i = 0; i < ks.length; ++i) {
                            if (typeof d.Values[ks[i]] !== "object")
                                throw TypeError(".GoPlay.Core.Protocols.Session.Values: object expected");
                            m.Values[ks[i]] = $root.google.protobuf.Any.fromObject(d.Values[ks[i]]);
                        }
                    }
                    return m;
                };

                Session.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.objects || o.defaults) {
                        d.Values = {};
                    }
                    if (o.defaults) {
                        d.Guid = "";
                    }
                    if (m.Guid != null && m.hasOwnProperty("Guid")) {
                        d.Guid = m.Guid;
                    }
                    var ks2;
                    if (m.Values && (ks2 = Object.keys(m.Values)).length) {
                        d.Values = {};
                        for (var j = 0; j < ks2.length; ++j) {
                            d.Values[ks2[j]] = $root.google.protobuf.Any.toObject(m.Values[ks2[j]], o);
                        }
                    }
                    return d;
                };

                Session.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                Session.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Session";
                };

                return Session;
            })();

            Protocols.PackageInfo = (function() {

                function PackageInfo(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                PackageInfo.prototype.Type = 0;
                PackageInfo.prototype.Id = 0;
                PackageInfo.prototype.EncodingType = 0;
                PackageInfo.prototype.Route = 0;
                PackageInfo.prototype.ContentSize = 0;

                PackageInfo.create = function create(properties) {
                    return new PackageInfo(properties);
                };

                PackageInfo.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Type != null && Object.hasOwnProperty.call(m, "Type"))
                        w.uint32(8).int32(m.Type);
                    if (m.Id != null && Object.hasOwnProperty.call(m, "Id"))
                        w.uint32(16).uint32(m.Id);
                    if (m.EncodingType != null && Object.hasOwnProperty.call(m, "EncodingType"))
                        w.uint32(24).int32(m.EncodingType);
                    if (m.Route != null && Object.hasOwnProperty.call(m, "Route"))
                        w.uint32(32).uint32(m.Route);
                    if (m.ContentSize != null && Object.hasOwnProperty.call(m, "ContentSize"))
                        w.uint32(40).uint32(m.ContentSize);
                    return w;
                };

                PackageInfo.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                PackageInfo.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.PackageInfo();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Type = r.int32();
                                break;
                            }
                        case 2: {
                                m.Id = r.uint32();
                                break;
                            }
                        case 3: {
                                m.EncodingType = r.int32();
                                break;
                            }
                        case 4: {
                                m.Route = r.uint32();
                                break;
                            }
                        case 5: {
                                m.ContentSize = r.uint32();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                PackageInfo.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                PackageInfo.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Type != null && m.hasOwnProperty("Type")) {
                        switch (m.Type) {
                        default:
                            return "Type: enum value expected";
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            break;
                        }
                    }
                    if (m.Id != null && m.hasOwnProperty("Id")) {
                        if (!$util.isInteger(m.Id))
                            return "Id: integer expected";
                    }
                    if (m.EncodingType != null && m.hasOwnProperty("EncodingType")) {
                        switch (m.EncodingType) {
                        default:
                            return "EncodingType: enum value expected";
                        case 0:
                        case 1:
                            break;
                        }
                    }
                    if (m.Route != null && m.hasOwnProperty("Route")) {
                        if (!$util.isInteger(m.Route))
                            return "Route: integer expected";
                    }
                    if (m.ContentSize != null && m.hasOwnProperty("ContentSize")) {
                        if (!$util.isInteger(m.ContentSize))
                            return "ContentSize: integer expected";
                    }
                    return null;
                };

                PackageInfo.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.PackageInfo)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.PackageInfo();
                    switch (d.Type) {
                    default:
                        if (typeof d.Type === "number") {
                            m.Type = d.Type;
                            break;
                        }
                        break;
                    case "HankShakeReq":
                    case 0:
                        m.Type = 0;
                        break;
                    case "HankShakeResp":
                    case 1:
                        m.Type = 1;
                        break;
                    case "Ping":
                    case 2:
                        m.Type = 2;
                        break;
                    case "Pong":
                    case 3:
                        m.Type = 3;
                        break;
                    case "Notify":
                    case 4:
                        m.Type = 4;
                        break;
                    case "Request":
                    case 5:
                        m.Type = 5;
                        break;
                    case "Response":
                    case 6:
                        m.Type = 6;
                        break;
                    case "Push":
                    case 7:
                        m.Type = 7;
                        break;
                    case "Kick":
                    case 8:
                        m.Type = 8;
                        break;
                    }
                    if (d.Id != null) {
                        m.Id = d.Id >>> 0;
                    }
                    switch (d.EncodingType) {
                    default:
                        if (typeof d.EncodingType === "number") {
                            m.EncodingType = d.EncodingType;
                            break;
                        }
                        break;
                    case "Protobuf":
                    case 0:
                        m.EncodingType = 0;
                        break;
                    case "Json":
                    case 1:
                        m.EncodingType = 1;
                        break;
                    }
                    if (d.Route != null) {
                        m.Route = d.Route >>> 0;
                    }
                    if (d.ContentSize != null) {
                        m.ContentSize = d.ContentSize >>> 0;
                    }
                    return m;
                };

                PackageInfo.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Type = o.enums === String ? "HankShakeReq" : 0;
                        d.Id = 0;
                        d.EncodingType = o.enums === String ? "Protobuf" : 0;
                        d.Route = 0;
                        d.ContentSize = 0;
                    }
                    if (m.Type != null && m.hasOwnProperty("Type")) {
                        d.Type = o.enums === String ? $root.GoPlay.Core.Protocols.PackageType[m.Type] === undefined ? m.Type : $root.GoPlay.Core.Protocols.PackageType[m.Type] : m.Type;
                    }
                    if (m.Id != null && m.hasOwnProperty("Id")) {
                        d.Id = m.Id;
                    }
                    if (m.EncodingType != null && m.hasOwnProperty("EncodingType")) {
                        d.EncodingType = o.enums === String ? $root.GoPlay.Core.Protocols.EncodingType[m.EncodingType] === undefined ? m.EncodingType : $root.GoPlay.Core.Protocols.EncodingType[m.EncodingType] : m.EncodingType;
                    }
                    if (m.Route != null && m.hasOwnProperty("Route")) {
                        d.Route = m.Route;
                    }
                    if (m.ContentSize != null && m.hasOwnProperty("ContentSize")) {
                        d.ContentSize = m.ContentSize;
                    }
                    return d;
                };

                PackageInfo.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                PackageInfo.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PackageInfo";
                };

                return PackageInfo;
            })();

            Protocols.Header = (function() {

                function Header(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                Header.prototype.Status = null;
                Header.prototype.Session = null;
                Header.prototype.PackageInfo = null;

                Header.create = function create(properties) {
                    return new Header(properties);
                };

                Header.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.Status != null && Object.hasOwnProperty.call(m, "Status"))
                        $root.GoPlay.Core.Protocols.Status.encode(m.Status, w.uint32(10).fork()).ldelim();
                    if (m.Session != null && Object.hasOwnProperty.call(m, "Session"))
                        $root.GoPlay.Core.Protocols.Session.encode(m.Session, w.uint32(18).fork()).ldelim();
                    if (m.PackageInfo != null && Object.hasOwnProperty.call(m, "PackageInfo"))
                        $root.GoPlay.Core.Protocols.PackageInfo.encode(m.PackageInfo, w.uint32(26).fork()).ldelim();
                    return w;
                };

                Header.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                Header.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.Header();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.Status = $root.GoPlay.Core.Protocols.Status.decode(r, r.uint32());
                                break;
                            }
                        case 2: {
                                m.Session = $root.GoPlay.Core.Protocols.Session.decode(r, r.uint32());
                                break;
                            }
                        case 3: {
                                m.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.decode(r, r.uint32());
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                Header.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                Header.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.Status != null && m.hasOwnProperty("Status")) {
                        {
                            var e = $root.GoPlay.Core.Protocols.Status.verify(m.Status);
                            if (e)
                                return "Status." + e;
                        }
                    }
                    if (m.Session != null && m.hasOwnProperty("Session")) {
                        {
                            var e = $root.GoPlay.Core.Protocols.Session.verify(m.Session);
                            if (e)
                                return "Session." + e;
                        }
                    }
                    if (m.PackageInfo != null && m.hasOwnProperty("PackageInfo")) {
                        {
                            var e = $root.GoPlay.Core.Protocols.PackageInfo.verify(m.PackageInfo);
                            if (e)
                                return "PackageInfo." + e;
                        }
                    }
                    return null;
                };

                Header.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.Header)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.Header();
                    if (d.Status != null) {
                        if (typeof d.Status !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.Status: object expected");
                        m.Status = $root.GoPlay.Core.Protocols.Status.fromObject(d.Status);
                    }
                    if (d.Session != null) {
                        if (typeof d.Session !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.Session: object expected");
                        m.Session = $root.GoPlay.Core.Protocols.Session.fromObject(d.Session);
                    }
                    if (d.PackageInfo != null) {
                        if (typeof d.PackageInfo !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.PackageInfo: object expected");
                        m.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.fromObject(d.PackageInfo);
                    }
                    return m;
                };

                Header.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.Status = null;
                        d.Session = null;
                        d.PackageInfo = null;
                    }
                    if (m.Status != null && m.hasOwnProperty("Status")) {
                        d.Status = $root.GoPlay.Core.Protocols.Status.toObject(m.Status, o);
                    }
                    if (m.Session != null && m.hasOwnProperty("Session")) {
                        d.Session = $root.GoPlay.Core.Protocols.Session.toObject(m.Session, o);
                    }
                    if (m.PackageInfo != null && m.hasOwnProperty("PackageInfo")) {
                        d.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.toObject(m.PackageInfo, o);
                    }
                    return d;
                };

                Header.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                Header.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Header";
                };

                return Header;
            })();

            Protocols.ReqHankShake = (function() {

                function ReqHankShake(p) {
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                ReqHankShake.prototype.ClientVersion = "";
                ReqHankShake.prototype.ServerTag = 0;
                ReqHankShake.prototype.AppKey = "";

                ReqHankShake.create = function create(properties) {
                    return new ReqHankShake(properties);
                };

                ReqHankShake.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.ClientVersion != null && Object.hasOwnProperty.call(m, "ClientVersion"))
                        w.uint32(10).string(m.ClientVersion);
                    if (m.ServerTag != null && Object.hasOwnProperty.call(m, "ServerTag"))
                        w.uint32(16).int32(m.ServerTag);
                    if (m.AppKey != null && Object.hasOwnProperty.call(m, "AppKey"))
                        w.uint32(26).string(m.AppKey);
                    return w;
                };

                ReqHankShake.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                ReqHankShake.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.ReqHankShake();
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.ClientVersion = r.string();
                                break;
                            }
                        case 2: {
                                m.ServerTag = r.int32();
                                break;
                            }
                        case 3: {
                                m.AppKey = r.string();
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                ReqHankShake.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                ReqHankShake.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.ClientVersion != null && m.hasOwnProperty("ClientVersion")) {
                        if (!$util.isString(m.ClientVersion))
                            return "ClientVersion: string expected";
                    }
                    if (m.ServerTag != null && m.hasOwnProperty("ServerTag")) {
                        switch (m.ServerTag) {
                        default:
                            return "ServerTag: enum value expected";
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            break;
                        }
                    }
                    if (m.AppKey != null && m.hasOwnProperty("AppKey")) {
                        if (!$util.isString(m.AppKey))
                            return "AppKey: string expected";
                    }
                    return null;
                };

                ReqHankShake.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.ReqHankShake)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.ReqHankShake();
                    if (d.ClientVersion != null) {
                        m.ClientVersion = String(d.ClientVersion);
                    }
                    switch (d.ServerTag) {
                    default:
                        if (typeof d.ServerTag === "number") {
                            m.ServerTag = d.ServerTag;
                            break;
                        }
                        break;
                    case "Empty":
                    case 0:
                        m.ServerTag = 0;
                        break;
                    case "FrontEnd":
                    case 1:
                        m.ServerTag = 1;
                        break;
                    case "BackEnd":
                    case 2:
                        m.ServerTag = 2;
                        break;
                    case "All":
                    case 3:
                        m.ServerTag = 3;
                        break;
                    }
                    if (d.AppKey != null) {
                        m.AppKey = String(d.AppKey);
                    }
                    return m;
                };

                ReqHankShake.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.defaults) {
                        d.ClientVersion = "";
                        d.ServerTag = o.enums === String ? "Empty" : 0;
                        d.AppKey = "";
                    }
                    if (m.ClientVersion != null && m.hasOwnProperty("ClientVersion")) {
                        d.ClientVersion = m.ClientVersion;
                    }
                    if (m.ServerTag != null && m.hasOwnProperty("ServerTag")) {
                        d.ServerTag = o.enums === String ? $root.GoPlay.Core.Protocols.ServerTag[m.ServerTag] === undefined ? m.ServerTag : $root.GoPlay.Core.Protocols.ServerTag[m.ServerTag] : m.ServerTag;
                    }
                    if (m.AppKey != null && m.hasOwnProperty("AppKey")) {
                        d.AppKey = m.AppKey;
                    }
                    return d;
                };

                ReqHankShake.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                ReqHankShake.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.ReqHankShake";
                };

                return ReqHankShake;
            })();

            Protocols.RespHandShake = (function() {

                function RespHandShake(p) {
                    this.Routes = {};
                    if (p)
                        for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                            if (p[ks[i]] != null)
                                this[ks[i]] = p[ks[i]];
                }

                RespHandShake.prototype.ServerVersion = "";
                RespHandShake.prototype.HeartBeatInterval = 0;
                RespHandShake.prototype.Routes = $util.emptyObject;

                RespHandShake.create = function create(properties) {
                    return new RespHandShake(properties);
                };

                RespHandShake.encode = function encode(m, w) {
                    if (!w)
                        w = $Writer.create();
                    if (m.ServerVersion != null && Object.hasOwnProperty.call(m, "ServerVersion"))
                        w.uint32(10).string(m.ServerVersion);
                    if (m.HeartBeatInterval != null && Object.hasOwnProperty.call(m, "HeartBeatInterval"))
                        w.uint32(16).uint32(m.HeartBeatInterval);
                    if (m.Routes != null && Object.hasOwnProperty.call(m, "Routes")) {
                        for (var ks = Object.keys(m.Routes), i = 0; i < ks.length; ++i) {
                            w.uint32(26).fork().uint32(10).string(ks[i]).uint32(16).uint32(m.Routes[ks[i]]).ldelim();
                        }
                    }
                    return w;
                };

                RespHandShake.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                RespHandShake.decode = function decode(r, l) {
                    if (!(r instanceof $Reader))
                        r = $Reader.create(r);
                    var c = l === undefined ? r.len : r.pos + l, m = new $root.GoPlay.Core.Protocols.RespHandShake(), k, value;
                    while (r.pos < c) {
                        var t = r.uint32();
                        switch (t >>> 3) {
                        case 1: {
                                m.ServerVersion = r.string();
                                break;
                            }
                        case 2: {
                                m.HeartBeatInterval = r.uint32();
                                break;
                            }
                        case 3: {
                                if (m.Routes === $util.emptyObject)
                                    m.Routes = {};
                                var c2 = r.uint32() + r.pos;
                                k = "";
                                value = 0;
                                while (r.pos < c2) {
                                    var tag2 = r.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        k = r.string();
                                        break;
                                    case 2:
                                        value = r.uint32();
                                        break;
                                    default:
                                        r.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                m.Routes[k] = value;
                                break;
                            }
                        default:
                            r.skipType(t & 7);
                            break;
                        }
                    }
                    return m;
                };

                RespHandShake.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                RespHandShake.verify = function verify(m) {
                    if (typeof m !== "object" || m === null)
                        return "object expected";
                    if (m.ServerVersion != null && m.hasOwnProperty("ServerVersion")) {
                        if (!$util.isString(m.ServerVersion))
                            return "ServerVersion: string expected";
                    }
                    if (m.HeartBeatInterval != null && m.hasOwnProperty("HeartBeatInterval")) {
                        if (!$util.isInteger(m.HeartBeatInterval))
                            return "HeartBeatInterval: integer expected";
                    }
                    if (m.Routes != null && m.hasOwnProperty("Routes")) {
                        if (!$util.isObject(m.Routes))
                            return "Routes: object expected";
                        var k = Object.keys(m.Routes);
                        for (var i = 0; i < k.length; ++i) {
                            if (!$util.isInteger(m.Routes[k[i]]))
                                return "Routes: integer{k:string} expected";
                        }
                    }
                    return null;
                };

                RespHandShake.fromObject = function fromObject(d) {
                    if (d instanceof $root.GoPlay.Core.Protocols.RespHandShake)
                        return d;
                    var m = new $root.GoPlay.Core.Protocols.RespHandShake();
                    if (d.ServerVersion != null) {
                        m.ServerVersion = String(d.ServerVersion);
                    }
                    if (d.HeartBeatInterval != null) {
                        m.HeartBeatInterval = d.HeartBeatInterval >>> 0;
                    }
                    if (d.Routes) {
                        if (typeof d.Routes !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.RespHandShake.Routes: object expected");
                        m.Routes = {};
                        for (var ks = Object.keys(d.Routes), i = 0; i < ks.length; ++i) {
                            m.Routes[ks[i]] = d.Routes[ks[i]] >>> 0;
                        }
                    }
                    return m;
                };

                RespHandShake.toObject = function toObject(m, o) {
                    if (!o)
                        o = {};
                    var d = {};
                    if (o.objects || o.defaults) {
                        d.Routes = {};
                    }
                    if (o.defaults) {
                        d.ServerVersion = "";
                        d.HeartBeatInterval = 0;
                    }
                    if (m.ServerVersion != null && m.hasOwnProperty("ServerVersion")) {
                        d.ServerVersion = m.ServerVersion;
                    }
                    if (m.HeartBeatInterval != null && m.hasOwnProperty("HeartBeatInterval")) {
                        d.HeartBeatInterval = m.HeartBeatInterval;
                    }
                    var ks2;
                    if (m.Routes && (ks2 = Object.keys(m.Routes)).length) {
                        d.Routes = {};
                        for (var j = 0; j < ks2.length; ++j) {
                            d.Routes[ks2[j]] = m.Routes[ks2[j]];
                        }
                    }
                    return d;
                };

                RespHandShake.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                RespHandShake.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.RespHandShake";
                };

                return RespHandShake;
            })();

            return Protocols;
        })();

        return Core;
    })();

    return GoPlay;
})();

$root.google = (function() {

    var google = {};

    google.protobuf = (function() {

        var protobuf = {};

        protobuf.Timestamp = (function() {

            function Timestamp(p) {
                if (p)
                    for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                        if (p[ks[i]] != null)
                            this[ks[i]] = p[ks[i]];
            }

            Timestamp.prototype.seconds = $util.Long ? $util.Long.fromBits(0,0,false) : 0;
            Timestamp.prototype.nanos = 0;

            Timestamp.create = function create(properties) {
                return new Timestamp(properties);
            };

            Timestamp.encode = function encode(m, w) {
                if (!w)
                    w = $Writer.create();
                if (m.seconds != null && Object.hasOwnProperty.call(m, "seconds"))
                    w.uint32(8).int64(m.seconds);
                if (m.nanos != null && Object.hasOwnProperty.call(m, "nanos"))
                    w.uint32(16).int32(m.nanos);
                return w;
            };

            Timestamp.encodeDelimited = function encodeDelimited(message, writer) {
                return this.encode(message, writer).ldelim();
            };

            Timestamp.decode = function decode(r, l) {
                if (!(r instanceof $Reader))
                    r = $Reader.create(r);
                var c = l === undefined ? r.len : r.pos + l, m = new $root.google.protobuf.Timestamp();
                while (r.pos < c) {
                    var t = r.uint32();
                    switch (t >>> 3) {
                    case 1: {
                            m.seconds = r.int64();
                            break;
                        }
                    case 2: {
                            m.nanos = r.int32();
                            break;
                        }
                    default:
                        r.skipType(t & 7);
                        break;
                    }
                }
                return m;
            };

            Timestamp.decodeDelimited = function decodeDelimited(reader) {
                if (!(reader instanceof $Reader))
                    reader = new $Reader(reader);
                return this.decode(reader, reader.uint32());
            };

            Timestamp.verify = function verify(m) {
                if (typeof m !== "object" || m === null)
                    return "object expected";
                if (m.seconds != null && m.hasOwnProperty("seconds")) {
                    if (!$util.isInteger(m.seconds) && !(m.seconds && $util.isInteger(m.seconds.low) && $util.isInteger(m.seconds.high)))
                        return "seconds: integer|Long expected";
                }
                if (m.nanos != null && m.hasOwnProperty("nanos")) {
                    if (!$util.isInteger(m.nanos))
                        return "nanos: integer expected";
                }
                return null;
            };

            Timestamp.fromObject = function fromObject(d) {
                if (d instanceof $root.google.protobuf.Timestamp)
                    return d;
                var m = new $root.google.protobuf.Timestamp();
                if (d.seconds != null) {
                    if ($util.Long)
                        (m.seconds = $util.Long.fromValue(d.seconds)).unsigned = false;
                    else if (typeof d.seconds === "string")
                        m.seconds = parseInt(d.seconds, 10);
                    else if (typeof d.seconds === "number")
                        m.seconds = d.seconds;
                    else if (typeof d.seconds === "object")
                        m.seconds = new $util.LongBits(d.seconds.low >>> 0, d.seconds.high >>> 0).toNumber();
                }
                if (d.nanos != null) {
                    m.nanos = d.nanos | 0;
                }
                return m;
            };

            Timestamp.toObject = function toObject(m, o) {
                if (!o)
                    o = {};
                var d = {};
                if (o.defaults) {
                    if ($util.Long) {
                        var n = new $util.Long(0, 0, false);
                        d.seconds = o.longs === String ? n.toString() : o.longs === Number ? n.toNumber() : n;
                    } else
                        d.seconds = o.longs === String ? "0" : 0;
                    d.nanos = 0;
                }
                if (m.seconds != null && m.hasOwnProperty("seconds")) {
                    if (typeof m.seconds === "number")
                        d.seconds = o.longs === String ? String(m.seconds) : m.seconds;
                    else
                        d.seconds = o.longs === String ? $util.Long.prototype.toString.call(m.seconds) : o.longs === Number ? new $util.LongBits(m.seconds.low >>> 0, m.seconds.high >>> 0).toNumber() : m.seconds;
                }
                if (m.nanos != null && m.hasOwnProperty("nanos")) {
                    d.nanos = m.nanos;
                }
                return d;
            };

            Timestamp.prototype.toJSON = function toJSON() {
                return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
            };

            Timestamp.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                if (typeUrlPrefix === undefined) {
                    typeUrlPrefix = "type.googleapis.com";
                }
                return typeUrlPrefix + "/google.protobuf.Timestamp";
            };

            return Timestamp;
        })();

        protobuf.Any = (function() {

            function Any(p) {
                if (p)
                    for (var ks = Object.keys(p), i = 0; i < ks.length; ++i)
                        if (p[ks[i]] != null)
                            this[ks[i]] = p[ks[i]];
            }

            Any.prototype.type_url = "";
            Any.prototype.value = $util.newBuffer([]);

            Any.create = function create(properties) {
                return new Any(properties);
            };

            Any.encode = function encode(m, w) {
                if (!w)
                    w = $Writer.create();
                if (m.type_url != null && Object.hasOwnProperty.call(m, "type_url"))
                    w.uint32(10).string(m.type_url);
                if (m.value != null && Object.hasOwnProperty.call(m, "value"))
                    w.uint32(18).bytes(m.value);
                return w;
            };

            Any.encodeDelimited = function encodeDelimited(message, writer) {
                return this.encode(message, writer).ldelim();
            };

            Any.decode = function decode(r, l) {
                if (!(r instanceof $Reader))
                    r = $Reader.create(r);
                var c = l === undefined ? r.len : r.pos + l, m = new $root.google.protobuf.Any();
                while (r.pos < c) {
                    var t = r.uint32();
                    switch (t >>> 3) {
                    case 1: {
                            m.type_url = r.string();
                            break;
                        }
                    case 2: {
                            m.value = r.bytes();
                            break;
                        }
                    default:
                        r.skipType(t & 7);
                        break;
                    }
                }
                return m;
            };

            Any.decodeDelimited = function decodeDelimited(reader) {
                if (!(reader instanceof $Reader))
                    reader = new $Reader(reader);
                return this.decode(reader, reader.uint32());
            };

            Any.verify = function verify(m) {
                if (typeof m !== "object" || m === null)
                    return "object expected";
                if (m.type_url != null && m.hasOwnProperty("type_url")) {
                    if (!$util.isString(m.type_url))
                        return "type_url: string expected";
                }
                if (m.value != null && m.hasOwnProperty("value")) {
                    if (!(m.value && typeof m.value.length === "number" || $util.isString(m.value)))
                        return "value: buffer expected";
                }
                return null;
            };

            Any.fromObject = function fromObject(d) {
                if (d instanceof $root.google.protobuf.Any)
                    return d;
                var m = new $root.google.protobuf.Any();
                if (d.type_url != null) {
                    m.type_url = String(d.type_url);
                }
                if (d.value != null) {
                    if (typeof d.value === "string")
                        $util.base64.decode(d.value, m.value = $util.newBuffer($util.base64.length(d.value)), 0);
                    else if (d.value.length >= 0)
                        m.value = d.value;
                }
                return m;
            };

            Any.toObject = function toObject(m, o) {
                if (!o)
                    o = {};
                var d = {};
                if (o.defaults) {
                    d.type_url = "";
                    if (o.bytes === String)
                        d.value = "";
                    else {
                        d.value = [];
                        if (o.bytes !== Array)
                            d.value = $util.newBuffer(d.value);
                    }
                }
                if (m.type_url != null && m.hasOwnProperty("type_url")) {
                    d.type_url = m.type_url;
                }
                if (m.value != null && m.hasOwnProperty("value")) {
                    d.value = o.bytes === String ? $util.base64.encode(m.value, 0, m.value.length) : o.bytes === Array ? Array.prototype.slice.call(m.value) : m.value;
                }
                return d;
            };

            Any.prototype.toJSON = function toJSON() {
                return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
            };

            Any.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                if (typeUrlPrefix === undefined) {
                    typeUrlPrefix = "type.googleapis.com";
                }
                return typeUrlPrefix + "/google.protobuf.Any";
            };

            return Any;
        })();

        return protobuf;
    })();

    return google;
})();