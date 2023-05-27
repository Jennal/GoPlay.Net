using System.Text;
using Newtonsoft.Json;
using GoPlay.Services.Core.Gof;
using GoPlay.Services.Core.Interfaces;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Encodes
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

            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Decode<T>(byte[] data)
        {   
            AssertTypeValid<T>();

            if (data == null || data.Length <= 0) return default;
            
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}