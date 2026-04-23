using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Exceptions;

namespace GoPlay.Core.Transports.TCP
{
    public class TcpClient : TransportClientBase
    {
        protected System.Net.Sockets.TcpClient m_client = null;
        protected byte[] m_readSizeBuffer = new byte[2];

        public override bool IsConnected => m_client?.Connected ?? false;

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            if (m_client != null) Disconnect();

            m_client = new System.Net.Sockets.TcpClient();
            m_client.ReceiveBufferSize = Consts.Buffer.ReadSize;
            m_client.SendBufferSize = Consts.Buffer.WriteSize;
            // m_client.NoDelay = true;
            
            m_client.Connect(host, port);
        }

        public override void Disconnect()
        {
            if (m_client == null) return;
            
            m_client.Close();
            m_client = null;
        }

        public override void Dispose()
        {
            Disconnect();
        }
        
        public override async ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            var ns = m_client.GetStream();
            var reads = new List<Socket>();

            while (!cancelSource.Token.IsCancellationRequested && reads.Count <= 0) {
                reads.Add(m_client.Client);
                try
                {
                    Socket.Select(reads, null, null, (int)Consts.TimeOut.Server.TotalMilliseconds * 1000);
                }
                catch
                {
                    throw new ServerDownException();  
                }
            }
            if (cancelSource.Token.IsCancellationRequested) return Array.Empty<byte>();
            
            //Socket.Select会直接报错
            // if (!ns.DataAvailable && m_client.Client.Poll(1000, SelectMode.SelectRead))
            // {
            //     //Socket.Select放行，却无有效数据，说明客户端已经断开连接
            //     throw new ServerDownException();
            // }

            //Read size
            var sizeLen = 0;
            while (!cancelSource.Token.IsCancellationRequested && sizeLen < m_readSizeBuffer.Length)
            {
                sizeLen += await ns.ReadAsync(m_readSizeBuffer, sizeLen, m_readSizeBuffer.Length - sizeLen, cancelSource.Token);
            }
            if (cancelSource.Token.IsCancellationRequested) return Array.Empty<byte>();
            
            var size = BinaryPrimitives.ReadUInt16LittleEndian(m_readSizeBuffer);

            // 直接分配目标大小的 byte[]，避免 MemoryStream+ToArray 的双重拷贝
            var payload = new byte[size];
            var read = 0;
            while (!cancelSource.Token.IsCancellationRequested && read < size)
            {
                var recvSize = await ns.ReadAsync(payload, read, size - read, cancelSource.Token);
                if (recvSize <= 0) break;
                read += recvSize;
            }
            if (cancelSource.Token.IsCancellationRequested) return Array.Empty<byte>();
            return payload;
        }

        public override async ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            var ns = m_client.GetStream();

            // 直接写长度前缀，省掉 MemoryStream/BinaryWriter 的临时分配
            var lenBuf = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(lenBuf, (ushort)data.Length);
            await ns.WriteAsync(lenBuf, 0, lenBuf.Length, cancelSource.Token);

            var start = 0;
            while (!cancelSource.Token.IsCancellationRequested && start < data.Length)
            {
                var size = Math.Min(data.Length - start, m_client.SendBufferSize);
                await ns.WriteAsync(data, start, size, cancelSource.Token);
                start += size;
            }
            
            ns.Flush();
        }

        /// <summary>
        /// 零拷贝发送：<paramref name="data"/> 已是完整 wire frame（含 outer ushort 前缀），
        /// 与 <see cref="NcClient"/> 契约一致。直接 <see cref="System.Net.Sockets.NetworkStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>，
        /// 省去 byte[] 版本里每包两次 <c>WriteAsync</c> + <c>lenBuf</c> 分配。
        /// </summary>
        public override async ValueTask Send(ReadOnlyMemory<byte> data, CancellationTokenSource cancelSource)
        {
            if (data.Length == 0) return;
            var ns = m_client.GetStream();

            var start = 0;
            var chunkSize = m_client.SendBufferSize;
            while (!cancelSource.Token.IsCancellationRequested && start < data.Length)
            {
                var size = Math.Min(data.Length - start, chunkSize);
                await ns.WriteAsync(data.Slice(start, size), cancelSource.Token);
                start += size;
            }

            ns.Flush();
        }
    }
}