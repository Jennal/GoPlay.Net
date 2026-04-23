using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;

namespace GoPlay.Core.Transport.NetCoreServer
{
    class PackSession : TcpSession
    {
        // ArrayPool-backed stash（同 WsPackSession），避免每次收包 ToArray。
        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;

        public uint ClientId { get; }
        public string ClientIP;
        public PackServer PackServer => Server as PackServer;

        public PackSession(TcpServer server, uint clientId) : base(server)
        {
            ClientId = clientId;
        }

        protected override void OnConnected()
        {
            var ep = Socket.RemoteEndPoint as IPEndPoint;
            if (ep == null)
            {
                ClientIP = "unknown";
            }
            else
            {
                ClientIP = ep.Address.ToString();
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            AppendToStash(buffer, (int)offset, (int)size);
            DrainStash();
        }

        protected override void OnDisconnected()
        {
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
        }

        private void AppendToStash(byte[] buffer, int offset, int size)
        {
            if (size <= 0) return;
            if (m_stash == null)
            {
                m_stash = ArrayPool<byte>.Shared.Rent(Math.Max(size, InitialStashCapacity));
                m_stashLen = 0;
            }
            if (m_stashLen + size > m_stash.Length)
            {
                var newCap = m_stash.Length;
                while (newCap < m_stashLen + size) newCap *= 2;
                var newBuf = ArrayPool<byte>.Shared.Rent(newCap);
                if (m_stashLen > 0) System.Buffer.BlockCopy(m_stash, 0, newBuf, 0, m_stashLen);
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = newBuf;
            }
            System.Buffer.BlockCopy(buffer, offset, m_stash, m_stashLen, size);
            m_stashLen += size;
        }

        private void DrainStash()
        {
            var pos = 0;
            while (m_stashLen - pos >= sizeof(ushort))
            {
                var len = BinaryPrimitives.ReadUInt16LittleEndian(
                    new ReadOnlySpan<byte>(m_stash, pos, sizeof(ushort)));
                if (m_stashLen - pos - sizeof(ushort) < len) break;

                // Step 3.14a: 直接以 stash 切片喂给 span 版 on-recv，省掉整帧 byte[] 分配。
                // 详见 WsServer.DrainStash 的同款注释 / TransportServerBase.InvokeOnDataReceivedSpan。
                var packSpan = new ReadOnlySpan<byte>(m_stash, pos + sizeof(ushort), len);
                pos += sizeof(ushort) + len;

                PackServer.OnRecvSpan(this, packSpan);
            }

            if (pos == 0) return;
            var remaining = m_stashLen - pos;
            if (remaining > 0)
            {
                System.Buffer.BlockCopy(m_stash, pos, m_stash, 0, remaining);
            }
            m_stashLen = remaining;
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session[{ClientId}] caught an error with code {error}");
        }
    }

    class PackServer : TcpServer
    {
        private NcServer m_ncServer;
        private IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        private ConcurrentDictionary<uint, PackSession> m_sessions = new ConcurrentDictionary<uint, PackSession>();
        // private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

        public PackServer(NcServer server, IPAddress address, int port) : base(address, port)
        {
            m_ncServer = server;
        }

        protected override TcpSession CreateSession() { return new PackSession(this, m_idGen.Next()); }

        /// <summary>
        /// 缺陷 4 修复：在 <see cref="TcpSession.Connect"/> 调用 <c>TryReceive()</c> 之前注册 session。
        ///
        /// <para>
        /// <b>原 bug 的场景</b>：100 并发 client 连上来，每个 client Connect 完立刻发 HandshakeReq。
        /// 服务端 <see cref="TcpSession.Connect"/> 里先 <c>TryReceive()</c>（line 139）再 <c>OnConnectedInternal()</c>（line 149）。
        /// <c>TryReceive()</c> 在当前 IOCP 线程 post 了 WSARecv；如果此时 client 的 HandshakeReq 已经
        /// 躺在内核 recv buffer 里（loopback 下这极容易发生），另一条 IOCP 线程可立刻完成该 WSARecv 并
        /// 同步回调 <see cref="PackSession.OnReceived"/>→<see cref="DrainStash"/>→
        /// <see cref="PackServer.OnRecvSpan"/>→Server&lt;T&gt; 握手回调→<see cref="NcServer.SendAsync"/>→
        /// <see cref="SendFramed"/>→<see cref="GetSession"/>。而原来 <c>m_sessions[clientId] = session</c>
        /// 是在 <c>OnConnected(session)</c>（即 <c>OnConnectedInternal</c>）里做的，时序上还没跑到，
        /// 导致 <c>GetSession</c> 返回 false → HandshakeResp 被 silently drop → client 卡 10s 超时。
        /// </para>
        ///
        /// <para>
        /// <b>修复思路</b>：把"注册到查找表"这个只读字典 insert 上移到 <see cref="OnConnecting"/>。
        /// 按 NetCoreServer 里 <c>TcpSession.Connect</c> 的实际顺序，<see cref="OnConnecting"/> 在
        /// <c>IsConnected=true</c>、<c>TryReceive()</c> 之前触发（line 130/133 vs 139），彻底消除竞态。
        /// 外部事件 <see cref="NcServer.OnConnected"/> 保留在 <see cref="OnConnected"/> 里：这是业务语义
        /// （连接真正建立完成），不是 session lookup，不能提前。
        /// </para>
        /// </summary>
        protected override void OnConnecting(TcpSession session)
        {
            if (session is PackSession packSession == false) return;

            m_sessions[packSession.ClientId] = packSession;
        }

        protected override void OnConnected(TcpSession session)
        {
            if (session is PackSession packSession == false) return;

            m_ncServer.OnConnected(packSession.ClientId);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            if (session is PackSession packSession == false) return;

            m_ncServer.OnDisconnected(packSession.ClientId);
            m_sessions.TryRemove(packSession.ClientId, out _);
        }

        internal void OnRecv(PackSession session, byte[] data)
        {
            m_ncServer.InvokeOnDataReceived(session.ClientId, data);
            // m_readChannel.Add((session.ClientId, data), m_ncServer.CancellationToken);
        }

        /// <summary>
        /// Step 3.14a: span 版 on-recv，直接 forward 到
        /// <see cref="TransportServerBase.InvokeOnDataReceivedSpan"/>，不做 byte[] 复制。
        /// </summary>
        internal void OnRecvSpan(PackSession session, ReadOnlySpan<byte> data)
        {
            m_ncServer.InvokeOnDataReceivedSpan(session.ClientId, data);
        }

        // public (uint, byte[]) Recv()
        // {
        //     return m_readChannel.Take(m_ncServer.CancellationToken);
        // }

        public void Send(uint clientId, byte[] data)
        {
            if (!GetSession(clientId, out var session)) return;

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)data.Length);
                    bw.Write(data);
                    
                    session.SendAsync(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 零拷贝批量发送：<paramref name="framedBytes"/> 已经是 wire bytes（含外层长度前缀），
        /// 直接 forward 到 session.SendAsync(span)。
        /// </summary>
        public void SendFramed(uint clientId, ReadOnlySpan<byte> framedBytes)
        {
            if (!GetSession(clientId, out var session)) return;
            session.SendAsync(framedBytes);
        }

        public bool GetSession(uint clientId, out PackSession session)
        {
            return m_sessions.TryGetValue(clientId, out session);
        }
    }
    
    public class NcServer : TransportServerBase
    {
        private PackServer m_server;
        private CancellationTokenSource m_cancelSource;

        internal CancellationToken CancellationToken => m_cancelSource.Token;
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            
            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            m_server = new PackServer(this, address, port);
            m_server.Start();
        }

        public override void Stop()
        {
            if (m_server == null) return;
            
            m_server.Dispose();
            m_server = null;
        }

        // public override (uint, byte[]) Recv()
        // {
        //     return m_server.Recv();
        // }

        public override void Send(uint clientId, byte[] data)
        {
            m_server.Send(clientId, data);
        }

        /// <inheritdoc />
        public override System.Threading.Tasks.ValueTask SendAsync(uint clientId, ReadOnlyMemory<byte> framedBytes, System.Threading.CancellationToken ct)
        {
            m_server.SendFramed(clientId, framedBytes.Span);
            return default;
        }

        public override string GetClientIp(uint clientId)
        {
            if (!m_server.GetSession(clientId, out var session)) return string.Empty;
            
            return session.ClientIP;
        }

        public override void DisconnectClient(uint clientId, Exception err)
        {
            if (!m_server.GetSession(clientId, out var session)) return;
            session.Disconnect();
        }

        internal void OnConnected(uint clientId)
        {
            InvokeOnClientConnected(clientId);
        }
        
        internal void OnDisconnected(uint clientId)
        {
            InvokeOnClientDisconnected(clientId);
        }

        internal void OnClientError(uint clientId, Exception err)
        {
            InvokeOnError(clientId, err);
        }
    }
}