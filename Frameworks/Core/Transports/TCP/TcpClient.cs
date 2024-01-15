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
        protected byte[] m_readBuffer = new byte[Consts.Buffer.ReadSize];
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
            // return br.ReadBytes(size);
            // Console.ForegroundColor = ConsoleColor.Yellow;
            // Console.WriteLine($"Recv: size={size}");
            // Console.ResetColor();

            //Read content
            using (var ms = new MemoryStream())
            {
                var length = 0;
                while (!cancelSource.Token.IsCancellationRequested && length < size)
                {
                    var nextSize = Math.Min(size - length, m_client.ReceiveBufferSize);
                    var recvSize = await ns.ReadAsync(m_readBuffer, 0, nextSize, cancelSource.Token);
                    ms.Write(m_readBuffer, 0, recvSize);
                    length += recvSize;
                }
                if (cancelSource.Token.IsCancellationRequested) return Array.Empty<byte>();

                // Console.ForegroundColor = ConsoleColor.Magenta;
                // var pack = Package.ParseRaw(ms.ToArray());
                // Console.WriteLine($"Recv: pack={pack}");
                // Console.ResetColor();
                return ms.ToArray();
            }
        }

        public override async ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            var ns = m_client.GetStream();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    //write length
                    bw.Write((ushort) data.Length);
                    var len = ms.ToArray();
                    await ns.WriteAsync(len, 0, len.Length, cancelSource.Token);
                }
            }

            //write bytes
            var start = 0;
            while (!cancelSource.Token.IsCancellationRequested && start < data.Length)
            {
                var size = Math.Min(data.Length - start, m_client.SendBufferSize);
                await ns.WriteAsync(data, start, size, cancelSource.Token);
                start += size;
            }
            
            ns.Flush();
        }
    }
}