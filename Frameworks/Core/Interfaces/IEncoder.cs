using System;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Interfaces
{
    public interface IEncoder
    {
        EncodingType Type { get; }
        void AssertTypeValid<T>();
        
        byte[] Encode<T>(T data);
        T Decode<T>(byte[] data);

        /// <summary>
        /// 返回 <paramref name="data"/> 编码后的字节数，允许热路径上先预留空间再原地写入。
        /// Protobuf 下走 IMessage.CalculateSize()，不产生任何分配；
        /// Json 等无法零分配预测长度的 encoder，fallback 到 <c>Encode(data).Length</c>。
        /// </summary>
        int GetEncodedSize<T>(T data);

        /// <summary>
        /// 将 <paramref name="data"/> 直接编码到目标 <paramref name="dest"/> 里，返回实际写入字节数。
        /// 调用方需要保证 <c>dest.Length &gt;= GetEncodedSize(data)</c>。
        /// 用 <see cref="Memory{Byte}"/> 是为了能桥接 <c>IBufferWriter&lt;byte&gt;</c>（Protobuf 的
        /// <c>MessageExtensions.WriteTo(IBufferWriter)</c> 路径）；Json 等 encoder 可以直接
        /// <c>dest.Span</c> 使用。目的是让 Package.WriteTo 在 IBufferWriter 的同一块 span 上
        /// 原地写 header / body，省掉一次中间 byte[] 分配。
        /// </summary>
        int EncodeTo<T>(T data, Memory<byte> dest);
    }
}
