using System;

namespace GoPlay.Core.Protocols
{
    public partial class Header
    {
        public uint ClientId;
        
        public static Header Parse(byte[] bytes)
        {
            var header = Parser.ParseFrom(bytes);
            if (header.Status == null)
            {
                header.Status = new Status();
            }

            return header;
        }

        /// <summary>
        /// Span 版解析。Google.Protobuf 3.15+ 的 <c>MessageParser.ParseFrom(ReadOnlySpan&lt;byte&gt;)</c>
        /// 直接消费 span，不需要调用方先 <c>.ToArray()</c>。
        /// Package.ParseRaw 热路径上省掉一次 header 字节的 byte[] 分配。
        /// </summary>
        public static Header Parse(ReadOnlySpan<byte> bytes)
        {
            var header = Parser.ParseFrom(bytes);
            if (header.Status == null)
            {
                header.Status = new Status();
            }

            return header;
        }
    }
}
