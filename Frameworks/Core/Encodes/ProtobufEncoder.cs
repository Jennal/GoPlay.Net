using System;
using System.IO;
using Google.Protobuf;
using GoPlay.Services.Core.Gof;
using GoPlay.Services.Core.Interfaces;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Encodes
{
    public class ProtobufEncoder : Singleton<ProtobufEncoder>, IEncoder
    {
        public EncodingType Type => EncodingType.Protobuf;

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
    }
}