using System;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Encodes.Factory
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