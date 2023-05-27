using System;
using GoPlay.Services.Core.Encodes;
using GoPlay.Services.Core.Interfaces;
using GoPlay.Services.Core.Protocols;

namespace Core.Encodes.Factory
{
    public class EncoderFactory
    {
        public static IEncoder Create(EncodingType type)
        {
            switch (type)
            {
                case EncodingType.Protobuf:
                    return ProtobufEncoder.Instance;
                case EncodingType.Json:
                    return JsonEncoder.Instance;
            }

            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}