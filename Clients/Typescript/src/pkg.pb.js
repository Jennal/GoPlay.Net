/*eslint-disable block-scoped-var, id-length, no-control-regex, no-magic-numbers, no-prototype-builtins, no-redeclare, no-shadow, no-var, sort-vars*/
"use strict";

var $protobuf = require("protobufjs/minimal");

// Common aliases
var $Reader = $protobuf.Reader, $Writer = $protobuf.Writer, $util = $protobuf.util;

// Exported root namespace
var $root = $protobuf.roots["default"] || ($protobuf.roots["default"] = {});

$root.GoPlay = (function() {

    /**
     * Namespace GoPlay.
     * @exports GoPlay
     * @namespace
     */
    var GoPlay = {};

    GoPlay.Core = (function() {

        /**
         * Namespace Core.
         * @memberof GoPlay
         * @namespace
         */
        var Core = {};

        Core.Protocols = (function() {

            /**
             * Namespace Protocols.
             * @memberof GoPlay.Core
             * @namespace
             */
            var Protocols = {};

            Protocols.PbAny = (function() {

                /**
                 * Properties of a PbAny.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbAny
                 * @property {google.protobuf.IAny|null} [Value] PbAny Value
                 */

                /**
                 * Constructs a new PbAny.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbAny.
                 * @implements IPbAny
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbAny=} [properties] Properties to set
                 */
                function PbAny(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbAny Value.
                 * @member {google.protobuf.IAny|null|undefined} Value
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @instance
                 */
                PbAny.prototype.Value = null;

                /**
                 * Creates a new PbAny instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbAny=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbAny} PbAny instance
                 */
                PbAny.create = function create(properties) {
                    return new PbAny(properties);
                };

                /**
                 * Encodes the specified PbAny message. Does not implicitly {@link GoPlay.Core.Protocols.PbAny.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbAny} message PbAny message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbAny.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        $root.google.protobuf.Any.encode(message.Value, writer.uint32(/* id 1, wireType 2 =*/10).fork()).ldelim();
                    return writer;
                };

                /**
                 * Encodes the specified PbAny message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbAny.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbAny} message PbAny message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbAny.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbAny message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbAny} PbAny
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbAny.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbAny();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = $root.google.protobuf.Any.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbAny message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbAny} PbAny
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbAny.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbAny message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbAny.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        var error = $root.google.protobuf.Any.verify(message.Value);
                        if (error)
                            return "Value." + error;
                    }
                    return null;
                };

                /**
                 * Creates a PbAny message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbAny} PbAny
                 */
                PbAny.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbAny)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbAny();
                    if (object.Value != null) {
                        if (typeof object.Value !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.PbAny.Value: object expected");
                        message.Value = $root.google.protobuf.Any.fromObject(object.Value);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbAny message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {GoPlay.Core.Protocols.PbAny} message PbAny
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbAny.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = null;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = $root.google.protobuf.Any.toObject(message.Value, options);
                    return object;
                };

                /**
                 * Converts this PbAny to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbAny.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbAny
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbAny
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbAny.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbAny";
                };

                return PbAny;
            })();

            Protocols.PbTime = (function() {

                /**
                 * Properties of a PbTime.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbTime
                 * @property {google.protobuf.ITimestamp|null} [Value] PbTime Value
                 */

                /**
                 * Constructs a new PbTime.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbTime.
                 * @implements IPbTime
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbTime=} [properties] Properties to set
                 */
                function PbTime(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbTime Value.
                 * @member {google.protobuf.ITimestamp|null|undefined} Value
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @instance
                 */
                PbTime.prototype.Value = null;

                /**
                 * Creates a new PbTime instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbTime=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbTime} PbTime instance
                 */
                PbTime.create = function create(properties) {
                    return new PbTime(properties);
                };

                /**
                 * Encodes the specified PbTime message. Does not implicitly {@link GoPlay.Core.Protocols.PbTime.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbTime} message PbTime message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbTime.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        $root.google.protobuf.Timestamp.encode(message.Value, writer.uint32(/* id 1, wireType 2 =*/10).fork()).ldelim();
                    return writer;
                };

                /**
                 * Encodes the specified PbTime message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbTime.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbTime} message PbTime message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbTime.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbTime message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbTime} PbTime
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbTime.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbTime();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = $root.google.protobuf.Timestamp.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbTime message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbTime} PbTime
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbTime.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbTime message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbTime.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        var error = $root.google.protobuf.Timestamp.verify(message.Value);
                        if (error)
                            return "Value." + error;
                    }
                    return null;
                };

                /**
                 * Creates a PbTime message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbTime} PbTime
                 */
                PbTime.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbTime)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbTime();
                    if (object.Value != null) {
                        if (typeof object.Value !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.PbTime.Value: object expected");
                        message.Value = $root.google.protobuf.Timestamp.fromObject(object.Value);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbTime message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {GoPlay.Core.Protocols.PbTime} message PbTime
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbTime.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = null;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = $root.google.protobuf.Timestamp.toObject(message.Value, options);
                    return object;
                };

                /**
                 * Converts this PbTime to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbTime.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbTime
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbTime
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbTime.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbTime";
                };

                return PbTime;
            })();

            Protocols.PbString = (function() {

                /**
                 * Properties of a PbString.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbString
                 * @property {string|null} [Value] PbString Value
                 */

                /**
                 * Constructs a new PbString.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbString.
                 * @implements IPbString
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbString=} [properties] Properties to set
                 */
                function PbString(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbString Value.
                 * @member {string} Value
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @instance
                 */
                PbString.prototype.Value = "";

                /**
                 * Creates a new PbString instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbString=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbString} PbString instance
                 */
                PbString.create = function create(properties) {
                    return new PbString(properties);
                };

                /**
                 * Encodes the specified PbString message. Does not implicitly {@link GoPlay.Core.Protocols.PbString.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbString} message PbString message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbString.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        writer.uint32(/* id 1, wireType 2 =*/10).string(message.Value);
                    return writer;
                };

                /**
                 * Encodes the specified PbString message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbString.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbString} message PbString message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbString.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbString message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbString} PbString
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbString.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbString();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbString message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbString} PbString
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbString.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbString message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbString.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (!$util.isString(message.Value))
                            return "Value: string expected";
                    return null;
                };

                /**
                 * Creates a PbString message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbString} PbString
                 */
                PbString.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbString)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbString();
                    if (object.Value != null)
                        message.Value = String(object.Value);
                    return message;
                };

                /**
                 * Creates a plain object from a PbString message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {GoPlay.Core.Protocols.PbString} message PbString
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbString.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = "";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = message.Value;
                    return object;
                };

                /**
                 * Converts this PbString to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbString.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbString
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbString
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbString.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbString";
                };

                return PbString;
            })();

            Protocols.PbInt = (function() {

                /**
                 * Properties of a PbInt.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbInt
                 * @property {number|null} [Value] PbInt Value
                 */

                /**
                 * Constructs a new PbInt.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbInt.
                 * @implements IPbInt
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbInt=} [properties] Properties to set
                 */
                function PbInt(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbInt Value.
                 * @member {number} Value
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @instance
                 */
                PbInt.prototype.Value = 0;

                /**
                 * Creates a new PbInt instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbInt=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbInt} PbInt instance
                 */
                PbInt.create = function create(properties) {
                    return new PbInt(properties);
                };

                /**
                 * Encodes the specified PbInt message. Does not implicitly {@link GoPlay.Core.Protocols.PbInt.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbInt} message PbInt message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbInt.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        writer.uint32(/* id 1, wireType 0 =*/8).int32(message.Value);
                    return writer;
                };

                /**
                 * Encodes the specified PbInt message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbInt.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbInt} message PbInt message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbInt.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbInt message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbInt} PbInt
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbInt.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbInt();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbInt message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbInt} PbInt
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbInt.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbInt message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbInt.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (!$util.isInteger(message.Value))
                            return "Value: integer expected";
                    return null;
                };

                /**
                 * Creates a PbInt message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbInt} PbInt
                 */
                PbInt.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbInt)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbInt();
                    if (object.Value != null)
                        message.Value = object.Value | 0;
                    return message;
                };

                /**
                 * Creates a plain object from a PbInt message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {GoPlay.Core.Protocols.PbInt} message PbInt
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbInt.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = 0;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = message.Value;
                    return object;
                };

                /**
                 * Converts this PbInt to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbInt.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbInt
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbInt
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbInt.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbInt";
                };

                return PbInt;
            })();

            Protocols.PbLong = (function() {

                /**
                 * Properties of a PbLong.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbLong
                 * @property {number|Long|null} [Value] PbLong Value
                 */

                /**
                 * Constructs a new PbLong.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbLong.
                 * @implements IPbLong
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbLong=} [properties] Properties to set
                 */
                function PbLong(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbLong Value.
                 * @member {number|Long} Value
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @instance
                 */
                PbLong.prototype.Value = $util.Long ? $util.Long.fromBits(0,0,false) : 0;

                /**
                 * Creates a new PbLong instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbLong=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbLong} PbLong instance
                 */
                PbLong.create = function create(properties) {
                    return new PbLong(properties);
                };

                /**
                 * Encodes the specified PbLong message. Does not implicitly {@link GoPlay.Core.Protocols.PbLong.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbLong} message PbLong message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbLong.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        writer.uint32(/* id 1, wireType 0 =*/8).int64(message.Value);
                    return writer;
                };

                /**
                 * Encodes the specified PbLong message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbLong.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbLong} message PbLong message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbLong.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbLong message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbLong} PbLong
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbLong.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbLong();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = reader.int64();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbLong message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbLong} PbLong
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbLong.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbLong message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbLong.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (!$util.isInteger(message.Value) && !(message.Value && $util.isInteger(message.Value.low) && $util.isInteger(message.Value.high)))
                            return "Value: integer|Long expected";
                    return null;
                };

                /**
                 * Creates a PbLong message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbLong} PbLong
                 */
                PbLong.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbLong)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbLong();
                    if (object.Value != null)
                        if ($util.Long)
                            (message.Value = $util.Long.fromValue(object.Value)).unsigned = false;
                        else if (typeof object.Value === "string")
                            message.Value = parseInt(object.Value, 10);
                        else if (typeof object.Value === "number")
                            message.Value = object.Value;
                        else if (typeof object.Value === "object")
                            message.Value = new $util.LongBits(object.Value.low >>> 0, object.Value.high >>> 0).toNumber();
                    return message;
                };

                /**
                 * Creates a plain object from a PbLong message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {GoPlay.Core.Protocols.PbLong} message PbLong
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbLong.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        if ($util.Long) {
                            var long = new $util.Long(0, 0, false);
                            object.Value = options.longs === String ? long.toString() : options.longs === Number ? long.toNumber() : long;
                        } else
                            object.Value = options.longs === String ? "0" : 0;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (typeof message.Value === "number")
                            object.Value = options.longs === String ? String(message.Value) : message.Value;
                        else
                            object.Value = options.longs === String ? $util.Long.prototype.toString.call(message.Value) : options.longs === Number ? new $util.LongBits(message.Value.low >>> 0, message.Value.high >>> 0).toNumber() : message.Value;
                    return object;
                };

                /**
                 * Converts this PbLong to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbLong.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbLong
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbLong
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbLong.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbLong";
                };

                return PbLong;
            })();

            Protocols.PbFloat = (function() {

                /**
                 * Properties of a PbFloat.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbFloat
                 * @property {number|null} [Value] PbFloat Value
                 */

                /**
                 * Constructs a new PbFloat.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbFloat.
                 * @implements IPbFloat
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbFloat=} [properties] Properties to set
                 */
                function PbFloat(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbFloat Value.
                 * @member {number} Value
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @instance
                 */
                PbFloat.prototype.Value = 0;

                /**
                 * Creates a new PbFloat instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloat=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbFloat} PbFloat instance
                 */
                PbFloat.create = function create(properties) {
                    return new PbFloat(properties);
                };

                /**
                 * Encodes the specified PbFloat message. Does not implicitly {@link GoPlay.Core.Protocols.PbFloat.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloat} message PbFloat message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbFloat.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        writer.uint32(/* id 1, wireType 5 =*/13).float(message.Value);
                    return writer;
                };

                /**
                 * Encodes the specified PbFloat message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbFloat.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloat} message PbFloat message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbFloat.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbFloat message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbFloat} PbFloat
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbFloat.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbFloat();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbFloat message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbFloat} PbFloat
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbFloat.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbFloat message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbFloat.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (typeof message.Value !== "number")
                            return "Value: number expected";
                    return null;
                };

                /**
                 * Creates a PbFloat message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbFloat} PbFloat
                 */
                PbFloat.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbFloat)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbFloat();
                    if (object.Value != null)
                        message.Value = Number(object.Value);
                    return message;
                };

                /**
                 * Creates a plain object from a PbFloat message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {GoPlay.Core.Protocols.PbFloat} message PbFloat
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbFloat.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = 0;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = options.json && !isFinite(message.Value) ? String(message.Value) : message.Value;
                    return object;
                };

                /**
                 * Converts this PbFloat to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbFloat.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbFloat
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbFloat
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbFloat.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbFloat";
                };

                return PbFloat;
            })();

            Protocols.PbBool = (function() {

                /**
                 * Properties of a PbBool.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbBool
                 * @property {boolean|null} [Value] PbBool Value
                 */

                /**
                 * Constructs a new PbBool.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbBool.
                 * @implements IPbBool
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbBool=} [properties] Properties to set
                 */
                function PbBool(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbBool Value.
                 * @member {boolean} Value
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @instance
                 */
                PbBool.prototype.Value = false;

                /**
                 * Creates a new PbBool instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBool=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbBool} PbBool instance
                 */
                PbBool.create = function create(properties) {
                    return new PbBool(properties);
                };

                /**
                 * Encodes the specified PbBool message. Does not implicitly {@link GoPlay.Core.Protocols.PbBool.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBool} message PbBool message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbBool.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && Object.hasOwnProperty.call(message, "Value"))
                        writer.uint32(/* id 1, wireType 0 =*/8).bool(message.Value);
                    return writer;
                };

                /**
                 * Encodes the specified PbBool message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbBool.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBool} message PbBool message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbBool.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbBool message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbBool} PbBool
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbBool.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbBool();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Value = reader.bool();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbBool message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbBool} PbBool
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbBool.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbBool message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbBool.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        if (typeof message.Value !== "boolean")
                            return "Value: boolean expected";
                    return null;
                };

                /**
                 * Creates a PbBool message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbBool} PbBool
                 */
                PbBool.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbBool)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbBool();
                    if (object.Value != null)
                        message.Value = Boolean(object.Value);
                    return message;
                };

                /**
                 * Creates a plain object from a PbBool message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {GoPlay.Core.Protocols.PbBool} message PbBool
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbBool.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults)
                        object.Value = false;
                    if (message.Value != null && message.hasOwnProperty("Value"))
                        object.Value = message.Value;
                    return object;
                };

                /**
                 * Converts this PbBool to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbBool.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbBool
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbBool
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbBool.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbBool";
                };

                return PbBool;
            })();

            Protocols.PbStringArray = (function() {

                /**
                 * Properties of a PbStringArray.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbStringArray
                 * @property {Array.<string>|null} [Value] PbStringArray Value
                 */

                /**
                 * Constructs a new PbStringArray.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbStringArray.
                 * @implements IPbStringArray
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbStringArray=} [properties] Properties to set
                 */
                function PbStringArray(properties) {
                    this.Value = [];
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbStringArray Value.
                 * @member {Array.<string>} Value
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @instance
                 */
                PbStringArray.prototype.Value = $util.emptyArray;

                /**
                 * Creates a new PbStringArray instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbStringArray=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbStringArray} PbStringArray instance
                 */
                PbStringArray.create = function create(properties) {
                    return new PbStringArray(properties);
                };

                /**
                 * Encodes the specified PbStringArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbStringArray.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbStringArray} message PbStringArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbStringArray.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && message.Value.length)
                        for (var i = 0; i < message.Value.length; ++i)
                            writer.uint32(/* id 1, wireType 2 =*/10).string(message.Value[i]);
                    return writer;
                };

                /**
                 * Encodes the specified PbStringArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbStringArray.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbStringArray} message PbStringArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbStringArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbStringArray message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbStringArray} PbStringArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbStringArray.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbStringArray();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.Value && message.Value.length))
                                    message.Value = [];
                                message.Value.push(reader.string());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbStringArray message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbStringArray} PbStringArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbStringArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbStringArray message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbStringArray.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        if (!Array.isArray(message.Value))
                            return "Value: array expected";
                        for (var i = 0; i < message.Value.length; ++i)
                            if (!$util.isString(message.Value[i]))
                                return "Value: string[] expected";
                    }
                    return null;
                };

                /**
                 * Creates a PbStringArray message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbStringArray} PbStringArray
                 */
                PbStringArray.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbStringArray)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbStringArray();
                    if (object.Value) {
                        if (!Array.isArray(object.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbStringArray.Value: array expected");
                        message.Value = [];
                        for (var i = 0; i < object.Value.length; ++i)
                            message.Value[i] = String(object.Value[i]);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbStringArray message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {GoPlay.Core.Protocols.PbStringArray} message PbStringArray
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbStringArray.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.arrays || options.defaults)
                        object.Value = [];
                    if (message.Value && message.Value.length) {
                        object.Value = [];
                        for (var j = 0; j < message.Value.length; ++j)
                            object.Value[j] = message.Value[j];
                    }
                    return object;
                };

                /**
                 * Converts this PbStringArray to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbStringArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbStringArray
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbStringArray
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbStringArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbStringArray";
                };

                return PbStringArray;
            })();

            Protocols.PbIntArray = (function() {

                /**
                 * Properties of a PbIntArray.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbIntArray
                 * @property {Array.<number>|null} [Value] PbIntArray Value
                 */

                /**
                 * Constructs a new PbIntArray.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbIntArray.
                 * @implements IPbIntArray
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbIntArray=} [properties] Properties to set
                 */
                function PbIntArray(properties) {
                    this.Value = [];
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbIntArray Value.
                 * @member {Array.<number>} Value
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @instance
                 */
                PbIntArray.prototype.Value = $util.emptyArray;

                /**
                 * Creates a new PbIntArray instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbIntArray=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbIntArray} PbIntArray instance
                 */
                PbIntArray.create = function create(properties) {
                    return new PbIntArray(properties);
                };

                /**
                 * Encodes the specified PbIntArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbIntArray.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbIntArray} message PbIntArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbIntArray.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && message.Value.length) {
                        writer.uint32(/* id 1, wireType 2 =*/10).fork();
                        for (var i = 0; i < message.Value.length; ++i)
                            writer.int32(message.Value[i]);
                        writer.ldelim();
                    }
                    return writer;
                };

                /**
                 * Encodes the specified PbIntArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbIntArray.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbIntArray} message PbIntArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbIntArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbIntArray message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbIntArray} PbIntArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbIntArray.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbIntArray();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.Value && message.Value.length))
                                    message.Value = [];
                                if ((tag & 7) === 2) {
                                    var end2 = reader.uint32() + reader.pos;
                                    while (reader.pos < end2)
                                        message.Value.push(reader.int32());
                                } else
                                    message.Value.push(reader.int32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbIntArray message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbIntArray} PbIntArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbIntArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbIntArray message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbIntArray.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        if (!Array.isArray(message.Value))
                            return "Value: array expected";
                        for (var i = 0; i < message.Value.length; ++i)
                            if (!$util.isInteger(message.Value[i]))
                                return "Value: integer[] expected";
                    }
                    return null;
                };

                /**
                 * Creates a PbIntArray message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbIntArray} PbIntArray
                 */
                PbIntArray.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbIntArray)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbIntArray();
                    if (object.Value) {
                        if (!Array.isArray(object.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbIntArray.Value: array expected");
                        message.Value = [];
                        for (var i = 0; i < object.Value.length; ++i)
                            message.Value[i] = object.Value[i] | 0;
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbIntArray message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {GoPlay.Core.Protocols.PbIntArray} message PbIntArray
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbIntArray.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.arrays || options.defaults)
                        object.Value = [];
                    if (message.Value && message.Value.length) {
                        object.Value = [];
                        for (var j = 0; j < message.Value.length; ++j)
                            object.Value[j] = message.Value[j];
                    }
                    return object;
                };

                /**
                 * Converts this PbIntArray to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbIntArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbIntArray
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbIntArray
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbIntArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbIntArray";
                };

                return PbIntArray;
            })();

            Protocols.PbFloatArray = (function() {

                /**
                 * Properties of a PbFloatArray.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbFloatArray
                 * @property {Array.<number>|null} [Value] PbFloatArray Value
                 */

                /**
                 * Constructs a new PbFloatArray.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbFloatArray.
                 * @implements IPbFloatArray
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbFloatArray=} [properties] Properties to set
                 */
                function PbFloatArray(properties) {
                    this.Value = [];
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbFloatArray Value.
                 * @member {Array.<number>} Value
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @instance
                 */
                PbFloatArray.prototype.Value = $util.emptyArray;

                /**
                 * Creates a new PbFloatArray instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloatArray=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbFloatArray} PbFloatArray instance
                 */
                PbFloatArray.create = function create(properties) {
                    return new PbFloatArray(properties);
                };

                /**
                 * Encodes the specified PbFloatArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbFloatArray.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloatArray} message PbFloatArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbFloatArray.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && message.Value.length) {
                        writer.uint32(/* id 1, wireType 2 =*/10).fork();
                        for (var i = 0; i < message.Value.length; ++i)
                            writer.float(message.Value[i]);
                        writer.ldelim();
                    }
                    return writer;
                };

                /**
                 * Encodes the specified PbFloatArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbFloatArray.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbFloatArray} message PbFloatArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbFloatArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbFloatArray message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbFloatArray} PbFloatArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbFloatArray.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbFloatArray();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.Value && message.Value.length))
                                    message.Value = [];
                                if ((tag & 7) === 2) {
                                    var end2 = reader.uint32() + reader.pos;
                                    while (reader.pos < end2)
                                        message.Value.push(reader.float());
                                } else
                                    message.Value.push(reader.float());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbFloatArray message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbFloatArray} PbFloatArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbFloatArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbFloatArray message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbFloatArray.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        if (!Array.isArray(message.Value))
                            return "Value: array expected";
                        for (var i = 0; i < message.Value.length; ++i)
                            if (typeof message.Value[i] !== "number")
                                return "Value: number[] expected";
                    }
                    return null;
                };

                /**
                 * Creates a PbFloatArray message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbFloatArray} PbFloatArray
                 */
                PbFloatArray.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbFloatArray)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbFloatArray();
                    if (object.Value) {
                        if (!Array.isArray(object.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbFloatArray.Value: array expected");
                        message.Value = [];
                        for (var i = 0; i < object.Value.length; ++i)
                            message.Value[i] = Number(object.Value[i]);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbFloatArray message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {GoPlay.Core.Protocols.PbFloatArray} message PbFloatArray
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbFloatArray.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.arrays || options.defaults)
                        object.Value = [];
                    if (message.Value && message.Value.length) {
                        object.Value = [];
                        for (var j = 0; j < message.Value.length; ++j)
                            object.Value[j] = options.json && !isFinite(message.Value[j]) ? String(message.Value[j]) : message.Value[j];
                    }
                    return object;
                };

                /**
                 * Converts this PbFloatArray to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbFloatArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbFloatArray
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbFloatArray
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbFloatArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbFloatArray";
                };

                return PbFloatArray;
            })();

            Protocols.PbBoolArray = (function() {

                /**
                 * Properties of a PbBoolArray.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPbBoolArray
                 * @property {Array.<boolean>|null} [Value] PbBoolArray Value
                 */

                /**
                 * Constructs a new PbBoolArray.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PbBoolArray.
                 * @implements IPbBoolArray
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPbBoolArray=} [properties] Properties to set
                 */
                function PbBoolArray(properties) {
                    this.Value = [];
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PbBoolArray Value.
                 * @member {Array.<boolean>} Value
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @instance
                 */
                PbBoolArray.prototype.Value = $util.emptyArray;

                /**
                 * Creates a new PbBoolArray instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBoolArray=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PbBoolArray} PbBoolArray instance
                 */
                PbBoolArray.create = function create(properties) {
                    return new PbBoolArray(properties);
                };

                /**
                 * Encodes the specified PbBoolArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbBoolArray.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBoolArray} message PbBoolArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbBoolArray.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Value != null && message.Value.length) {
                        writer.uint32(/* id 1, wireType 2 =*/10).fork();
                        for (var i = 0; i < message.Value.length; ++i)
                            writer.bool(message.Value[i]);
                        writer.ldelim();
                    }
                    return writer;
                };

                /**
                 * Encodes the specified PbBoolArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbBoolArray.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {GoPlay.Core.Protocols.IPbBoolArray} message PbBoolArray message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PbBoolArray.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PbBoolArray message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PbBoolArray} PbBoolArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbBoolArray.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PbBoolArray();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.Value && message.Value.length))
                                    message.Value = [];
                                if ((tag & 7) === 2) {
                                    var end2 = reader.uint32() + reader.pos;
                                    while (reader.pos < end2)
                                        message.Value.push(reader.bool());
                                } else
                                    message.Value.push(reader.bool());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PbBoolArray message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PbBoolArray} PbBoolArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PbBoolArray.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PbBoolArray message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PbBoolArray.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Value != null && message.hasOwnProperty("Value")) {
                        if (!Array.isArray(message.Value))
                            return "Value: array expected";
                        for (var i = 0; i < message.Value.length; ++i)
                            if (typeof message.Value[i] !== "boolean")
                                return "Value: boolean[] expected";
                    }
                    return null;
                };

                /**
                 * Creates a PbBoolArray message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PbBoolArray} PbBoolArray
                 */
                PbBoolArray.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PbBoolArray)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PbBoolArray();
                    if (object.Value) {
                        if (!Array.isArray(object.Value))
                            throw TypeError(".GoPlay.Core.Protocols.PbBoolArray.Value: array expected");
                        message.Value = [];
                        for (var i = 0; i < object.Value.length; ++i)
                            message.Value[i] = Boolean(object.Value[i]);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a PbBoolArray message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {GoPlay.Core.Protocols.PbBoolArray} message PbBoolArray
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PbBoolArray.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.arrays || options.defaults)
                        object.Value = [];
                    if (message.Value && message.Value.length) {
                        object.Value = [];
                        for (var j = 0; j < message.Value.length; ++j)
                            object.Value[j] = message.Value[j];
                    }
                    return object;
                };

                /**
                 * Converts this PbBoolArray to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PbBoolArray.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PbBoolArray
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PbBoolArray
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PbBoolArray.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PbBoolArray";
                };

                return PbBoolArray;
            })();

            /**
             * StatusCode enum.
             * @name GoPlay.Core.Protocols.StatusCode
             * @enum {number}
             * @property {number} Success=0 Success value
             * @property {number} Failed=1 Failed value
             * @property {number} Error=2 Error value
             * @property {number} Timeout=3 Timeout value
             */
            Protocols.StatusCode = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Success"] = 0;
                values[valuesById[1] = "Failed"] = 1;
                values[valuesById[2] = "Error"] = 2;
                values[valuesById[3] = "Timeout"] = 3;
                return values;
            })();

            /**
             * PackageType enum.
             * @name GoPlay.Core.Protocols.PackageType
             * @enum {number}
             * @property {number} HankShakeReq=0 HankShakeReq value
             * @property {number} HankShakeResp=1 HankShakeResp value
             * @property {number} Ping=2 Ping value
             * @property {number} Pong=3 Pong value
             * @property {number} Notify=4 Notify value
             * @property {number} Request=5 Request value
             * @property {number} Response=6 Response value
             * @property {number} Push=7 Push value
             * @property {number} Kick=8 Kick value
             */
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

            /**
             * EncodingType enum.
             * @name GoPlay.Core.Protocols.EncodingType
             * @enum {number}
             * @property {number} Protobuf=0 Protobuf value
             * @property {number} Json=1 Json value
             */
            Protocols.EncodingType = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Protobuf"] = 0;
                values[valuesById[1] = "Json"] = 1;
                return values;
            })();

            /**
             * ServerTag enum.
             * @name GoPlay.Core.Protocols.ServerTag
             * @enum {number}
             * @property {number} Empty=0 Empty value
             * @property {number} FrontEnd=1 FrontEnd value
             * @property {number} BackEnd=2 BackEnd value
             * @property {number} All=3 All value
             */
            Protocols.ServerTag = (function() {
                var valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "Empty"] = 0;
                values[valuesById[1] = "FrontEnd"] = 1;
                values[valuesById[2] = "BackEnd"] = 2;
                values[valuesById[3] = "All"] = 3;
                return values;
            })();

            Protocols.Status = (function() {

                /**
                 * Properties of a Status.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IStatus
                 * @property {GoPlay.Core.Protocols.StatusCode|null} [Code] Status Code
                 * @property {string|null} [Message] Status Message
                 */

                /**
                 * Constructs a new Status.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a Status.
                 * @implements IStatus
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IStatus=} [properties] Properties to set
                 */
                function Status(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * Status Code.
                 * @member {GoPlay.Core.Protocols.StatusCode} Code
                 * @memberof GoPlay.Core.Protocols.Status
                 * @instance
                 */
                Status.prototype.Code = 0;

                /**
                 * Status Message.
                 * @member {string} Message
                 * @memberof GoPlay.Core.Protocols.Status
                 * @instance
                 */
                Status.prototype.Message = "";

                /**
                 * Creates a new Status instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {GoPlay.Core.Protocols.IStatus=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.Status} Status instance
                 */
                Status.create = function create(properties) {
                    return new Status(properties);
                };

                /**
                 * Encodes the specified Status message. Does not implicitly {@link GoPlay.Core.Protocols.Status.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {GoPlay.Core.Protocols.IStatus} message Status message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Status.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Code != null && Object.hasOwnProperty.call(message, "Code"))
                        writer.uint32(/* id 1, wireType 0 =*/8).int32(message.Code);
                    if (message.Message != null && Object.hasOwnProperty.call(message, "Message"))
                        writer.uint32(/* id 2, wireType 2 =*/18).string(message.Message);
                    return writer;
                };

                /**
                 * Encodes the specified Status message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Status.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {GoPlay.Core.Protocols.IStatus} message Status message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Status.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a Status message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.Status} Status
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Status.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.Status();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Code = reader.int32();
                                break;
                            }
                        case 2: {
                                message.Message = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a Status message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.Status} Status
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Status.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a Status message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                Status.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Code != null && message.hasOwnProperty("Code"))
                        switch (message.Code) {
                        default:
                            return "Code: enum value expected";
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            break;
                        }
                    if (message.Message != null && message.hasOwnProperty("Message"))
                        if (!$util.isString(message.Message))
                            return "Message: string expected";
                    return null;
                };

                /**
                 * Creates a Status message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.Status} Status
                 */
                Status.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.Status)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.Status();
                    switch (object.Code) {
                    default:
                        if (typeof object.Code === "number") {
                            message.Code = object.Code;
                            break;
                        }
                        break;
                    case "Success":
                    case 0:
                        message.Code = 0;
                        break;
                    case "Failed":
                    case 1:
                        message.Code = 1;
                        break;
                    case "Error":
                    case 2:
                        message.Code = 2;
                        break;
                    case "Timeout":
                    case 3:
                        message.Code = 3;
                        break;
                    }
                    if (object.Message != null)
                        message.Message = String(object.Message);
                    return message;
                };

                /**
                 * Creates a plain object from a Status message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {GoPlay.Core.Protocols.Status} message Status
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                Status.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults) {
                        object.Code = options.enums === String ? "Success" : 0;
                        object.Message = "";
                    }
                    if (message.Code != null && message.hasOwnProperty("Code"))
                        object.Code = options.enums === String ? $root.GoPlay.Core.Protocols.StatusCode[message.Code] === undefined ? message.Code : $root.GoPlay.Core.Protocols.StatusCode[message.Code] : message.Code;
                    if (message.Message != null && message.hasOwnProperty("Message"))
                        object.Message = message.Message;
                    return object;
                };

                /**
                 * Converts this Status to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.Status
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                Status.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for Status
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.Status
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                Status.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Status";
                };

                return Status;
            })();

            Protocols.Session = (function() {

                /**
                 * Properties of a Session.
                 * @memberof GoPlay.Core.Protocols
                 * @interface ISession
                 * @property {string|null} [Guid] Session Guid
                 * @property {Object.<string,google.protobuf.IAny>|null} [Values] Session Values
                 */

                /**
                 * Constructs a new Session.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a Session.
                 * @implements ISession
                 * @constructor
                 * @param {GoPlay.Core.Protocols.ISession=} [properties] Properties to set
                 */
                function Session(properties) {
                    this.Values = {};
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * Session Guid.
                 * @member {string} Guid
                 * @memberof GoPlay.Core.Protocols.Session
                 * @instance
                 */
                Session.prototype.Guid = "";

                /**
                 * Session Values.
                 * @member {Object.<string,google.protobuf.IAny>} Values
                 * @memberof GoPlay.Core.Protocols.Session
                 * @instance
                 */
                Session.prototype.Values = $util.emptyObject;

                /**
                 * Creates a new Session instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {GoPlay.Core.Protocols.ISession=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.Session} Session instance
                 */
                Session.create = function create(properties) {
                    return new Session(properties);
                };

                /**
                 * Encodes the specified Session message. Does not implicitly {@link GoPlay.Core.Protocols.Session.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {GoPlay.Core.Protocols.ISession} message Session message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Session.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Guid != null && Object.hasOwnProperty.call(message, "Guid"))
                        writer.uint32(/* id 1, wireType 2 =*/10).string(message.Guid);
                    if (message.Values != null && Object.hasOwnProperty.call(message, "Values"))
                        for (var keys = Object.keys(message.Values), i = 0; i < keys.length; ++i) {
                            writer.uint32(/* id 2, wireType 2 =*/18).fork().uint32(/* id 1, wireType 2 =*/10).string(keys[i]);
                            $root.google.protobuf.Any.encode(message.Values[keys[i]], writer.uint32(/* id 2, wireType 2 =*/18).fork()).ldelim().ldelim();
                        }
                    return writer;
                };

                /**
                 * Encodes the specified Session message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Session.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {GoPlay.Core.Protocols.ISession} message Session message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Session.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a Session message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.Session} Session
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Session.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.Session(), key, value;
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Guid = reader.string();
                                break;
                            }
                        case 2: {
                                if (message.Values === $util.emptyObject)
                                    message.Values = {};
                                var end2 = reader.uint32() + reader.pos;
                                key = "";
                                value = null;
                                while (reader.pos < end2) {
                                    var tag2 = reader.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        key = reader.string();
                                        break;
                                    case 2:
                                        value = $root.google.protobuf.Any.decode(reader, reader.uint32());
                                        break;
                                    default:
                                        reader.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                message.Values[key] = value;
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a Session message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.Session} Session
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Session.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a Session message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                Session.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Guid != null && message.hasOwnProperty("Guid"))
                        if (!$util.isString(message.Guid))
                            return "Guid: string expected";
                    if (message.Values != null && message.hasOwnProperty("Values")) {
                        if (!$util.isObject(message.Values))
                            return "Values: object expected";
                        var key = Object.keys(message.Values);
                        for (var i = 0; i < key.length; ++i) {
                            var error = $root.google.protobuf.Any.verify(message.Values[key[i]]);
                            if (error)
                                return "Values." + error;
                        }
                    }
                    return null;
                };

                /**
                 * Creates a Session message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.Session} Session
                 */
                Session.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.Session)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.Session();
                    if (object.Guid != null)
                        message.Guid = String(object.Guid);
                    if (object.Values) {
                        if (typeof object.Values !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Session.Values: object expected");
                        message.Values = {};
                        for (var keys = Object.keys(object.Values), i = 0; i < keys.length; ++i) {
                            if (typeof object.Values[keys[i]] !== "object")
                                throw TypeError(".GoPlay.Core.Protocols.Session.Values: object expected");
                            message.Values[keys[i]] = $root.google.protobuf.Any.fromObject(object.Values[keys[i]]);
                        }
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a Session message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {GoPlay.Core.Protocols.Session} message Session
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                Session.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.objects || options.defaults)
                        object.Values = {};
                    if (options.defaults)
                        object.Guid = "";
                    if (message.Guid != null && message.hasOwnProperty("Guid"))
                        object.Guid = message.Guid;
                    var keys2;
                    if (message.Values && (keys2 = Object.keys(message.Values)).length) {
                        object.Values = {};
                        for (var j = 0; j < keys2.length; ++j)
                            object.Values[keys2[j]] = $root.google.protobuf.Any.toObject(message.Values[keys2[j]], options);
                    }
                    return object;
                };

                /**
                 * Converts this Session to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.Session
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                Session.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for Session
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.Session
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                Session.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Session";
                };

                return Session;
            })();

            Protocols.PackageInfo = (function() {

                /**
                 * Properties of a PackageInfo.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IPackageInfo
                 * @property {GoPlay.Core.Protocols.PackageType|null} [Type] PackageInfo Type
                 * @property {number|null} [Id] PackageInfo Id
                 * @property {GoPlay.Core.Protocols.EncodingType|null} [EncodingType] PackageInfo EncodingType
                 * @property {number|null} [Route] PackageInfo Route
                 * @property {number|null} [ContentSize] PackageInfo ContentSize
                 * @property {number|null} [ChunkCount] PackageInfo ChunkCount
                 * @property {number|null} [ChunkIndex] PackageInfo ChunkIndex
                 */

                /**
                 * Constructs a new PackageInfo.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a PackageInfo.
                 * @implements IPackageInfo
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IPackageInfo=} [properties] Properties to set
                 */
                function PackageInfo(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * PackageInfo Type.
                 * @member {GoPlay.Core.Protocols.PackageType} Type
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.Type = 0;

                /**
                 * PackageInfo Id.
                 * @member {number} Id
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.Id = 0;

                /**
                 * PackageInfo EncodingType.
                 * @member {GoPlay.Core.Protocols.EncodingType} EncodingType
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.EncodingType = 0;

                /**
                 * PackageInfo Route.
                 * @member {number} Route
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.Route = 0;

                /**
                 * PackageInfo ContentSize.
                 * @member {number} ContentSize
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.ContentSize = 0;

                /**
                 * PackageInfo ChunkCount.
                 * @member {number} ChunkCount
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.ChunkCount = 0;

                /**
                 * PackageInfo ChunkIndex.
                 * @member {number} ChunkIndex
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 */
                PackageInfo.prototype.ChunkIndex = 0;

                /**
                 * Creates a new PackageInfo instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {GoPlay.Core.Protocols.IPackageInfo=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.PackageInfo} PackageInfo instance
                 */
                PackageInfo.create = function create(properties) {
                    return new PackageInfo(properties);
                };

                /**
                 * Encodes the specified PackageInfo message. Does not implicitly {@link GoPlay.Core.Protocols.PackageInfo.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {GoPlay.Core.Protocols.IPackageInfo} message PackageInfo message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PackageInfo.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Type != null && Object.hasOwnProperty.call(message, "Type"))
                        writer.uint32(/* id 1, wireType 0 =*/8).int32(message.Type);
                    if (message.Id != null && Object.hasOwnProperty.call(message, "Id"))
                        writer.uint32(/* id 2, wireType 0 =*/16).uint32(message.Id);
                    if (message.EncodingType != null && Object.hasOwnProperty.call(message, "EncodingType"))
                        writer.uint32(/* id 3, wireType 0 =*/24).int32(message.EncodingType);
                    if (message.Route != null && Object.hasOwnProperty.call(message, "Route"))
                        writer.uint32(/* id 4, wireType 0 =*/32).uint32(message.Route);
                    if (message.ContentSize != null && Object.hasOwnProperty.call(message, "ContentSize"))
                        writer.uint32(/* id 5, wireType 0 =*/40).uint32(message.ContentSize);
                    if (message.ChunkCount != null && Object.hasOwnProperty.call(message, "ChunkCount"))
                        writer.uint32(/* id 6, wireType 0 =*/48).uint32(message.ChunkCount);
                    if (message.ChunkIndex != null && Object.hasOwnProperty.call(message, "ChunkIndex"))
                        writer.uint32(/* id 7, wireType 0 =*/56).uint32(message.ChunkIndex);
                    return writer;
                };

                /**
                 * Encodes the specified PackageInfo message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PackageInfo.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {GoPlay.Core.Protocols.IPackageInfo} message PackageInfo message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                PackageInfo.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a PackageInfo message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.PackageInfo} PackageInfo
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PackageInfo.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.PackageInfo();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Type = reader.int32();
                                break;
                            }
                        case 2: {
                                message.Id = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.EncodingType = reader.int32();
                                break;
                            }
                        case 4: {
                                message.Route = reader.uint32();
                                break;
                            }
                        case 5: {
                                message.ContentSize = reader.uint32();
                                break;
                            }
                        case 6: {
                                message.ChunkCount = reader.uint32();
                                break;
                            }
                        case 7: {
                                message.ChunkIndex = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a PackageInfo message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.PackageInfo} PackageInfo
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                PackageInfo.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a PackageInfo message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                PackageInfo.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Type != null && message.hasOwnProperty("Type"))
                        switch (message.Type) {
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
                    if (message.Id != null && message.hasOwnProperty("Id"))
                        if (!$util.isInteger(message.Id))
                            return "Id: integer expected";
                    if (message.EncodingType != null && message.hasOwnProperty("EncodingType"))
                        switch (message.EncodingType) {
                        default:
                            return "EncodingType: enum value expected";
                        case 0:
                        case 1:
                            break;
                        }
                    if (message.Route != null && message.hasOwnProperty("Route"))
                        if (!$util.isInteger(message.Route))
                            return "Route: integer expected";
                    if (message.ContentSize != null && message.hasOwnProperty("ContentSize"))
                        if (!$util.isInteger(message.ContentSize))
                            return "ContentSize: integer expected";
                    if (message.ChunkCount != null && message.hasOwnProperty("ChunkCount"))
                        if (!$util.isInteger(message.ChunkCount))
                            return "ChunkCount: integer expected";
                    if (message.ChunkIndex != null && message.hasOwnProperty("ChunkIndex"))
                        if (!$util.isInteger(message.ChunkIndex))
                            return "ChunkIndex: integer expected";
                    return null;
                };

                /**
                 * Creates a PackageInfo message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.PackageInfo} PackageInfo
                 */
                PackageInfo.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.PackageInfo)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.PackageInfo();
                    switch (object.Type) {
                    default:
                        if (typeof object.Type === "number") {
                            message.Type = object.Type;
                            break;
                        }
                        break;
                    case "HankShakeReq":
                    case 0:
                        message.Type = 0;
                        break;
                    case "HankShakeResp":
                    case 1:
                        message.Type = 1;
                        break;
                    case "Ping":
                    case 2:
                        message.Type = 2;
                        break;
                    case "Pong":
                    case 3:
                        message.Type = 3;
                        break;
                    case "Notify":
                    case 4:
                        message.Type = 4;
                        break;
                    case "Request":
                    case 5:
                        message.Type = 5;
                        break;
                    case "Response":
                    case 6:
                        message.Type = 6;
                        break;
                    case "Push":
                    case 7:
                        message.Type = 7;
                        break;
                    case "Kick":
                    case 8:
                        message.Type = 8;
                        break;
                    }
                    if (object.Id != null)
                        message.Id = object.Id >>> 0;
                    switch (object.EncodingType) {
                    default:
                        if (typeof object.EncodingType === "number") {
                            message.EncodingType = object.EncodingType;
                            break;
                        }
                        break;
                    case "Protobuf":
                    case 0:
                        message.EncodingType = 0;
                        break;
                    case "Json":
                    case 1:
                        message.EncodingType = 1;
                        break;
                    }
                    if (object.Route != null)
                        message.Route = object.Route >>> 0;
                    if (object.ContentSize != null)
                        message.ContentSize = object.ContentSize >>> 0;
                    if (object.ChunkCount != null)
                        message.ChunkCount = object.ChunkCount >>> 0;
                    if (object.ChunkIndex != null)
                        message.ChunkIndex = object.ChunkIndex >>> 0;
                    return message;
                };

                /**
                 * Creates a plain object from a PackageInfo message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {GoPlay.Core.Protocols.PackageInfo} message PackageInfo
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                PackageInfo.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults) {
                        object.Type = options.enums === String ? "HankShakeReq" : 0;
                        object.Id = 0;
                        object.EncodingType = options.enums === String ? "Protobuf" : 0;
                        object.Route = 0;
                        object.ContentSize = 0;
                        object.ChunkCount = 0;
                        object.ChunkIndex = 0;
                    }
                    if (message.Type != null && message.hasOwnProperty("Type"))
                        object.Type = options.enums === String ? $root.GoPlay.Core.Protocols.PackageType[message.Type] === undefined ? message.Type : $root.GoPlay.Core.Protocols.PackageType[message.Type] : message.Type;
                    if (message.Id != null && message.hasOwnProperty("Id"))
                        object.Id = message.Id;
                    if (message.EncodingType != null && message.hasOwnProperty("EncodingType"))
                        object.EncodingType = options.enums === String ? $root.GoPlay.Core.Protocols.EncodingType[message.EncodingType] === undefined ? message.EncodingType : $root.GoPlay.Core.Protocols.EncodingType[message.EncodingType] : message.EncodingType;
                    if (message.Route != null && message.hasOwnProperty("Route"))
                        object.Route = message.Route;
                    if (message.ContentSize != null && message.hasOwnProperty("ContentSize"))
                        object.ContentSize = message.ContentSize;
                    if (message.ChunkCount != null && message.hasOwnProperty("ChunkCount"))
                        object.ChunkCount = message.ChunkCount;
                    if (message.ChunkIndex != null && message.hasOwnProperty("ChunkIndex"))
                        object.ChunkIndex = message.ChunkIndex;
                    return object;
                };

                /**
                 * Converts this PackageInfo to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                PackageInfo.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for PackageInfo
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.PackageInfo
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                PackageInfo.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.PackageInfo";
                };

                return PackageInfo;
            })();

            Protocols.Header = (function() {

                /**
                 * Properties of a Header.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IHeader
                 * @property {GoPlay.Core.Protocols.IStatus|null} [Status] Header Status
                 * @property {GoPlay.Core.Protocols.ISession|null} [Session] Header Session
                 * @property {GoPlay.Core.Protocols.IPackageInfo|null} [PackageInfo] Header PackageInfo
                 */

                /**
                 * Constructs a new Header.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a Header.
                 * @implements IHeader
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IHeader=} [properties] Properties to set
                 */
                function Header(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * Header Status.
                 * @member {GoPlay.Core.Protocols.IStatus|null|undefined} Status
                 * @memberof GoPlay.Core.Protocols.Header
                 * @instance
                 */
                Header.prototype.Status = null;

                /**
                 * Header Session.
                 * @member {GoPlay.Core.Protocols.ISession|null|undefined} Session
                 * @memberof GoPlay.Core.Protocols.Header
                 * @instance
                 */
                Header.prototype.Session = null;

                /**
                 * Header PackageInfo.
                 * @member {GoPlay.Core.Protocols.IPackageInfo|null|undefined} PackageInfo
                 * @memberof GoPlay.Core.Protocols.Header
                 * @instance
                 */
                Header.prototype.PackageInfo = null;

                /**
                 * Creates a new Header instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {GoPlay.Core.Protocols.IHeader=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.Header} Header instance
                 */
                Header.create = function create(properties) {
                    return new Header(properties);
                };

                /**
                 * Encodes the specified Header message. Does not implicitly {@link GoPlay.Core.Protocols.Header.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {GoPlay.Core.Protocols.IHeader} message Header message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Header.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.Status != null && Object.hasOwnProperty.call(message, "Status"))
                        $root.GoPlay.Core.Protocols.Status.encode(message.Status, writer.uint32(/* id 1, wireType 2 =*/10).fork()).ldelim();
                    if (message.Session != null && Object.hasOwnProperty.call(message, "Session"))
                        $root.GoPlay.Core.Protocols.Session.encode(message.Session, writer.uint32(/* id 2, wireType 2 =*/18).fork()).ldelim();
                    if (message.PackageInfo != null && Object.hasOwnProperty.call(message, "PackageInfo"))
                        $root.GoPlay.Core.Protocols.PackageInfo.encode(message.PackageInfo, writer.uint32(/* id 3, wireType 2 =*/26).fork()).ldelim();
                    return writer;
                };

                /**
                 * Encodes the specified Header message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Header.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {GoPlay.Core.Protocols.IHeader} message Header message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                Header.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a Header message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.Header} Header
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Header.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.Header();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.Status = $root.GoPlay.Core.Protocols.Status.decode(reader, reader.uint32());
                                break;
                            }
                        case 2: {
                                message.Session = $root.GoPlay.Core.Protocols.Session.decode(reader, reader.uint32());
                                break;
                            }
                        case 3: {
                                message.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a Header message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.Header} Header
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                Header.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a Header message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                Header.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.Status != null && message.hasOwnProperty("Status")) {
                        var error = $root.GoPlay.Core.Protocols.Status.verify(message.Status);
                        if (error)
                            return "Status." + error;
                    }
                    if (message.Session != null && message.hasOwnProperty("Session")) {
                        var error = $root.GoPlay.Core.Protocols.Session.verify(message.Session);
                        if (error)
                            return "Session." + error;
                    }
                    if (message.PackageInfo != null && message.hasOwnProperty("PackageInfo")) {
                        var error = $root.GoPlay.Core.Protocols.PackageInfo.verify(message.PackageInfo);
                        if (error)
                            return "PackageInfo." + error;
                    }
                    return null;
                };

                /**
                 * Creates a Header message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.Header} Header
                 */
                Header.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.Header)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.Header();
                    if (object.Status != null) {
                        if (typeof object.Status !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.Status: object expected");
                        message.Status = $root.GoPlay.Core.Protocols.Status.fromObject(object.Status);
                    }
                    if (object.Session != null) {
                        if (typeof object.Session !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.Session: object expected");
                        message.Session = $root.GoPlay.Core.Protocols.Session.fromObject(object.Session);
                    }
                    if (object.PackageInfo != null) {
                        if (typeof object.PackageInfo !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.Header.PackageInfo: object expected");
                        message.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.fromObject(object.PackageInfo);
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a Header message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {GoPlay.Core.Protocols.Header} message Header
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                Header.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults) {
                        object.Status = null;
                        object.Session = null;
                        object.PackageInfo = null;
                    }
                    if (message.Status != null && message.hasOwnProperty("Status"))
                        object.Status = $root.GoPlay.Core.Protocols.Status.toObject(message.Status, options);
                    if (message.Session != null && message.hasOwnProperty("Session"))
                        object.Session = $root.GoPlay.Core.Protocols.Session.toObject(message.Session, options);
                    if (message.PackageInfo != null && message.hasOwnProperty("PackageInfo"))
                        object.PackageInfo = $root.GoPlay.Core.Protocols.PackageInfo.toObject(message.PackageInfo, options);
                    return object;
                };

                /**
                 * Converts this Header to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.Header
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                Header.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for Header
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.Header
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                Header.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.Header";
                };

                return Header;
            })();

            Protocols.ReqHankShake = (function() {

                /**
                 * Properties of a ReqHankShake.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IReqHankShake
                 * @property {string|null} [ClientVersion] ReqHankShake ClientVersion
                 * @property {GoPlay.Core.Protocols.ServerTag|null} [ServerTag] ReqHankShake ServerTag
                 * @property {string|null} [AppKey] ReqHankShake AppKey
                 */

                /**
                 * Constructs a new ReqHankShake.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a ReqHankShake.
                 * @implements IReqHankShake
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IReqHankShake=} [properties] Properties to set
                 */
                function ReqHankShake(properties) {
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * ReqHankShake ClientVersion.
                 * @member {string} ClientVersion
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @instance
                 */
                ReqHankShake.prototype.ClientVersion = "";

                /**
                 * ReqHankShake ServerTag.
                 * @member {GoPlay.Core.Protocols.ServerTag} ServerTag
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @instance
                 */
                ReqHankShake.prototype.ServerTag = 0;

                /**
                 * ReqHankShake AppKey.
                 * @member {string} AppKey
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @instance
                 */
                ReqHankShake.prototype.AppKey = "";

                /**
                 * Creates a new ReqHankShake instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IReqHankShake=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.ReqHankShake} ReqHankShake instance
                 */
                ReqHankShake.create = function create(properties) {
                    return new ReqHankShake(properties);
                };

                /**
                 * Encodes the specified ReqHankShake message. Does not implicitly {@link GoPlay.Core.Protocols.ReqHankShake.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IReqHankShake} message ReqHankShake message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                ReqHankShake.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.ClientVersion != null && Object.hasOwnProperty.call(message, "ClientVersion"))
                        writer.uint32(/* id 1, wireType 2 =*/10).string(message.ClientVersion);
                    if (message.ServerTag != null && Object.hasOwnProperty.call(message, "ServerTag"))
                        writer.uint32(/* id 2, wireType 0 =*/16).int32(message.ServerTag);
                    if (message.AppKey != null && Object.hasOwnProperty.call(message, "AppKey"))
                        writer.uint32(/* id 3, wireType 2 =*/26).string(message.AppKey);
                    return writer;
                };

                /**
                 * Encodes the specified ReqHankShake message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.ReqHankShake.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IReqHankShake} message ReqHankShake message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                ReqHankShake.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a ReqHankShake message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.ReqHankShake} ReqHankShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                ReqHankShake.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.ReqHankShake();
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.ClientVersion = reader.string();
                                break;
                            }
                        case 2: {
                                message.ServerTag = reader.int32();
                                break;
                            }
                        case 3: {
                                message.AppKey = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a ReqHankShake message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.ReqHankShake} ReqHankShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                ReqHankShake.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a ReqHankShake message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                ReqHankShake.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.ClientVersion != null && message.hasOwnProperty("ClientVersion"))
                        if (!$util.isString(message.ClientVersion))
                            return "ClientVersion: string expected";
                    if (message.ServerTag != null && message.hasOwnProperty("ServerTag"))
                        switch (message.ServerTag) {
                        default:
                            return "ServerTag: enum value expected";
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            break;
                        }
                    if (message.AppKey != null && message.hasOwnProperty("AppKey"))
                        if (!$util.isString(message.AppKey))
                            return "AppKey: string expected";
                    return null;
                };

                /**
                 * Creates a ReqHankShake message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.ReqHankShake} ReqHankShake
                 */
                ReqHankShake.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.ReqHankShake)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.ReqHankShake();
                    if (object.ClientVersion != null)
                        message.ClientVersion = String(object.ClientVersion);
                    switch (object.ServerTag) {
                    default:
                        if (typeof object.ServerTag === "number") {
                            message.ServerTag = object.ServerTag;
                            break;
                        }
                        break;
                    case "Empty":
                    case 0:
                        message.ServerTag = 0;
                        break;
                    case "FrontEnd":
                    case 1:
                        message.ServerTag = 1;
                        break;
                    case "BackEnd":
                    case 2:
                        message.ServerTag = 2;
                        break;
                    case "All":
                    case 3:
                        message.ServerTag = 3;
                        break;
                    }
                    if (object.AppKey != null)
                        message.AppKey = String(object.AppKey);
                    return message;
                };

                /**
                 * Creates a plain object from a ReqHankShake message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {GoPlay.Core.Protocols.ReqHankShake} message ReqHankShake
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                ReqHankShake.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.defaults) {
                        object.ClientVersion = "";
                        object.ServerTag = options.enums === String ? "Empty" : 0;
                        object.AppKey = "";
                    }
                    if (message.ClientVersion != null && message.hasOwnProperty("ClientVersion"))
                        object.ClientVersion = message.ClientVersion;
                    if (message.ServerTag != null && message.hasOwnProperty("ServerTag"))
                        object.ServerTag = options.enums === String ? $root.GoPlay.Core.Protocols.ServerTag[message.ServerTag] === undefined ? message.ServerTag : $root.GoPlay.Core.Protocols.ServerTag[message.ServerTag] : message.ServerTag;
                    if (message.AppKey != null && message.hasOwnProperty("AppKey"))
                        object.AppKey = message.AppKey;
                    return object;
                };

                /**
                 * Converts this ReqHankShake to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                ReqHankShake.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for ReqHankShake
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.ReqHankShake
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
                ReqHankShake.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/GoPlay.Core.Protocols.ReqHankShake";
                };

                return ReqHankShake;
            })();

            Protocols.RespHandShake = (function() {

                /**
                 * Properties of a RespHandShake.
                 * @memberof GoPlay.Core.Protocols
                 * @interface IRespHandShake
                 * @property {string|null} [ServerVersion] RespHandShake ServerVersion
                 * @property {number|null} [HeartBeatInterval] RespHandShake HeartBeatInterval
                 * @property {Object.<string,number>|null} [Routes] RespHandShake Routes
                 */

                /**
                 * Constructs a new RespHandShake.
                 * @memberof GoPlay.Core.Protocols
                 * @classdesc Represents a RespHandShake.
                 * @implements IRespHandShake
                 * @constructor
                 * @param {GoPlay.Core.Protocols.IRespHandShake=} [properties] Properties to set
                 */
                function RespHandShake(properties) {
                    this.Routes = {};
                    if (properties)
                        for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                /**
                 * RespHandShake ServerVersion.
                 * @member {string} ServerVersion
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @instance
                 */
                RespHandShake.prototype.ServerVersion = "";

                /**
                 * RespHandShake HeartBeatInterval.
                 * @member {number} HeartBeatInterval
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @instance
                 */
                RespHandShake.prototype.HeartBeatInterval = 0;

                /**
                 * RespHandShake Routes.
                 * @member {Object.<string,number>} Routes
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @instance
                 */
                RespHandShake.prototype.Routes = $util.emptyObject;

                /**
                 * Creates a new RespHandShake instance using the specified properties.
                 * @function create
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IRespHandShake=} [properties] Properties to set
                 * @returns {GoPlay.Core.Protocols.RespHandShake} RespHandShake instance
                 */
                RespHandShake.create = function create(properties) {
                    return new RespHandShake(properties);
                };

                /**
                 * Encodes the specified RespHandShake message. Does not implicitly {@link GoPlay.Core.Protocols.RespHandShake.verify|verify} messages.
                 * @function encode
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IRespHandShake} message RespHandShake message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                RespHandShake.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.ServerVersion != null && Object.hasOwnProperty.call(message, "ServerVersion"))
                        writer.uint32(/* id 1, wireType 2 =*/10).string(message.ServerVersion);
                    if (message.HeartBeatInterval != null && Object.hasOwnProperty.call(message, "HeartBeatInterval"))
                        writer.uint32(/* id 2, wireType 0 =*/16).uint32(message.HeartBeatInterval);
                    if (message.Routes != null && Object.hasOwnProperty.call(message, "Routes"))
                        for (var keys = Object.keys(message.Routes), i = 0; i < keys.length; ++i)
                            writer.uint32(/* id 3, wireType 2 =*/26).fork().uint32(/* id 1, wireType 2 =*/10).string(keys[i]).uint32(/* id 2, wireType 0 =*/16).uint32(message.Routes[keys[i]]).ldelim();
                    return writer;
                };

                /**
                 * Encodes the specified RespHandShake message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.RespHandShake.verify|verify} messages.
                 * @function encodeDelimited
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {GoPlay.Core.Protocols.IRespHandShake} message RespHandShake message or plain object to encode
                 * @param {$protobuf.Writer} [writer] Writer to encode to
                 * @returns {$protobuf.Writer} Writer
                 */
                RespHandShake.encodeDelimited = function encodeDelimited(message, writer) {
                    return this.encode(message, writer).ldelim();
                };

                /**
                 * Decodes a RespHandShake message from the specified reader or buffer.
                 * @function decode
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @param {number} [length] Message length if known beforehand
                 * @returns {GoPlay.Core.Protocols.RespHandShake} RespHandShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                RespHandShake.decode = function decode(reader, length) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    var end = length === undefined ? reader.len : reader.pos + length, message = new $root.GoPlay.Core.Protocols.RespHandShake(), key, value;
                    while (reader.pos < end) {
                        var tag = reader.uint32();
                        switch (tag >>> 3) {
                        case 1: {
                                message.ServerVersion = reader.string();
                                break;
                            }
                        case 2: {
                                message.HeartBeatInterval = reader.uint32();
                                break;
                            }
                        case 3: {
                                if (message.Routes === $util.emptyObject)
                                    message.Routes = {};
                                var end2 = reader.uint32() + reader.pos;
                                key = "";
                                value = 0;
                                while (reader.pos < end2) {
                                    var tag2 = reader.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        key = reader.string();
                                        break;
                                    case 2:
                                        value = reader.uint32();
                                        break;
                                    default:
                                        reader.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                message.Routes[key] = value;
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                /**
                 * Decodes a RespHandShake message from the specified reader or buffer, length delimited.
                 * @function decodeDelimited
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
                 * @returns {GoPlay.Core.Protocols.RespHandShake} RespHandShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                RespHandShake.decodeDelimited = function decodeDelimited(reader) {
                    if (!(reader instanceof $Reader))
                        reader = new $Reader(reader);
                    return this.decode(reader, reader.uint32());
                };

                /**
                 * Verifies a RespHandShake message.
                 * @function verify
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {Object.<string,*>} message Plain object to verify
                 * @returns {string|null} `null` if valid, otherwise the reason why it is not
                 */
                RespHandShake.verify = function verify(message) {
                    if (typeof message !== "object" || message === null)
                        return "object expected";
                    if (message.ServerVersion != null && message.hasOwnProperty("ServerVersion"))
                        if (!$util.isString(message.ServerVersion))
                            return "ServerVersion: string expected";
                    if (message.HeartBeatInterval != null && message.hasOwnProperty("HeartBeatInterval"))
                        if (!$util.isInteger(message.HeartBeatInterval))
                            return "HeartBeatInterval: integer expected";
                    if (message.Routes != null && message.hasOwnProperty("Routes")) {
                        if (!$util.isObject(message.Routes))
                            return "Routes: object expected";
                        var key = Object.keys(message.Routes);
                        for (var i = 0; i < key.length; ++i)
                            if (!$util.isInteger(message.Routes[key[i]]))
                                return "Routes: integer{k:string} expected";
                    }
                    return null;
                };

                /**
                 * Creates a RespHandShake message from a plain object. Also converts values to their respective internal types.
                 * @function fromObject
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {Object.<string,*>} object Plain object
                 * @returns {GoPlay.Core.Protocols.RespHandShake} RespHandShake
                 */
                RespHandShake.fromObject = function fromObject(object) {
                    if (object instanceof $root.GoPlay.Core.Protocols.RespHandShake)
                        return object;
                    var message = new $root.GoPlay.Core.Protocols.RespHandShake();
                    if (object.ServerVersion != null)
                        message.ServerVersion = String(object.ServerVersion);
                    if (object.HeartBeatInterval != null)
                        message.HeartBeatInterval = object.HeartBeatInterval >>> 0;
                    if (object.Routes) {
                        if (typeof object.Routes !== "object")
                            throw TypeError(".GoPlay.Core.Protocols.RespHandShake.Routes: object expected");
                        message.Routes = {};
                        for (var keys = Object.keys(object.Routes), i = 0; i < keys.length; ++i)
                            message.Routes[keys[i]] = object.Routes[keys[i]] >>> 0;
                    }
                    return message;
                };

                /**
                 * Creates a plain object from a RespHandShake message. Also converts values to other types if specified.
                 * @function toObject
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {GoPlay.Core.Protocols.RespHandShake} message RespHandShake
                 * @param {$protobuf.IConversionOptions} [options] Conversion options
                 * @returns {Object.<string,*>} Plain object
                 */
                RespHandShake.toObject = function toObject(message, options) {
                    if (!options)
                        options = {};
                    var object = {};
                    if (options.objects || options.defaults)
                        object.Routes = {};
                    if (options.defaults) {
                        object.ServerVersion = "";
                        object.HeartBeatInterval = 0;
                    }
                    if (message.ServerVersion != null && message.hasOwnProperty("ServerVersion"))
                        object.ServerVersion = message.ServerVersion;
                    if (message.HeartBeatInterval != null && message.hasOwnProperty("HeartBeatInterval"))
                        object.HeartBeatInterval = message.HeartBeatInterval;
                    var keys2;
                    if (message.Routes && (keys2 = Object.keys(message.Routes)).length) {
                        object.Routes = {};
                        for (var j = 0; j < keys2.length; ++j)
                            object.Routes[keys2[j]] = message.Routes[keys2[j]];
                    }
                    return object;
                };

                /**
                 * Converts this RespHandShake to JSON.
                 * @function toJSON
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @instance
                 * @returns {Object.<string,*>} JSON object
                 */
                RespHandShake.prototype.toJSON = function toJSON() {
                    return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
                };

                /**
                 * Gets the default type url for RespHandShake
                 * @function getTypeUrl
                 * @memberof GoPlay.Core.Protocols.RespHandShake
                 * @static
                 * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns {string} The default type url
                 */
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

    /**
     * Namespace google.
     * @exports google
     * @namespace
     */
    var google = {};

    google.protobuf = (function() {

        /**
         * Namespace protobuf.
         * @memberof google
         * @namespace
         */
        var protobuf = {};

        protobuf.Timestamp = (function() {

            /**
             * Properties of a Timestamp.
             * @memberof google.protobuf
             * @interface ITimestamp
             * @property {number|Long|null} [seconds] Timestamp seconds
             * @property {number|null} [nanos] Timestamp nanos
             */

            /**
             * Constructs a new Timestamp.
             * @memberof google.protobuf
             * @classdesc Represents a Timestamp.
             * @implements ITimestamp
             * @constructor
             * @param {google.protobuf.ITimestamp=} [properties] Properties to set
             */
            function Timestamp(properties) {
                if (properties)
                    for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                        if (properties[keys[i]] != null)
                            this[keys[i]] = properties[keys[i]];
            }

            /**
             * Timestamp seconds.
             * @member {number|Long} seconds
             * @memberof google.protobuf.Timestamp
             * @instance
             */
            Timestamp.prototype.seconds = $util.Long ? $util.Long.fromBits(0,0,false) : 0;

            /**
             * Timestamp nanos.
             * @member {number} nanos
             * @memberof google.protobuf.Timestamp
             * @instance
             */
            Timestamp.prototype.nanos = 0;

            /**
             * Creates a new Timestamp instance using the specified properties.
             * @function create
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {google.protobuf.ITimestamp=} [properties] Properties to set
             * @returns {google.protobuf.Timestamp} Timestamp instance
             */
            Timestamp.create = function create(properties) {
                return new Timestamp(properties);
            };

            /**
             * Encodes the specified Timestamp message. Does not implicitly {@link google.protobuf.Timestamp.verify|verify} messages.
             * @function encode
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {google.protobuf.ITimestamp} message Timestamp message or plain object to encode
             * @param {$protobuf.Writer} [writer] Writer to encode to
             * @returns {$protobuf.Writer} Writer
             */
            Timestamp.encode = function encode(message, writer) {
                if (!writer)
                    writer = $Writer.create();
                if (message.seconds != null && Object.hasOwnProperty.call(message, "seconds"))
                    writer.uint32(/* id 1, wireType 0 =*/8).int64(message.seconds);
                if (message.nanos != null && Object.hasOwnProperty.call(message, "nanos"))
                    writer.uint32(/* id 2, wireType 0 =*/16).int32(message.nanos);
                return writer;
            };

            /**
             * Encodes the specified Timestamp message, length delimited. Does not implicitly {@link google.protobuf.Timestamp.verify|verify} messages.
             * @function encodeDelimited
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {google.protobuf.ITimestamp} message Timestamp message or plain object to encode
             * @param {$protobuf.Writer} [writer] Writer to encode to
             * @returns {$protobuf.Writer} Writer
             */
            Timestamp.encodeDelimited = function encodeDelimited(message, writer) {
                return this.encode(message, writer).ldelim();
            };

            /**
             * Decodes a Timestamp message from the specified reader or buffer.
             * @function decode
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
             * @param {number} [length] Message length if known beforehand
             * @returns {google.protobuf.Timestamp} Timestamp
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            Timestamp.decode = function decode(reader, length) {
                if (!(reader instanceof $Reader))
                    reader = $Reader.create(reader);
                var end = length === undefined ? reader.len : reader.pos + length, message = new $root.google.protobuf.Timestamp();
                while (reader.pos < end) {
                    var tag = reader.uint32();
                    switch (tag >>> 3) {
                    case 1: {
                            message.seconds = reader.int64();
                            break;
                        }
                    case 2: {
                            message.nanos = reader.int32();
                            break;
                        }
                    default:
                        reader.skipType(tag & 7);
                        break;
                    }
                }
                return message;
            };

            /**
             * Decodes a Timestamp message from the specified reader or buffer, length delimited.
             * @function decodeDelimited
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
             * @returns {google.protobuf.Timestamp} Timestamp
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            Timestamp.decodeDelimited = function decodeDelimited(reader) {
                if (!(reader instanceof $Reader))
                    reader = new $Reader(reader);
                return this.decode(reader, reader.uint32());
            };

            /**
             * Verifies a Timestamp message.
             * @function verify
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {Object.<string,*>} message Plain object to verify
             * @returns {string|null} `null` if valid, otherwise the reason why it is not
             */
            Timestamp.verify = function verify(message) {
                if (typeof message !== "object" || message === null)
                    return "object expected";
                if (message.seconds != null && message.hasOwnProperty("seconds"))
                    if (!$util.isInteger(message.seconds) && !(message.seconds && $util.isInteger(message.seconds.low) && $util.isInteger(message.seconds.high)))
                        return "seconds: integer|Long expected";
                if (message.nanos != null && message.hasOwnProperty("nanos"))
                    if (!$util.isInteger(message.nanos))
                        return "nanos: integer expected";
                return null;
            };

            /**
             * Creates a Timestamp message from a plain object. Also converts values to their respective internal types.
             * @function fromObject
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {Object.<string,*>} object Plain object
             * @returns {google.protobuf.Timestamp} Timestamp
             */
            Timestamp.fromObject = function fromObject(object) {
                if (object instanceof $root.google.protobuf.Timestamp)
                    return object;
                var message = new $root.google.protobuf.Timestamp();
                if (object.seconds != null)
                    if ($util.Long)
                        (message.seconds = $util.Long.fromValue(object.seconds)).unsigned = false;
                    else if (typeof object.seconds === "string")
                        message.seconds = parseInt(object.seconds, 10);
                    else if (typeof object.seconds === "number")
                        message.seconds = object.seconds;
                    else if (typeof object.seconds === "object")
                        message.seconds = new $util.LongBits(object.seconds.low >>> 0, object.seconds.high >>> 0).toNumber();
                if (object.nanos != null)
                    message.nanos = object.nanos | 0;
                return message;
            };

            /**
             * Creates a plain object from a Timestamp message. Also converts values to other types if specified.
             * @function toObject
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {google.protobuf.Timestamp} message Timestamp
             * @param {$protobuf.IConversionOptions} [options] Conversion options
             * @returns {Object.<string,*>} Plain object
             */
            Timestamp.toObject = function toObject(message, options) {
                if (!options)
                    options = {};
                var object = {};
                if (options.defaults) {
                    if ($util.Long) {
                        var long = new $util.Long(0, 0, false);
                        object.seconds = options.longs === String ? long.toString() : options.longs === Number ? long.toNumber() : long;
                    } else
                        object.seconds = options.longs === String ? "0" : 0;
                    object.nanos = 0;
                }
                if (message.seconds != null && message.hasOwnProperty("seconds"))
                    if (typeof message.seconds === "number")
                        object.seconds = options.longs === String ? String(message.seconds) : message.seconds;
                    else
                        object.seconds = options.longs === String ? $util.Long.prototype.toString.call(message.seconds) : options.longs === Number ? new $util.LongBits(message.seconds.low >>> 0, message.seconds.high >>> 0).toNumber() : message.seconds;
                if (message.nanos != null && message.hasOwnProperty("nanos"))
                    object.nanos = message.nanos;
                return object;
            };

            /**
             * Converts this Timestamp to JSON.
             * @function toJSON
             * @memberof google.protobuf.Timestamp
             * @instance
             * @returns {Object.<string,*>} JSON object
             */
            Timestamp.prototype.toJSON = function toJSON() {
                return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
            };

            /**
             * Gets the default type url for Timestamp
             * @function getTypeUrl
             * @memberof google.protobuf.Timestamp
             * @static
             * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
             * @returns {string} The default type url
             */
            Timestamp.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                if (typeUrlPrefix === undefined) {
                    typeUrlPrefix = "type.googleapis.com";
                }
                return typeUrlPrefix + "/google.protobuf.Timestamp";
            };

            return Timestamp;
        })();

        protobuf.Any = (function() {

            /**
             * Properties of an Any.
             * @memberof google.protobuf
             * @interface IAny
             * @property {string|null} [type_url] Any type_url
             * @property {Uint8Array|null} [value] Any value
             */

            /**
             * Constructs a new Any.
             * @memberof google.protobuf
             * @classdesc Represents an Any.
             * @implements IAny
             * @constructor
             * @param {google.protobuf.IAny=} [properties] Properties to set
             */
            function Any(properties) {
                if (properties)
                    for (var keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                        if (properties[keys[i]] != null)
                            this[keys[i]] = properties[keys[i]];
            }

            /**
             * Any type_url.
             * @member {string} type_url
             * @memberof google.protobuf.Any
             * @instance
             */
            Any.prototype.type_url = "";

            /**
             * Any value.
             * @member {Uint8Array} value
             * @memberof google.protobuf.Any
             * @instance
             */
            Any.prototype.value = $util.newBuffer([]);

            /**
             * Creates a new Any instance using the specified properties.
             * @function create
             * @memberof google.protobuf.Any
             * @static
             * @param {google.protobuf.IAny=} [properties] Properties to set
             * @returns {google.protobuf.Any} Any instance
             */
            Any.create = function create(properties) {
                return new Any(properties);
            };

            /**
             * Encodes the specified Any message. Does not implicitly {@link google.protobuf.Any.verify|verify} messages.
             * @function encode
             * @memberof google.protobuf.Any
             * @static
             * @param {google.protobuf.IAny} message Any message or plain object to encode
             * @param {$protobuf.Writer} [writer] Writer to encode to
             * @returns {$protobuf.Writer} Writer
             */
            Any.encode = function encode(message, writer) {
                if (!writer)
                    writer = $Writer.create();
                if (message.type_url != null && Object.hasOwnProperty.call(message, "type_url"))
                    writer.uint32(/* id 1, wireType 2 =*/10).string(message.type_url);
                if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                    writer.uint32(/* id 2, wireType 2 =*/18).bytes(message.value);
                return writer;
            };

            /**
             * Encodes the specified Any message, length delimited. Does not implicitly {@link google.protobuf.Any.verify|verify} messages.
             * @function encodeDelimited
             * @memberof google.protobuf.Any
             * @static
             * @param {google.protobuf.IAny} message Any message or plain object to encode
             * @param {$protobuf.Writer} [writer] Writer to encode to
             * @returns {$protobuf.Writer} Writer
             */
            Any.encodeDelimited = function encodeDelimited(message, writer) {
                return this.encode(message, writer).ldelim();
            };

            /**
             * Decodes an Any message from the specified reader or buffer.
             * @function decode
             * @memberof google.protobuf.Any
             * @static
             * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
             * @param {number} [length] Message length if known beforehand
             * @returns {google.protobuf.Any} Any
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            Any.decode = function decode(reader, length) {
                if (!(reader instanceof $Reader))
                    reader = $Reader.create(reader);
                var end = length === undefined ? reader.len : reader.pos + length, message = new $root.google.protobuf.Any();
                while (reader.pos < end) {
                    var tag = reader.uint32();
                    switch (tag >>> 3) {
                    case 1: {
                            message.type_url = reader.string();
                            break;
                        }
                    case 2: {
                            message.value = reader.bytes();
                            break;
                        }
                    default:
                        reader.skipType(tag & 7);
                        break;
                    }
                }
                return message;
            };

            /**
             * Decodes an Any message from the specified reader or buffer, length delimited.
             * @function decodeDelimited
             * @memberof google.protobuf.Any
             * @static
             * @param {$protobuf.Reader|Uint8Array} reader Reader or buffer to decode from
             * @returns {google.protobuf.Any} Any
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            Any.decodeDelimited = function decodeDelimited(reader) {
                if (!(reader instanceof $Reader))
                    reader = new $Reader(reader);
                return this.decode(reader, reader.uint32());
            };

            /**
             * Verifies an Any message.
             * @function verify
             * @memberof google.protobuf.Any
             * @static
             * @param {Object.<string,*>} message Plain object to verify
             * @returns {string|null} `null` if valid, otherwise the reason why it is not
             */
            Any.verify = function verify(message) {
                if (typeof message !== "object" || message === null)
                    return "object expected";
                if (message.type_url != null && message.hasOwnProperty("type_url"))
                    if (!$util.isString(message.type_url))
                        return "type_url: string expected";
                if (message.value != null && message.hasOwnProperty("value"))
                    if (!(message.value && typeof message.value.length === "number" || $util.isString(message.value)))
                        return "value: buffer expected";
                return null;
            };

            /**
             * Creates an Any message from a plain object. Also converts values to their respective internal types.
             * @function fromObject
             * @memberof google.protobuf.Any
             * @static
             * @param {Object.<string,*>} object Plain object
             * @returns {google.protobuf.Any} Any
             */
            Any.fromObject = function fromObject(object) {
                if (object instanceof $root.google.protobuf.Any)
                    return object;
                var message = new $root.google.protobuf.Any();
                if (object.type_url != null)
                    message.type_url = String(object.type_url);
                if (object.value != null)
                    if (typeof object.value === "string")
                        $util.base64.decode(object.value, message.value = $util.newBuffer($util.base64.length(object.value)), 0);
                    else if (object.value.length >= 0)
                        message.value = object.value;
                return message;
            };

            /**
             * Creates a plain object from an Any message. Also converts values to other types if specified.
             * @function toObject
             * @memberof google.protobuf.Any
             * @static
             * @param {google.protobuf.Any} message Any
             * @param {$protobuf.IConversionOptions} [options] Conversion options
             * @returns {Object.<string,*>} Plain object
             */
            Any.toObject = function toObject(message, options) {
                if (!options)
                    options = {};
                var object = {};
                if (options.defaults) {
                    object.type_url = "";
                    if (options.bytes === String)
                        object.value = "";
                    else {
                        object.value = [];
                        if (options.bytes !== Array)
                            object.value = $util.newBuffer(object.value);
                    }
                }
                if (message.type_url != null && message.hasOwnProperty("type_url"))
                    object.type_url = message.type_url;
                if (message.value != null && message.hasOwnProperty("value"))
                    object.value = options.bytes === String ? $util.base64.encode(message.value, 0, message.value.length) : options.bytes === Array ? Array.prototype.slice.call(message.value) : message.value;
                return object;
            };

            /**
             * Converts this Any to JSON.
             * @function toJSON
             * @memberof google.protobuf.Any
             * @instance
             * @returns {Object.<string,*>} JSON object
             */
            Any.prototype.toJSON = function toJSON() {
                return this.constructor.toObject(this, $protobuf.util.toJSONOptions);
            };

            /**
             * Gets the default type url for Any
             * @function getTypeUrl
             * @memberof google.protobuf.Any
             * @static
             * @param {string} [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
             * @returns {string} The default type url
             */
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

module.exports = $root;
