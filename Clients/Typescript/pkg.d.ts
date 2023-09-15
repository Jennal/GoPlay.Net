import * as $protobuf from "protobufjs";
import Long = require("long");
/** Namespace GoPlay. */
export namespace GoPlay {

    /** Namespace Core. */
    namespace Core {

        /** Namespace Protocols. */
        namespace Protocols {

            /** Properties of a PbAny. */
            interface IPbAny {

                /** PbAny Value */
                Value?: (google.protobuf.IAny|null);
            }

            /** Represents a PbAny. */
            class PbAny implements IPbAny {

                /**
                 * Constructs a new PbAny.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbAny);

                /** PbAny Value. */
                public Value?: (google.protobuf.IAny|null);

                /**
                 * Creates a new PbAny instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbAny instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbAny): GoPlay.Core.Protocols.PbAny;

                /**
                 * Encodes the specified PbAny message. Does not implicitly {@link GoPlay.Core.Protocols.PbAny.verify|verify} messages.
                 * @param message PbAny message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbAny, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbAny message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbAny.verify|verify} messages.
                 * @param message PbAny message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbAny, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbAny message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbAny
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbAny;

                /**
                 * Decodes a PbAny message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbAny
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbAny;

                /**
                 * Verifies a PbAny message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbAny message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbAny
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbAny;

                /**
                 * Creates a plain object from a PbAny message. Also converts values to other types if specified.
                 * @param message PbAny
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbAny, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbAny to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbAny
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbTime. */
            interface IPbTime {

                /** PbTime Value */
                Value?: (google.protobuf.ITimestamp|null);
            }

            /** Represents a PbTime. */
            class PbTime implements IPbTime {

                /**
                 * Constructs a new PbTime.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbTime);

                /** PbTime Value. */
                public Value?: (google.protobuf.ITimestamp|null);

                /**
                 * Creates a new PbTime instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbTime instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbTime): GoPlay.Core.Protocols.PbTime;

                /**
                 * Encodes the specified PbTime message. Does not implicitly {@link GoPlay.Core.Protocols.PbTime.verify|verify} messages.
                 * @param message PbTime message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbTime, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbTime message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbTime.verify|verify} messages.
                 * @param message PbTime message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbTime, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbTime message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbTime
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbTime;

                /**
                 * Decodes a PbTime message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbTime
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbTime;

                /**
                 * Verifies a PbTime message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbTime message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbTime
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbTime;

                /**
                 * Creates a plain object from a PbTime message. Also converts values to other types if specified.
                 * @param message PbTime
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbTime, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbTime to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbTime
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbString. */
            interface IPbString {

                /** PbString Value */
                Value?: (string|null);
            }

            /** Represents a PbString. */
            class PbString implements IPbString {

                /**
                 * Constructs a new PbString.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbString);

                /** PbString Value. */
                public Value: string;

                /**
                 * Creates a new PbString instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbString instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbString): GoPlay.Core.Protocols.PbString;

                /**
                 * Encodes the specified PbString message. Does not implicitly {@link GoPlay.Core.Protocols.PbString.verify|verify} messages.
                 * @param message PbString message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbString, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbString message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbString.verify|verify} messages.
                 * @param message PbString message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbString, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbString message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbString
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbString;

                /**
                 * Decodes a PbString message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbString
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbString;

                /**
                 * Verifies a PbString message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbString message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbString
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbString;

                /**
                 * Creates a plain object from a PbString message. Also converts values to other types if specified.
                 * @param message PbString
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbString, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbString to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbString
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbInt. */
            interface IPbInt {

                /** PbInt Value */
                Value?: (number|null);
            }

            /** Represents a PbInt. */
            class PbInt implements IPbInt {

                /**
                 * Constructs a new PbInt.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbInt);

                /** PbInt Value. */
                public Value: number;

                /**
                 * Creates a new PbInt instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbInt instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbInt): GoPlay.Core.Protocols.PbInt;

                /**
                 * Encodes the specified PbInt message. Does not implicitly {@link GoPlay.Core.Protocols.PbInt.verify|verify} messages.
                 * @param message PbInt message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbInt, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbInt message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbInt.verify|verify} messages.
                 * @param message PbInt message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbInt, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbInt message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbInt
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbInt;

                /**
                 * Decodes a PbInt message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbInt
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbInt;

                /**
                 * Verifies a PbInt message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbInt message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbInt
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbInt;

                /**
                 * Creates a plain object from a PbInt message. Also converts values to other types if specified.
                 * @param message PbInt
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbInt, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbInt to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbInt
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbLong. */
            interface IPbLong {

                /** PbLong Value */
                Value?: (number|Long|null);
            }

            /** Represents a PbLong. */
            class PbLong implements IPbLong {

                /**
                 * Constructs a new PbLong.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbLong);

                /** PbLong Value. */
                public Value: (number|Long);

                /**
                 * Creates a new PbLong instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbLong instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbLong): GoPlay.Core.Protocols.PbLong;

                /**
                 * Encodes the specified PbLong message. Does not implicitly {@link GoPlay.Core.Protocols.PbLong.verify|verify} messages.
                 * @param message PbLong message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbLong, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbLong message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbLong.verify|verify} messages.
                 * @param message PbLong message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbLong, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbLong message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbLong
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbLong;

                /**
                 * Decodes a PbLong message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbLong
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbLong;

                /**
                 * Verifies a PbLong message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbLong message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbLong
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbLong;

                /**
                 * Creates a plain object from a PbLong message. Also converts values to other types if specified.
                 * @param message PbLong
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbLong, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbLong to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbLong
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbFloat. */
            interface IPbFloat {

                /** PbFloat Value */
                Value?: (number|null);
            }

            /** Represents a PbFloat. */
            class PbFloat implements IPbFloat {

                /**
                 * Constructs a new PbFloat.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbFloat);

                /** PbFloat Value. */
                public Value: number;

                /**
                 * Creates a new PbFloat instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbFloat instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbFloat): GoPlay.Core.Protocols.PbFloat;

                /**
                 * Encodes the specified PbFloat message. Does not implicitly {@link GoPlay.Core.Protocols.PbFloat.verify|verify} messages.
                 * @param message PbFloat message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbFloat, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbFloat message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbFloat.verify|verify} messages.
                 * @param message PbFloat message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbFloat, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbFloat message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbFloat
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbFloat;

                /**
                 * Decodes a PbFloat message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbFloat
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbFloat;

                /**
                 * Verifies a PbFloat message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbFloat message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbFloat
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbFloat;

                /**
                 * Creates a plain object from a PbFloat message. Also converts values to other types if specified.
                 * @param message PbFloat
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbFloat, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbFloat to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbFloat
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbBool. */
            interface IPbBool {

                /** PbBool Value */
                Value?: (boolean|null);
            }

            /** Represents a PbBool. */
            class PbBool implements IPbBool {

                /**
                 * Constructs a new PbBool.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbBool);

                /** PbBool Value. */
                public Value: boolean;

                /**
                 * Creates a new PbBool instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbBool instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbBool): GoPlay.Core.Protocols.PbBool;

                /**
                 * Encodes the specified PbBool message. Does not implicitly {@link GoPlay.Core.Protocols.PbBool.verify|verify} messages.
                 * @param message PbBool message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbBool, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbBool message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbBool.verify|verify} messages.
                 * @param message PbBool message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbBool, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbBool message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbBool
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbBool;

                /**
                 * Decodes a PbBool message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbBool
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbBool;

                /**
                 * Verifies a PbBool message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbBool message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbBool
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbBool;

                /**
                 * Creates a plain object from a PbBool message. Also converts values to other types if specified.
                 * @param message PbBool
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbBool, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbBool to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbBool
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbStringArray. */
            interface IPbStringArray {

                /** PbStringArray Value */
                Value?: (string[]|null);
            }

            /** Represents a PbStringArray. */
            class PbStringArray implements IPbStringArray {

                /**
                 * Constructs a new PbStringArray.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbStringArray);

                /** PbStringArray Value. */
                public Value: string[];

                /**
                 * Creates a new PbStringArray instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbStringArray instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbStringArray): GoPlay.Core.Protocols.PbStringArray;

                /**
                 * Encodes the specified PbStringArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbStringArray.verify|verify} messages.
                 * @param message PbStringArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbStringArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbStringArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbStringArray.verify|verify} messages.
                 * @param message PbStringArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbStringArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbStringArray message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbStringArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbStringArray;

                /**
                 * Decodes a PbStringArray message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbStringArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbStringArray;

                /**
                 * Verifies a PbStringArray message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbStringArray message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbStringArray
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbStringArray;

                /**
                 * Creates a plain object from a PbStringArray message. Also converts values to other types if specified.
                 * @param message PbStringArray
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbStringArray, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbStringArray to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbStringArray
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbIntArray. */
            interface IPbIntArray {

                /** PbIntArray Value */
                Value?: (number[]|null);
            }

            /** Represents a PbIntArray. */
            class PbIntArray implements IPbIntArray {

                /**
                 * Constructs a new PbIntArray.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbIntArray);

                /** PbIntArray Value. */
                public Value: number[];

                /**
                 * Creates a new PbIntArray instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbIntArray instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbIntArray): GoPlay.Core.Protocols.PbIntArray;

                /**
                 * Encodes the specified PbIntArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbIntArray.verify|verify} messages.
                 * @param message PbIntArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbIntArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbIntArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbIntArray.verify|verify} messages.
                 * @param message PbIntArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbIntArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbIntArray message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbIntArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbIntArray;

                /**
                 * Decodes a PbIntArray message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbIntArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbIntArray;

                /**
                 * Verifies a PbIntArray message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbIntArray message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbIntArray
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbIntArray;

                /**
                 * Creates a plain object from a PbIntArray message. Also converts values to other types if specified.
                 * @param message PbIntArray
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbIntArray, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbIntArray to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbIntArray
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbFloatArray. */
            interface IPbFloatArray {

                /** PbFloatArray Value */
                Value?: (number[]|null);
            }

            /** Represents a PbFloatArray. */
            class PbFloatArray implements IPbFloatArray {

                /**
                 * Constructs a new PbFloatArray.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbFloatArray);

                /** PbFloatArray Value. */
                public Value: number[];

                /**
                 * Creates a new PbFloatArray instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbFloatArray instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbFloatArray): GoPlay.Core.Protocols.PbFloatArray;

                /**
                 * Encodes the specified PbFloatArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbFloatArray.verify|verify} messages.
                 * @param message PbFloatArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbFloatArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbFloatArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbFloatArray.verify|verify} messages.
                 * @param message PbFloatArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbFloatArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbFloatArray message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbFloatArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbFloatArray;

                /**
                 * Decodes a PbFloatArray message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbFloatArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbFloatArray;

                /**
                 * Verifies a PbFloatArray message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbFloatArray message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbFloatArray
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbFloatArray;

                /**
                 * Creates a plain object from a PbFloatArray message. Also converts values to other types if specified.
                 * @param message PbFloatArray
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbFloatArray, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbFloatArray to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbFloatArray
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PbBoolArray. */
            interface IPbBoolArray {

                /** PbBoolArray Value */
                Value?: (boolean[]|null);
            }

            /** Represents a PbBoolArray. */
            class PbBoolArray implements IPbBoolArray {

                /**
                 * Constructs a new PbBoolArray.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPbBoolArray);

                /** PbBoolArray Value. */
                public Value: boolean[];

                /**
                 * Creates a new PbBoolArray instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PbBoolArray instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPbBoolArray): GoPlay.Core.Protocols.PbBoolArray;

                /**
                 * Encodes the specified PbBoolArray message. Does not implicitly {@link GoPlay.Core.Protocols.PbBoolArray.verify|verify} messages.
                 * @param message PbBoolArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPbBoolArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PbBoolArray message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PbBoolArray.verify|verify} messages.
                 * @param message PbBoolArray message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPbBoolArray, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PbBoolArray message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PbBoolArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PbBoolArray;

                /**
                 * Decodes a PbBoolArray message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PbBoolArray
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PbBoolArray;

                /**
                 * Verifies a PbBoolArray message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PbBoolArray message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PbBoolArray
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PbBoolArray;

                /**
                 * Creates a plain object from a PbBoolArray message. Also converts values to other types if specified.
                 * @param message PbBoolArray
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PbBoolArray, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PbBoolArray to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PbBoolArray
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** StatusCode enum. */
            enum StatusCode {
                Success = 0,
                Failed = 1,
                Error = 2,
                Timeout = 3
            }

            /** PackageType enum. */
            enum PackageType {
                HankShakeReq = 0,
                HankShakeResp = 1,
                Ping = 2,
                Pong = 3,
                Notify = 4,
                Request = 5,
                Response = 6,
                Push = 7,
                Kick = 8
            }

            /** EncodingType enum. */
            enum EncodingType {
                Protobuf = 0,
                Json = 1
            }

            /** ServerTag enum. */
            enum ServerTag {
                Empty = 0,
                FrontEnd = 1,
                BackEnd = 2,
                All = 3
            }

            /** Properties of a Status. */
            interface IStatus {

                /** Status Code */
                Code?: (GoPlay.Core.Protocols.StatusCode|null);

                /** Status Message */
                Message?: (string|null);
            }

            /** Represents a Status. */
            class Status implements IStatus {

                /**
                 * Constructs a new Status.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IStatus);

                /** Status Code. */
                public Code: GoPlay.Core.Protocols.StatusCode;

                /** Status Message. */
                public Message: string;

                /**
                 * Creates a new Status instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns Status instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IStatus): GoPlay.Core.Protocols.Status;

                /**
                 * Encodes the specified Status message. Does not implicitly {@link GoPlay.Core.Protocols.Status.verify|verify} messages.
                 * @param message Status message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IStatus, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified Status message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Status.verify|verify} messages.
                 * @param message Status message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IStatus, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a Status message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns Status
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.Status;

                /**
                 * Decodes a Status message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns Status
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.Status;

                /**
                 * Verifies a Status message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a Status message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns Status
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.Status;

                /**
                 * Creates a plain object from a Status message. Also converts values to other types if specified.
                 * @param message Status
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.Status, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this Status to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for Status
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a Session. */
            interface ISession {

                /** Session Guid */
                Guid?: (string|null);

                /** Session Values */
                Values?: ({ [k: string]: google.protobuf.IAny }|null);
            }

            /** Represents a Session. */
            class Session implements ISession {

                /**
                 * Constructs a new Session.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.ISession);

                /** Session Guid. */
                public Guid: string;

                /** Session Values. */
                public Values: { [k: string]: google.protobuf.IAny };

                /**
                 * Creates a new Session instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns Session instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.ISession): GoPlay.Core.Protocols.Session;

                /**
                 * Encodes the specified Session message. Does not implicitly {@link GoPlay.Core.Protocols.Session.verify|verify} messages.
                 * @param message Session message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.ISession, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified Session message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Session.verify|verify} messages.
                 * @param message Session message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.ISession, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a Session message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns Session
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.Session;

                /**
                 * Decodes a Session message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns Session
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.Session;

                /**
                 * Verifies a Session message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a Session message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns Session
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.Session;

                /**
                 * Creates a plain object from a Session message. Also converts values to other types if specified.
                 * @param message Session
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.Session, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this Session to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for Session
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a PackageInfo. */
            interface IPackageInfo {

                /** PackageInfo Type */
                Type?: (GoPlay.Core.Protocols.PackageType|null);

                /** PackageInfo Id */
                Id?: (number|null);

                /** PackageInfo EncodingType */
                EncodingType?: (GoPlay.Core.Protocols.EncodingType|null);

                /** PackageInfo Route */
                Route?: (number|null);

                /** PackageInfo ContentSize */
                ContentSize?: (number|null);
            }

            /** Represents a PackageInfo. */
            class PackageInfo implements IPackageInfo {

                /**
                 * Constructs a new PackageInfo.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IPackageInfo);

                /** PackageInfo Type. */
                public Type: GoPlay.Core.Protocols.PackageType;

                /** PackageInfo Id. */
                public Id: number;

                /** PackageInfo EncodingType. */
                public EncodingType: GoPlay.Core.Protocols.EncodingType;

                /** PackageInfo Route. */
                public Route: number;

                /** PackageInfo ContentSize. */
                public ContentSize: number;

                /**
                 * Creates a new PackageInfo instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns PackageInfo instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IPackageInfo): GoPlay.Core.Protocols.PackageInfo;

                /**
                 * Encodes the specified PackageInfo message. Does not implicitly {@link GoPlay.Core.Protocols.PackageInfo.verify|verify} messages.
                 * @param message PackageInfo message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IPackageInfo, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified PackageInfo message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.PackageInfo.verify|verify} messages.
                 * @param message PackageInfo message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IPackageInfo, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a PackageInfo message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns PackageInfo
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.PackageInfo;

                /**
                 * Decodes a PackageInfo message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns PackageInfo
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.PackageInfo;

                /**
                 * Verifies a PackageInfo message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a PackageInfo message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns PackageInfo
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.PackageInfo;

                /**
                 * Creates a plain object from a PackageInfo message. Also converts values to other types if specified.
                 * @param message PackageInfo
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.PackageInfo, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this PackageInfo to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for PackageInfo
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a Header. */
            interface IHeader {

                /** Header Status */
                Status?: (GoPlay.Core.Protocols.IStatus|null);

                /** Header Session */
                Session?: (GoPlay.Core.Protocols.ISession|null);

                /** Header PackageInfo */
                PackageInfo?: (GoPlay.Core.Protocols.IPackageInfo|null);
            }

            /** Represents a Header. */
            class Header implements IHeader {

                /**
                 * Constructs a new Header.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IHeader);

                /** Header Status. */
                public Status?: (GoPlay.Core.Protocols.IStatus|null);

                /** Header Session. */
                public Session?: (GoPlay.Core.Protocols.ISession|null);

                /** Header PackageInfo. */
                public PackageInfo?: (GoPlay.Core.Protocols.IPackageInfo|null);

                /**
                 * Creates a new Header instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns Header instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IHeader): GoPlay.Core.Protocols.Header;

                /**
                 * Encodes the specified Header message. Does not implicitly {@link GoPlay.Core.Protocols.Header.verify|verify} messages.
                 * @param message Header message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IHeader, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified Header message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.Header.verify|verify} messages.
                 * @param message Header message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IHeader, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a Header message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns Header
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.Header;

                /**
                 * Decodes a Header message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns Header
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.Header;

                /**
                 * Verifies a Header message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a Header message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns Header
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.Header;

                /**
                 * Creates a plain object from a Header message. Also converts values to other types if specified.
                 * @param message Header
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.Header, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this Header to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for Header
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a ReqHankShake. */
            interface IReqHankShake {

                /** ReqHankShake ClientVersion */
                ClientVersion?: (string|null);

                /** ReqHankShake ServerTag */
                ServerTag?: (GoPlay.Core.Protocols.ServerTag|null);

                /** ReqHankShake AppKey */
                AppKey?: (string|null);
            }

            /** Represents a ReqHankShake. */
            class ReqHankShake implements IReqHankShake {

                /**
                 * Constructs a new ReqHankShake.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IReqHankShake);

                /** ReqHankShake ClientVersion. */
                public ClientVersion: string;

                /** ReqHankShake ServerTag. */
                public ServerTag: GoPlay.Core.Protocols.ServerTag;

                /** ReqHankShake AppKey. */
                public AppKey: string;

                /**
                 * Creates a new ReqHankShake instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns ReqHankShake instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IReqHankShake): GoPlay.Core.Protocols.ReqHankShake;

                /**
                 * Encodes the specified ReqHankShake message. Does not implicitly {@link GoPlay.Core.Protocols.ReqHankShake.verify|verify} messages.
                 * @param message ReqHankShake message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IReqHankShake, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified ReqHankShake message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.ReqHankShake.verify|verify} messages.
                 * @param message ReqHankShake message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IReqHankShake, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a ReqHankShake message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns ReqHankShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.ReqHankShake;

                /**
                 * Decodes a ReqHankShake message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns ReqHankShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.ReqHankShake;

                /**
                 * Verifies a ReqHankShake message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a ReqHankShake message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns ReqHankShake
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.ReqHankShake;

                /**
                 * Creates a plain object from a ReqHankShake message. Also converts values to other types if specified.
                 * @param message ReqHankShake
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.ReqHankShake, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this ReqHankShake to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for ReqHankShake
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }

            /** Properties of a RespHandShake. */
            interface IRespHandShake {

                /** RespHandShake ServerVersion */
                ServerVersion?: (string|null);

                /** RespHandShake HeartBeatInterval */
                HeartBeatInterval?: (number|null);

                /** RespHandShake Routes */
                Routes?: ({ [k: string]: number }|null);
            }

            /** Represents a RespHandShake. */
            class RespHandShake implements IRespHandShake {

                /**
                 * Constructs a new RespHandShake.
                 * @param [properties] Properties to set
                 */
                constructor(properties?: GoPlay.Core.Protocols.IRespHandShake);

                /** RespHandShake ServerVersion. */
                public ServerVersion: string;

                /** RespHandShake HeartBeatInterval. */
                public HeartBeatInterval: number;

                /** RespHandShake Routes. */
                public Routes: { [k: string]: number };

                /**
                 * Creates a new RespHandShake instance using the specified properties.
                 * @param [properties] Properties to set
                 * @returns RespHandShake instance
                 */
                public static create(properties?: GoPlay.Core.Protocols.IRespHandShake): GoPlay.Core.Protocols.RespHandShake;

                /**
                 * Encodes the specified RespHandShake message. Does not implicitly {@link GoPlay.Core.Protocols.RespHandShake.verify|verify} messages.
                 * @param message RespHandShake message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encode(message: GoPlay.Core.Protocols.IRespHandShake, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Encodes the specified RespHandShake message, length delimited. Does not implicitly {@link GoPlay.Core.Protocols.RespHandShake.verify|verify} messages.
                 * @param message RespHandShake message or plain object to encode
                 * @param [writer] Writer to encode to
                 * @returns Writer
                 */
                public static encodeDelimited(message: GoPlay.Core.Protocols.IRespHandShake, writer?: $protobuf.Writer): $protobuf.Writer;

                /**
                 * Decodes a RespHandShake message from the specified reader or buffer.
                 * @param reader Reader or buffer to decode from
                 * @param [length] Message length if known beforehand
                 * @returns RespHandShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): GoPlay.Core.Protocols.RespHandShake;

                /**
                 * Decodes a RespHandShake message from the specified reader or buffer, length delimited.
                 * @param reader Reader or buffer to decode from
                 * @returns RespHandShake
                 * @throws {Error} If the payload is not a reader or valid buffer
                 * @throws {$protobuf.util.ProtocolError} If required fields are missing
                 */
                public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): GoPlay.Core.Protocols.RespHandShake;

                /**
                 * Verifies a RespHandShake message.
                 * @param message Plain object to verify
                 * @returns `null` if valid, otherwise the reason why it is not
                 */
                public static verify(message: { [k: string]: any }): (string|null);

                /**
                 * Creates a RespHandShake message from a plain object. Also converts values to their respective internal types.
                 * @param object Plain object
                 * @returns RespHandShake
                 */
                public static fromObject(object: { [k: string]: any }): GoPlay.Core.Protocols.RespHandShake;

                /**
                 * Creates a plain object from a RespHandShake message. Also converts values to other types if specified.
                 * @param message RespHandShake
                 * @param [options] Conversion options
                 * @returns Plain object
                 */
                public static toObject(message: GoPlay.Core.Protocols.RespHandShake, options?: $protobuf.IConversionOptions): { [k: string]: any };

                /**
                 * Converts this RespHandShake to JSON.
                 * @returns JSON object
                 */
                public toJSON(): { [k: string]: any };

                /**
                 * Gets the default type url for RespHandShake
                 * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
                 * @returns The default type url
                 */
                public static getTypeUrl(typeUrlPrefix?: string): string;
            }
        }
    }
}

/** Namespace google. */
export namespace google {

    /** Namespace protobuf. */
    namespace protobuf {

        /** Properties of a Timestamp. */
        interface ITimestamp {

            /** Timestamp seconds */
            seconds?: (number|Long|null);

            /** Timestamp nanos */
            nanos?: (number|null);
        }

        /** Represents a Timestamp. */
        class Timestamp implements ITimestamp {

            /**
             * Constructs a new Timestamp.
             * @param [properties] Properties to set
             */
            constructor(properties?: google.protobuf.ITimestamp);

            /** Timestamp seconds. */
            public seconds: (number|Long);

            /** Timestamp nanos. */
            public nanos: number;

            /**
             * Creates a new Timestamp instance using the specified properties.
             * @param [properties] Properties to set
             * @returns Timestamp instance
             */
            public static create(properties?: google.protobuf.ITimestamp): google.protobuf.Timestamp;

            /**
             * Encodes the specified Timestamp message. Does not implicitly {@link google.protobuf.Timestamp.verify|verify} messages.
             * @param message Timestamp message or plain object to encode
             * @param [writer] Writer to encode to
             * @returns Writer
             */
            public static encode(message: google.protobuf.ITimestamp, writer?: $protobuf.Writer): $protobuf.Writer;

            /**
             * Encodes the specified Timestamp message, length delimited. Does not implicitly {@link google.protobuf.Timestamp.verify|verify} messages.
             * @param message Timestamp message or plain object to encode
             * @param [writer] Writer to encode to
             * @returns Writer
             */
            public static encodeDelimited(message: google.protobuf.ITimestamp, writer?: $protobuf.Writer): $protobuf.Writer;

            /**
             * Decodes a Timestamp message from the specified reader or buffer.
             * @param reader Reader or buffer to decode from
             * @param [length] Message length if known beforehand
             * @returns Timestamp
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): google.protobuf.Timestamp;

            /**
             * Decodes a Timestamp message from the specified reader or buffer, length delimited.
             * @param reader Reader or buffer to decode from
             * @returns Timestamp
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): google.protobuf.Timestamp;

            /**
             * Verifies a Timestamp message.
             * @param message Plain object to verify
             * @returns `null` if valid, otherwise the reason why it is not
             */
            public static verify(message: { [k: string]: any }): (string|null);

            /**
             * Creates a Timestamp message from a plain object. Also converts values to their respective internal types.
             * @param object Plain object
             * @returns Timestamp
             */
            public static fromObject(object: { [k: string]: any }): google.protobuf.Timestamp;

            /**
             * Creates a plain object from a Timestamp message. Also converts values to other types if specified.
             * @param message Timestamp
             * @param [options] Conversion options
             * @returns Plain object
             */
            public static toObject(message: google.protobuf.Timestamp, options?: $protobuf.IConversionOptions): { [k: string]: any };

            /**
             * Converts this Timestamp to JSON.
             * @returns JSON object
             */
            public toJSON(): { [k: string]: any };

            /**
             * Gets the default type url for Timestamp
             * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
             * @returns The default type url
             */
            public static getTypeUrl(typeUrlPrefix?: string): string;
        }

        /** Properties of an Any. */
        interface IAny {

            /** Any type_url */
            type_url?: (string|null);

            /** Any value */
            value?: (Uint8Array|null);
        }

        /** Represents an Any. */
        class Any implements IAny {

            /**
             * Constructs a new Any.
             * @param [properties] Properties to set
             */
            constructor(properties?: google.protobuf.IAny);

            /** Any type_url. */
            public type_url: string;

            /** Any value. */
            public value: Uint8Array;

            /**
             * Creates a new Any instance using the specified properties.
             * @param [properties] Properties to set
             * @returns Any instance
             */
            public static create(properties?: google.protobuf.IAny): google.protobuf.Any;

            /**
             * Encodes the specified Any message. Does not implicitly {@link google.protobuf.Any.verify|verify} messages.
             * @param message Any message or plain object to encode
             * @param [writer] Writer to encode to
             * @returns Writer
             */
            public static encode(message: google.protobuf.IAny, writer?: $protobuf.Writer): $protobuf.Writer;

            /**
             * Encodes the specified Any message, length delimited. Does not implicitly {@link google.protobuf.Any.verify|verify} messages.
             * @param message Any message or plain object to encode
             * @param [writer] Writer to encode to
             * @returns Writer
             */
            public static encodeDelimited(message: google.protobuf.IAny, writer?: $protobuf.Writer): $protobuf.Writer;

            /**
             * Decodes an Any message from the specified reader or buffer.
             * @param reader Reader or buffer to decode from
             * @param [length] Message length if known beforehand
             * @returns Any
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            public static decode(reader: ($protobuf.Reader|Uint8Array), length?: number): google.protobuf.Any;

            /**
             * Decodes an Any message from the specified reader or buffer, length delimited.
             * @param reader Reader or buffer to decode from
             * @returns Any
             * @throws {Error} If the payload is not a reader or valid buffer
             * @throws {$protobuf.util.ProtocolError} If required fields are missing
             */
            public static decodeDelimited(reader: ($protobuf.Reader|Uint8Array)): google.protobuf.Any;

            /**
             * Verifies an Any message.
             * @param message Plain object to verify
             * @returns `null` if valid, otherwise the reason why it is not
             */
            public static verify(message: { [k: string]: any }): (string|null);

            /**
             * Creates an Any message from a plain object. Also converts values to their respective internal types.
             * @param object Plain object
             * @returns Any
             */
            public static fromObject(object: { [k: string]: any }): google.protobuf.Any;

            /**
             * Creates a plain object from an Any message. Also converts values to other types if specified.
             * @param message Any
             * @param [options] Conversion options
             * @returns Plain object
             */
            public static toObject(message: google.protobuf.Any, options?: $protobuf.IConversionOptions): { [k: string]: any };

            /**
             * Converts this Any to JSON.
             * @returns JSON object
             */
            public toJSON(): { [k: string]: any };

            /**
             * Gets the default type url for Any
             * @param [typeUrlPrefix] your custom typeUrlPrefix(default "type.googleapis.com")
             * @returns The default type url
             */
            public static getTypeUrl(typeUrlPrefix?: string): string;
        }
    }
}
