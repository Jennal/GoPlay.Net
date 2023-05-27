using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Interfaces
{
    public interface IEncoder
    {
        EncodingType Type { get; }
        void AssertTypeValid<T>();
        
        byte[] Encode<T>(T data);
        T Decode<T>(byte[] data);
    }
}