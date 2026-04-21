using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoPlay.Core.Encodes.Factory;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Debug;

namespace GoPlay.Core.Protocols
{
    public class Package
    {
        private static readonly IdLoopGenerator s_idGen = new IdLoopGenerator();
        
        public Header Header;
        public byte[] RawData;

        public bool IsChunk => Header.PackageInfo.ChunkCount > 1;
        public bool IsLastChunk => !IsChunk || Header.PackageInfo.ChunkIndex >= Header.PackageInfo.ChunkCount - 1;
        
        public virtual void UpdateContentSize()
        {
            Header.PackageInfo.ContentSize = (uint) (RawData?.Length ?? 0);
        }

        /// <summary>
        /// 计算 body 编码后字节数，用于在 <see cref="Split"/> / <see cref="WriteTo"/> 路径上**不落盘**地预测尺寸。
        /// 基类（<see cref="Package"/>）只有 RawData，直接返回长度；
        /// <see cref="Package{T}"/> 覆盖为 <c>RawData ?? encoder.GetEncodedSize(Data)</c>，
        /// 让 Protobuf 走 <c>IMessage.CalculateSize()</c> 0 alloc 拿到尺寸。
        /// </summary>
        protected virtual int GetBodyEncodedSize() => RawData?.Length ?? 0;
        
        public virtual byte[] GetBytes()
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    Header.PackageInfo.ContentSize = (uint) (RawData?.Length ?? 0);
                    var headerBytes = encoder.Encode(Header);

                    bw.Write((ushort) headerBytes.Length);
                    bw.Write(headerBytes);
                    if (RawData != null && RawData.Length > 0) bw.Write(RawData);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 零拷贝写入完整 wire frame：
        /// [ushort outerLen][ushort headerLen][headerBytes][bodyBytes]
        /// 其中 outerLen 等于随后 (headerLen + headerBytes + bodyBytes) 的字节数。
        ///
        /// 与旧路径 GetBytes() + Transport.Send(byte[]) 自己加 ushort 前缀等价，但：
        /// - 一次写入目标 IBufferWriter，支持 SessionSender 把 N 个包拼入同一 buffer；
        /// - 避免 MemoryStream/ToArray 的两段额外分配；
        /// - header 通过 <see cref="IEncoder.EncodeTo"/> 原地写入 span，省掉中间 <c>byte[] headerBytes</c> 分配；
        /// - 为 Package&lt;T&gt; 提供子类覆盖点以避免 body 双重编码。
        ///
        /// 返回值：已写入字节数（包含 outerLen 自己）。
        /// </summary>
        public virtual int WriteTo(IBufferWriter<byte> writer)
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            var bodyLen = RawData?.Length ?? 0;
            Header.PackageInfo.ContentSize = (uint)bodyLen;

            var headerLen = encoder.GetEncodedSize(Header);
            return WriteFrame(writer, encoder, Header, headerLen, RawData, bodyLen);
        }

        /// <summary>
        /// 给子类（Package&lt;T&gt;）复用的 wire 写入原语；其它路径不要直接调用。
        /// header / body 都原地写入 writer 申请的同一块 span，对 Protobuf 是 0 次 byte[] 分配。
        /// </summary>
        protected static int WriteFrame(IBufferWriter<byte> writer, IEncoder encoder, Header header,
            int headerLen, byte[] bodyBytes, int bodyLen)
        {
            if (headerLen > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: header exceeds ushort: {headerLen}");

            var innerLen = sizeof(ushort) + headerLen + bodyLen;
            if (innerLen > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: frame size exceeds ushort outer prefix: {innerLen}");

            var totalLen = sizeof(ushort) + innerLen;
            // 用 GetMemory 而不是 GetSpan：Memory 能桥接到 Protobuf 的 IBufferWriter<byte> 路径，
            // 让 Header 编码直接落在这块 buffer 里，不再分配临时 byte[]。
            var memory = writer.GetMemory(totalLen);
            var span = memory.Span;
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)innerLen);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(sizeof(ushort)), (ushort)headerLen);

            var written = encoder.EncodeTo(header, memory.Slice(sizeof(ushort) * 2, headerLen));
            if (written != headerLen)
                throw new Exception(
                    $"Package.WriteTo: header encoder wrote {written} but pre-measured {headerLen}");

            if (bodyLen > 0)
            {
                bodyBytes.AsSpan(0, bodyLen).CopyTo(span.Slice(sizeof(ushort) * 2 + headerLen));
            }
            writer.Advance(totalLen);
            return totalLen;
        }

        /// <summary>
        /// 给 <see cref="Package{T}"/> 在 RawData 还没被填的 zero-alloc 路径下复用：
        /// header 与 body 都通过 <see cref="IEncoder.EncodeTo"/> 直接写到调用方 IBufferWriter
        /// 申请的同一块连续 span，全程 0 byte[] 中间分配（Protobuf 编码器下）。
        /// </summary>
        protected static int WriteFrameWithDataBody<TData>(IBufferWriter<byte> writer, IEncoder encoder,
            Header header, int headerLen, TData data, int bodyLen)
        {
            if (headerLen > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: header exceeds ushort: {headerLen}");

            var innerLen = sizeof(ushort) + headerLen + bodyLen;
            if (innerLen > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: frame size exceeds ushort outer prefix: {innerLen}");

            var totalLen = sizeof(ushort) + innerLen;
            var memory = writer.GetMemory(totalLen);
            var span = memory.Span;
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)innerLen);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(sizeof(ushort)), (ushort)headerLen);

            var hWritten = encoder.EncodeTo(header, memory.Slice(sizeof(ushort) * 2, headerLen));
            if (hWritten != headerLen)
                throw new Exception(
                    $"Package.WriteTo: header encoder wrote {hWritten} but pre-measured {headerLen}");

            if (bodyLen > 0)
            {
                var bWritten = encoder.EncodeTo(data, memory.Slice(sizeof(ushort) * 2 + headerLen, bodyLen));
                if (bWritten != bodyLen)
                    throw new Exception(
                        $"Package.WriteTo: body encoder wrote {bWritten} but pre-measured {bodyLen}");
            }

            writer.Advance(totalLen);
            return totalLen;
        }

        public static Package Create(uint route, PackageType type, EncodingType encoding)
        {
            var pack = new Package
            {
                Header = new Header
                {
                    Status = new Status
                    {
                        Code = StatusCode.Success,
                    },
                    PackageInfo = new PackageInfo
                    {
                        EncodingType = encoding,
                        Id = s_idGen.Next(),
                        Route = route,
                        Type = type,
                    }
                },
            };

            return pack;
        }

        public static Package<T> Create<T>(uint route, T data, PackageType type, EncodingType encoding)
        {
            if (data is Task) throw new Exception("No Task as data accepted!");
            
            var pack = new Package<T>
            {
                Header = new Header
                {
                    Status = new Status
                    {
                        Code = StatusCode.Success,
                    },
                    PackageInfo = new PackageInfo
                    {
                        EncodingType = encoding,
                        Id = s_idGen.Next(),
                        Route = route,
                        Type = type,
                    }
                },
                Data = data
            };

            return pack;
        }
        
        public static Package ParseRaw(byte[] data)
        {
            return ParseRaw(new ReadOnlySpan<byte>(data));
        }

        /// <summary>
        /// span 版解析：输入为单个 pack 的 inner 字节（无 outer ushort 前缀）。
        /// Header 直接从 span 切片喂给 <see cref="Header.Parse(ReadOnlySpan{byte})"/>
        /// （Protobuf 3.15+ 的 ParseFrom(ReadOnlySpan) 零拷贝消费 span），省掉一次 headerBytes.ToArray() 分配。
        /// body 仍然会拷贝出一份独立 byte[]（业务后续会 async 持有 Package，不能留 span 引用；
        /// 进一步改造为 ArrayPool 租借见 §八 路线图）。
        /// </summary>
        public static Package ParseRaw(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(ushort))
                throw new Exception("Package.ParseRaw: data too short for header length");

            var headerLength = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var pos = sizeof(ushort);
            if (data.Length < pos + headerLength)
                throw new Exception("Package.ParseRaw: data too short for header bytes");

            var header = Header.Parse(data.Slice(pos, headerLength));
            pos += headerLength;

            if (header.PackageInfo.ContentSize > Consts.Package.MAX_SIZE)
                throw new Exception(
                    $"Package.Parse: Exceed max size({Consts.Package.MAX_SIZE}): {header.PackageInfo.ContentSize}");

            byte[] body = null;
            if (header.PackageInfo.ContentSize > 0)
            {
                var bodyLen = (int)header.PackageInfo.ContentSize;
                if (data.Length < pos + bodyLen)
                    throw new Exception("Package.ParseRaw: data too short for body");
                body = data.Slice(pos, bodyLen).ToArray();
            }

            return new Package
            {
                Header = header,
                RawData = body
            };
        }

        public static Package<TData> Parse<TData>(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    var headerLength = br.ReadUInt16();
                    var headerBytes = br.ReadBytes(headerLength);
                    var header = Header.Parse(headerBytes);

                    var encoder = EncoderFactory.Create(header.PackageInfo.EncodingType);
                    TData body = default(TData);
                    if (header.PackageInfo.ContentSize > 0)
                    {
                        var bodyBytes = br.ReadBytes((int) header.PackageInfo.ContentSize);
                        body = encoder.Decode<TData>(bodyBytes);
                    }

                    return new Package<TData>
                    {
                        Header = header,
                        Data = body
                    };
                }
            }
        }

        public static Package<TData> ParseFromRaw<TData>(Package pack)
        {
            var result = new Package<TData>
            {
                Header = pack.Header,
                RawData = pack.RawData
            };
            var encoder = EncoderFactory.Create(result.Header.PackageInfo.EncodingType);
            result.Data = encoder.Decode<TData>(result.RawData);
            return result;
        }

        public static async Task<object> GetDataFromTask(Task task)
        {
            await task;
            if (task.IsFaulted) throw new Exception("", task.Exception);
            
            var type = task.GetType();
            var propInfo = type.GetProperty("Result");
            if (propInfo == null) return null;
            return propInfo.GetValue(task);
        }
        
        public override string ToString()
        {
            return $"Header: {Header}, RawData: {RawData.Dump()}";
        }

        public virtual Package Clone()
        {
            byte[] rawData = null;
            if (RawData != null)
            {
                using var ms = new MemoryStream(RawData);
                rawData = ms.ToArray();
            }

            var h = Header.Clone();
            h.ClientId = Header.ClientId;
            return new Package
            {
                Header = h,
                RawData = rawData,
            };
        }

        public virtual IEnumerable<Package> Split()
        {
            // 先用 0-alloc 路径算 body 尺寸：Protobuf 走 IMessage.CalculateSize()，
            // 不会触发 byte[] 分配。Header.ContentSize 必须先于 WriteTo 设置好（Header 编码会带入这一字段）。
            var bodySize = GetBodyEncodedSize();
            Header.PackageInfo.ContentSize = (uint)bodySize;

            if (bodySize <= Consts.Package.MAX_CHUNK_SIZE)
            {
                // 小消息热路径：保持 RawData = null（对 Package<T>），让 WriteTo 走 zero-alloc body 分支。
                yield return this;
                yield break;
            }

            // 分块路径：必须把 body 编码落盘到 RawData，再按块切。这是大消息的一次性代价。
            UpdateContentSize();

            var header = Header.Clone();
            header.PackageInfo.ChunkCount = (uint) Math.Ceiling(Header.PackageInfo.ContentSize / (float) Consts.Package.MAX_CHUNK_SIZE);
            var chunkSize = Consts.Package.MAX_CHUNK_SIZE;
            for (uint start=0, i=0; start < RawData.Length; start += chunkSize, i++)
            {
                var chunk = new byte[Math.Min(chunkSize, RawData.Length - start)];
                Array.Copy(RawData, start, chunk, 0, chunk.Length);
                header.PackageInfo.ChunkIndex = i;
                header.PackageInfo.ContentSize = (uint) chunk.Length;
                var h = header.Clone();
                h.ClientId = Header.ClientId;
                yield return new Package
                {
                    Header = h,
                    RawData = chunk
                };
            }
        }

        public static Package Join(IEnumerable<Package> packs)
        {
            if (!packs.Any()) throw new Exception("Package.Join: packs is empty!");
            
            var p = packs.FirstOrDefault().Clone();
            
            using var ms = new MemoryStream();
            foreach (var chunk in packs.OrderBy(o => o.Header.PackageInfo.ChunkIndex))
            {
                ms.Write(chunk.RawData);
            }
            p.RawData = ms.ToArray();
            p.Header.PackageInfo.ChunkCount = 1;
            p.Header.PackageInfo.ChunkIndex = 0;
            p.Header.PackageInfo.ContentSize = (uint)p.RawData.Length;

            return p;
        }
    }

    public class Package<T> : Package
    {
        public T Data;

        public override void UpdateContentSize()
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            RawData= encoder.Encode(Data);
            Header.PackageInfo.ContentSize = (uint) (RawData?.Length ?? 0);
        }

        protected override int GetBodyEncodedSize()
        {
            if (RawData != null) return RawData.Length;
            if (Data == null) return 0;
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            return encoder.GetEncodedSize(Data);
        }

        public override byte[] GetBytes()
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    var bodyBytes = encoder.Encode(Data);
                    Header.PackageInfo.ContentSize = (uint) (bodyBytes?.Length ?? 0);
                    if (Header.PackageInfo.ContentSize > Consts.Package.MAX_SIZE)
                        throw new Exception(
                            $"Package<{typeof(T).Name}>.GetBytes: Exceed max size({Consts.Package.MAX_SIZE}): {Header.PackageInfo.ContentSize}");

                    var headerBytes = encoder.Encode(Header);
                    if (headerBytes.Length > ushort.MaxValue)
                        throw new Exception(
                            $"Package<{typeof(T).Name}>.GetBytes: Exceed max size({Consts.Package.MAX_SIZE}): {headerBytes.Length}");

                    bw.Write((ushort) headerBytes.Length);
                    bw.Write(headerBytes);
                    if (bodyBytes != null && bodyBytes.Length > 0) bw.Write(bodyBytes);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// <see cref="Package{T}"/> 的 wire 写入。两条路径：
        /// 1. <see cref="Package.RawData"/> 已被填好（分块场景，或上层手动设了 RawData）：
        ///    走 byte[] body 路径，把 RawData 拷进 wire span。
        /// 2. <see cref="Package.RawData"/> 仍为 null（小消息热路径，从 Step 3.10 起 <see cref="Split"/> 不再预编码）：
        ///    通过 <see cref="IEncoder.GetEncodedSize"/> + <see cref="IEncoder.EncodeTo"/> 把 Data 直接
        ///    编码到 IBufferWriter 申请的同一块 span，全程 0 byte[] 中间分配（Protobuf 路径）。
        /// 注意：zero-alloc 路径**不会**把编码结果回写到 RawData，避免引入额外 byte[] 分配；
        /// 上层若需要 byte[] 形式的 body 应显式调用 <see cref="UpdateContentSize"/>。
        /// </summary>
        public override int WriteTo(IBufferWriter<byte> writer)
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);

            if (RawData != null)
            {
                var bodyLen = RawData.Length;
                if (bodyLen > Consts.Package.MAX_SIZE)
                    throw new Exception(
                        $"Package<{typeof(T).Name}>.WriteTo: Exceed max size({Consts.Package.MAX_SIZE}): {bodyLen}");

                Header.PackageInfo.ContentSize = (uint)bodyLen;
                var hLen = encoder.GetEncodedSize(Header);
                return WriteFrame(writer, encoder, Header, hLen, RawData, bodyLen);
            }

            var dataBodyLen = Data == null ? 0 : encoder.GetEncodedSize(Data);
            if (dataBodyLen > Consts.Package.MAX_SIZE)
                throw new Exception(
                    $"Package<{typeof(T).Name}>.WriteTo: Exceed max size({Consts.Package.MAX_SIZE}): {dataBodyLen}");

            Header.PackageInfo.ContentSize = (uint)dataBodyLen;
            var headerLen = encoder.GetEncodedSize(Header);
            return WriteFrameWithDataBody(writer, encoder, Header, headerLen, Data, dataBodyLen);
        }

        public override string ToString()
        {
            return $"Header: {Header}, Data: {Data}, RawData: {RawData.Dump()}";
        }
    }
}