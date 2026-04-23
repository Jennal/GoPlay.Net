using System;
using System.IO;
using Google.Protobuf;
using GoPlay.Core.Gof;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Encodes
{
    public class ProtobufEncoder : Singleton<ProtobufEncoder>, IEncoder
    {
        public EncodingType Type => EncodingType.Protobuf;

        // 每线程一个 MemoryBufferWriter 实例：给 pb.WriteTo(IBufferWriter) 用，
        // 写完后 Clear。线程内热路径上 0 次对象分配。
        [ThreadStatic]
        private static MemoryBufferWriter s_writer;

        public void AssertTypeValid<T>()
        {
            if (typeof(IMessage).IsAssignableFrom(typeof(T))) return;

            throw new Exception($"ProtobufEncoder: type not valid: {typeof(T).Name}");
        }

        public byte[] Encode<T>(T data)
        {
            AssertTypeValid<T>();
            
            var pb = data as IMessage;
            if(pb == null) throw new Exception("ProtobufEncoder: convert on wrong type value");

            var ms = new MemoryStream();
            pb.WriteTo(ms);
            return ms.ToArray();
        }

        public T Decode<T>(byte[] data)
        {
            AssertTypeValid<T>();
            
            var parserInfo = typeof(T).GetProperty("Parser", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (parserInfo == null) throw new Exception("ProtobufEncoder: convert on wrong type value");

            var parser = parserInfo.GetValue(null, null) as MessageParser;
            if (parser == null) throw new Exception("ProtobufEncoder: convert on wrong type value");

            if (data == null || data.Length <= 0) return default;
            return (T)parser.ParseFrom(data);
        }

        public int GetEncodedSize<T>(T data)
        {
            AssertTypeValid<T>();
            var pb = data as IMessage;
            if (pb == null) throw new Exception("ProtobufEncoder: convert on wrong type value");
            return pb.CalculateSize();
        }

        public int EncodeTo<T>(T data, Memory<byte> dest)
        {
            AssertTypeValid<T>();
            var pb = data as IMessage;
            if (pb == null) throw new Exception("ProtobufEncoder: convert on wrong type value");

            var size = pb.CalculateSize();
            if (dest.Length < size)
                throw new ArgumentException(
                    $"ProtobufEncoder.EncodeTo: dest too small ({dest.Length} < {size})",
                    nameof(dest));

            var writer = s_writer ??= new MemoryBufferWriter();
            writer.Reset(dest.Slice(0, size));
            try
            {
                // Google.Protobuf 3.15+ 的 public 扩展方法：
                // MessageExtensions.WriteTo(this IMessage, IBufferWriter<byte>)
                // 走内部 WriteContext，直接把 wire bytes 刷进 buffer，无额外分配。
                pb.WriteTo(writer);
                return writer.WrittenCount;
            }
            finally
            {
                writer.Clear();
            }
        }
    }
}
