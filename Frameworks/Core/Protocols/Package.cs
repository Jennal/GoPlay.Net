using System;
using System.IO;
using System.Threading.Tasks;
using Core.Encodes.Factory;
using GoPlay.Services.Core.Debug;

namespace GoPlay.Services.Core.Protocols
{
    public class Package
    {
        private static readonly IdLoopGenerator s_idGen = new IdLoopGenerator();
        
        public Header Header;
        public byte[] RawData;

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
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    var headerLength = br.ReadUInt16();
                    var headerBytes = br.ReadBytes(headerLength);
                    var header = Header.Parser.ParseFrom(headerBytes);

                    if (header.PackageInfo.ContentSize > Consts.Package.MAX_SIZE)
                        throw new Exception(
                            $"Package.Parse: Exceed max size({Consts.Package.MAX_SIZE}): {header.PackageInfo.ContentSize}");
                    var body = header.PackageInfo.ContentSize > 0
                        ? br.ReadBytes((int) header.PackageInfo.ContentSize)
                        : null;
                    return new Package
                    {
                        Header = header,
                        RawData = body
                    };
                }
            }
        }

        public static Package<TData> Parse<TData>(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    var headerLength = br.ReadUInt16();
                    var headerBytes = br.ReadBytes(headerLength);
                    var header = Header.Parser.ParseFrom(headerBytes);

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

        public override string ToString()
        {
            return $"Header: {Header}, Data: {Data}, RawData: {RawData.Dump()}";
        }
    }
}