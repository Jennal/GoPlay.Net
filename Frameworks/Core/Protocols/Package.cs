using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoPlay.Core.Encodes.Factory;
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
        /// - 为 Package&lt;T&gt; 提供子类覆盖点以避免 body 双重编码。
        ///
        /// 返回值：已写入字节数（包含 outerLen 自己）。
        /// </summary>
        public virtual int WriteTo(IBufferWriter<byte> writer)
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            Header.PackageInfo.ContentSize = (uint)(RawData?.Length ?? 0);
            var headerBytes = encoder.Encode(Header);
            if (headerBytes.Length > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: header exceeds ushort: {headerBytes.Length}");

            var bodyLen = RawData?.Length ?? 0;
            return WriteFrame(writer, headerBytes, RawData, bodyLen);
        }

        /// <summary>
        /// 给子类（Package&lt;T&gt;）复用的 wire 写入原语；其它路径不要直接调用。
        /// </summary>
        protected static int WriteFrame(IBufferWriter<byte> writer, byte[] headerBytes, byte[] bodyBytes, int bodyLen)
        {
            var innerLen = sizeof(ushort) + headerBytes.Length + bodyLen;
            if (innerLen > ushort.MaxValue)
                throw new Exception($"Package.WriteTo: frame size exceeds ushort outer prefix: {innerLen}");

            var totalLen = sizeof(ushort) + innerLen;
            var span = writer.GetSpan(totalLen);
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)innerLen);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(sizeof(ushort)), (ushort)headerBytes.Length);
            headerBytes.AsSpan().CopyTo(span.Slice(sizeof(ushort) * 2));
            if (bodyLen > 0)
            {
                bodyBytes.AsSpan(0, bodyLen).CopyTo(span.Slice(sizeof(ushort) * 2 + headerBytes.Length));
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
        /// body 仍然会拷贝出一份独立 byte[]（业务后续会 async 持有 Package，不能留 span 引用）。
        /// 目的是在 WsPackSession 等收包路径上消除 MemoryStream/BinaryReader 的频繁分配。
        /// </summary>
        public static Package ParseRaw(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(ushort))
                throw new Exception("Package.ParseRaw: data too short for header length");

            var headerLength = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var pos = sizeof(ushort);
            if (data.Length < pos + headerLength)
                throw new Exception("Package.ParseRaw: data too short for header bytes");

            var headerBytes = data.Slice(pos, headerLength).ToArray();
            pos += headerLength;
            var header = Header.Parse(headerBytes);

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
            UpdateContentSize();
            if (Header.PackageInfo.ContentSize <= Consts.Package.MAX_CHUNK_SIZE)
            {
                yield return this;
                yield break;
            }

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
        /// <see cref="Package{T}"/> 的 wire 写入：
        /// 正常通过 <see cref="Server.Send"/> -&gt; Split 路径进入时，RawData 已由 <see cref="UpdateContentSize"/> 编码填好，
        /// 这里直接复用即可（避免对同一个 Data 编码两次）；
        /// 若上层绕开了 Split（直接构造 Package&lt;T&gt; 后 WriteTo），则按需惰性编码一次。
        /// </summary>
        public override int WriteTo(IBufferWriter<byte> writer)
        {
            var encoder = EncoderFactory.Create(Header.PackageInfo.EncodingType);
            byte[] bodyBytes;
            if (RawData != null)
            {
                bodyBytes = RawData;
            }
            else
            {
                bodyBytes = encoder.Encode(Data);
                RawData = bodyBytes;
            }

            var bodyLen = bodyBytes?.Length ?? 0;
            if (bodyLen > Consts.Package.MAX_SIZE)
                throw new Exception(
                    $"Package<{typeof(T).Name}>.WriteTo: Exceed max size({Consts.Package.MAX_SIZE}): {bodyLen}");

            Header.PackageInfo.ContentSize = (uint)bodyLen;
            var headerBytes = encoder.Encode(Header);
            if (headerBytes.Length > ushort.MaxValue)
                throw new Exception(
                    $"Package<{typeof(T).Name}>.WriteTo: header exceeds ushort: {headerBytes.Length}");

            return WriteFrame(writer, headerBytes, bodyBytes, bodyLen);
        }

        public override string ToString()
        {
            return $"Header: {Header}, Data: {Data}, RawData: {RawData.Dump()}";
        }
    }
}