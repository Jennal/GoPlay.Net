using System;
using System.Net;
using System.Text;

namespace GoPlay.Core.Utils
{
    public static class ProxyProtocolUtil
    {
        public static (int, string) ParseProxyProtocol(byte[] buffer, long offset, long size)
        {
            if (IsProxyProtocolV2(buffer, offset, size))
            {
                return ParseProxyProtocolV2(buffer, offset, size);
            }

            if (IsProxyProtocolV1(buffer, offset, size))
            {
                return ParseProxyProtocolV1(buffer, offset, size);
            }

            return (0, string.Empty);
        }

        // Helper: Check for the 12-byte v2 signature
        private static bool IsProxyProtocolV2(byte[] buffer, long offset, long size)
        {
            if (size < 16) return false; // Minimum v2 header size

            byte[] signature = { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };
            for (int i = 0; i < signature.Length; i++)
            {
                if (buffer[offset + i] != signature[i]) return false;
            }

            return true;
        }

        // Helper: Check for the v1 "PROXY " prefix
        private static bool IsProxyProtocolV1(byte[] buffer, long offset, long size)
        {
            if (size < 6 || offset + 6 > buffer.Length) return false;

            // ASCII "PROXY "
            return buffer[offset + 0] == (byte)'P' &&
                   buffer[offset + 1] == (byte)'R' &&
                   buffer[offset + 2] == (byte)'O' &&
                   buffer[offset + 3] == (byte)'X' &&
                   buffer[offset + 4] == (byte)'Y' &&
                   buffer[offset + 5] == (byte)' ';
        }

        // Helper: Parse the header and return its total length
        private static (int, string) ParseProxyProtocolV2(byte[] buffer, long offset, long size)
        {
            try
            {
                // The signature is 12 bytes.
                // Byte 12: Version (high 4 bits) + Command (low 4 bits). Expecting 0x21 (v2, PROXY)
                byte verCmd = buffer[offset + 12];
                if ((verCmd & 0xF0) != 0x20) return (0, string.Empty); // Not version 2

                // Byte 13: Address Family (high 4 bits) + Transport (low 4 bits)
                byte famTrans = buffer[offset + 13];

                // Byte 14-15: Length of the remaining address data (Big Endian)
                ushort addrLength = (ushort)((buffer[offset + 14] << 8) | buffer[offset + 15]);

                int totalHeaderLength = 16 + addrLength;
                if (size < totalHeaderLength) return (0, string.Empty); // Incomplete packet, wait for more data (simplified)

                // Parse IP based on Family
                // 0x11 = IPv4 (AF_INET) + TCP (STREAM)
                // 0x21 = IPv6 (AF_INET6) + TCP (STREAM)

                var realIp = string.Empty;
                if (famTrans == 0x11) // IPv4
                {
                    // IPv4: src_addr(4) + dst_addr(4) + src_port(2) + dst_port(2)
                    byte[] ipBytes = new byte[4];
                    Array.Copy(buffer, offset + 16, ipBytes, 0, 4);
                    realIp = new IPAddress(ipBytes).ToString();
                }
                else if (famTrans == 0x21) // IPv6
                {
                    // IPv6: src_addr(16) + dst_addr(16) + src_port(2) + dst_port(2)
                    byte[] ipBytes = new byte[16];
                    Array.Copy(buffer, offset + 16, ipBytes, 0, 16);
                    realIp = new IPAddress(ipBytes).ToString();
                }

                return (totalHeaderLength, realIp);
            }
            catch
            {
                return (0, string.Empty); // Parsing failed
            }
        }

        // Parse Proxy Protocol v1 line (text based)
        private static (int, string) ParseProxyProtocolV1(byte[] buffer, long offset, long size)
        {
            try
            {
                if (!IsProxyProtocolV1(buffer, offset, size)) return (0, string.Empty);

                long remaining = buffer.Length - offset;
                if (remaining <= 0) return (0, string.Empty);

                int available = (int)Math.Min(size, remaining);
                if (available < 2) return (0, string.Empty);

                // v1 header line is max 108 bytes (spec)
                int scanLimit = Math.Min(available, 108);
                int lineEnd = -1;

                // Find CRLF terminator
                for (int i = 0; i <= scanLimit - 2; i++)
                {
                    if (buffer[offset + i] == (byte)'\r' && buffer[offset + i + 1] == (byte)'\n')
                    {
                        lineEnd = i;
                        break;
                    }
                }

                if (lineEnd < 0) return (0, string.Empty); // Incomplete header, need more data

                string headerLine = Encoding.ASCII.GetString(buffer, (int)offset, lineEnd);
                string[] parts = headerLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Expected: PROXY TCP4|TCP6 src_addr dst_addr src_port dst_port
                if (parts.Length >= 6)
                {
                    string family = parts[1];
                    if (family == "TCP4" || family == "TCP6")
                    {
                        return (lineEnd + 2, parts[2]);
                    }
                }
                else if (parts.Length >= 2 && parts[1] == "UNKNOWN")
                {
                    return (lineEnd + 2, string.Empty);
                }

                return (lineEnd + 2, string.Empty);
            }
            catch
            {
                return (0, string.Empty);
            }
        }
    }
}