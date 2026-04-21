using System;
using System.Text;
using GoPlay.Core.Gof;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using LitJson;

namespace GoPlay.Core.Encodes
{
    public class JsonEncoder : Singleton<JsonEncoder>, IEncoder
    {
        public EncodingType Type => EncodingType.Json;

        public void AssertTypeValid<T>()
        {
            /* DO NOTHING */
        }

        public byte[] Encode<T>(T data)
        {
            AssertTypeValid<T>();

            var json = JsonMapper.ToJson(data);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Decode<T>(byte[] data)
        {   
            AssertTypeValid<T>();

            if (data == null || data.Length <= 0) return default;
            
            var json = Encoding.UTF8.GetString(data);
            return JsonMapper.ToObject<T>(json);
        }

        public int GetEncodedSize<T>(T data)
        {
            // LitJson 没有 "size-only" API，Json 串本来就是先构造再 UTF8 编码。
            // fallback：编码一次拿长度。代价被限制在低频的 handshake / 非热路径。
            var json = JsonMapper.ToJson(data);
            return Encoding.UTF8.GetByteCount(json);
        }

        public int EncodeTo<T>(T data, Memory<byte> dest)
        {
            var json = JsonMapper.ToJson(data);
            var size = Encoding.UTF8.GetByteCount(json);
            if (dest.Length < size)
                throw new ArgumentException(
                    $"JsonEncoder.EncodeTo: dest too small ({dest.Length} < {size})",
                    nameof(dest));

            return Encoding.UTF8.GetBytes(json, dest.Span);
        }
    }
}
