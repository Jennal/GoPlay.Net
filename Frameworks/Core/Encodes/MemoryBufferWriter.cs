using System;
using System.Buffers;

namespace GoPlay.Core.Encodes
{
    /// <summary>
    /// 包装一段已分配好的 <see cref="Memory{Byte}"/>，实现 <see cref="IBufferWriter{Byte}"/>，
    /// 让 Google.Protobuf 的 <c>MessageExtensions.WriteTo(IBufferWriter&lt;byte&gt;)</c> 可以直接
    /// 把消息写到调用方已准备好的 span 里，避免中间分配一个 <c>byte[]</c>。
    ///
    /// 设计约束：
    /// - 一次性使用：通过 <see cref="Reset"/> 装载目标 Memory 后，调用 Protobuf 的 WriteTo，
    ///   然后 <see cref="WrittenCount"/> 读出实际写入字节数；
    /// - 线程安全靠 <c>[ThreadStatic]</c>，见 <see cref="ProtobufEncoder"/> 的使用方式；
    /// - 如果写入超过了装载的 Memory 长度，<see cref="GetSpan"/> / <see cref="GetMemory"/>
    ///   会返回剩余空间不足的 span，Protobuf 内部会抛 <c>IndexOutOfRangeException</c>，
    ///   调用方负责提前用 <c>CalculateSize()</c> 预估长度。
    /// </summary>
    internal sealed class MemoryBufferWriter : IBufferWriter<byte>
    {
        private Memory<byte> m_memory;
        private int m_position;

        public int WrittenCount => m_position;

        public void Reset(Memory<byte> memory)
        {
            m_memory = memory;
            m_position = 0;
        }

        public void Clear()
        {
            m_memory = default;
            m_position = 0;
        }

        public void Advance(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var next = m_position + count;
            if (next > m_memory.Length)
                throw new InvalidOperationException(
                    $"MemoryBufferWriter.Advance: position {next} exceeds capacity {m_memory.Length}");
            m_position = next;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var remaining = m_memory.Slice(m_position);
            if (sizeHint > 0 && remaining.Length < sizeHint)
                throw new InvalidOperationException(
                    $"MemoryBufferWriter.GetMemory: requested {sizeHint}, remaining {remaining.Length}");
            return remaining;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var remaining = m_memory.Span.Slice(m_position);
            if (sizeHint > 0 && remaining.Length < sizeHint)
                throw new InvalidOperationException(
                    $"MemoryBufferWriter.GetSpan: requested {sizeHint}, remaining {remaining.Length}");
            return remaining;
        }
    }
}
