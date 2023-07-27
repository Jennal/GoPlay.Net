using GoPlay.Core.Protocols;

namespace GoPlay.Core.Interfaces
{
    public interface IEncoder
    {
        EncodingType Type { get; }
        void AssertTypeValid<T>();
        
        byte[] Encode<T>(T data);
        T Decode<T>(byte[] data);
    }
}