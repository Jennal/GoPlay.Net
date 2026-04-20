using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GoPlay.Core.Interfaces;
using NetCoreServer;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;

namespace GoPlay.Core.Transport.Ws
{
    class WsPackSession : WsSession
    {
        // 使用 ArrayPool 做 ring-style stash buffer：
        // - m_stash 随接收流增长而扩容（rent 更大 buffer，拷贝旧数据并归还原 buffer）
        // - m_stashLen 表示当前已填入字节
        // - parse 完整包后通过 Buffer.BlockCopy 把未消费部分前移，不另分配；
        //   这相比旧实现每次 ToArray 整段 buffer 可节省大量 GC。
        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;

        public uint ClientId { get; }
        public string ClientIP;
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public WsPackServer PackServer => Server as WsPackServer;

        public WsPackSession(WsPackServer server, uint clientId) : base(server)
        {
            ClientId = clientId;
        }

        public void SetHeaders(HttpRequest request)
        {
            Headers.Clear();
            for (var i = 0; i < request.Headers; i++)
            {
                var (key, value) = request.Header(i);
                Headers.Add(key, value);
            }
        }

        public bool HasHeader(string key)
        {
            foreach (var item in Headers)
            {
                if (string.Equals(key, item.Key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        
        public string GetHeader(string key)
        {
            foreach (var item in Headers)
            {
                if (string.Equals(key, item.Key, StringComparison.OrdinalIgnoreCase)) return item.Value;
            }
            return string.Empty;
        }
        
        public override void OnWsConnected(HttpRequest request)
        {
            SetHeaders(request);
            if (HasHeader("X-Forwarded-For"))
            {
                ClientIP = GetHeader("X-Forwarded-For");
            }
            else if (HasHeader("X-Real-IP"))
            {
                ClientIP = GetHeader("X-Real-IP");
            }
            else
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

            if (ClientIP.Contains(","))
            {
                var ipItem = ClientIP.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ipItem))
                {
                    ClientIP = ipItem.Trim();
                }
            }
            Console.WriteLine($"WebSocket session with Id {Id} connected: {ClientIP}");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            AppendToStash(buffer, (int)offset, (int)size);
            DrainStash();
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
            // 循环 drain：每轮从当前 pos 读取一个完整 pack，直到凑不齐为止。
            // 对标 TcpServer.ReadFromClient 已修复的"一次只 parse 一个 pack"半包 bug，
            // 避免 client pipeline 连发场景下后续 pack 卡在用户态 buffer 里等不到新事件。
            while (m_stashLen - pos >= sizeof(ushort))
            {
                var len = BinaryPrimitives.ReadUInt16LittleEndian(
                    new ReadOnlySpan<byte>(m_stash, pos, sizeof(ushort)));
                if (m_stashLen - pos - sizeof(ushort) < len) break;

                // 下游 InvokeOnDataReceived 最终会走 Package.ParseRaw(byte[])；
                // 为保持现有 API 兼容（下游异步持有 Package.RawData），这里拷贝出独立 byte[]。
                // stash 本身是 ArrayPool 的，生命周期 & 扩容开销大，因此 stash 复用仍然是大头收益。
                var packData = new byte[len];
                System.Buffer.BlockCopy(m_stash, pos + sizeof(ushort), packData, 0, len);
                pos += sizeof(ushort) + len;

                PackServer.OnRecv(this, packData);
            }

            if (pos == 0) return;
            var remaining = m_stashLen - pos;
            if (remaining > 0)
            {
                System.Buffer.BlockCopy(m_stash, pos, m_stash, 0, remaining);
            }
            m_stashLen = remaining;
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"WebSocket session with Id {Id} disconnected!");
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session[{ClientId}] caught an error with code {error}");
        }
    }

    class WsPackServer : global::NetCoreServer.WsServer
    {
        internal WsServer m_server;
        private IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        private ConcurrentDictionary<uint, WsPackSession> m_sessions = new ConcurrentDictionary<uint, WsPackSession>();
        // private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

        public WsPackServer(WsServer server, IPAddress address, int port) : base(address, port)
        {
            m_server = server;
        }

        protected override TcpSession CreateSession() { return new WsPackSession(this, m_idGen.Next()); }

        protected override void OnConnected(TcpSession session)
        {
            base.OnConnected(session);
            if (session is WsPackSession packSession == false) return;
            
            m_sessions[packSession.ClientId] = packSession;
            m_server.OnConnected(packSession.ClientId);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            if (session is WsPackSession packSession == false) return;

            m_server.OnDisconnected(packSession.ClientId);
            m_sessions.TryRemove(packSession.ClientId, out _);
        }

        internal void OnRecv(TcpSession session, byte[] data)
        {
            if (session is WsPackSession packSession == false) return;
            
            m_server.InvokeOnDataReceived(packSession.ClientId, data);
            // m_readChannel.Add((packSession.ClientId, data), m_server.CancellationToken);
        }

        // public (uint, byte[]) Recv()
        // {
        //     return m_readChannel.Take(m_server.CancellationToken);
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
                    
                    session.SendBinaryAsync(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 零拷贝批量发送：<paramref name="framedBytes"/> 已经包含每个 pack 的 ushort 外层长度前缀，
        /// 由 SessionSender 一次性拼装完成；这里直接 forward 到 session.SendBinaryAsync(span)，
        /// 避免 MemoryStream.ToArray + 重复 prefix 计算。
        /// </summary>
        public void SendFramed(uint clientId, ReadOnlySpan<byte> framedBytes)
        {
            if (!GetSession(clientId, out var session)) return;
            session.SendBinaryAsync(framedBytes);
        }

        public bool GetSession(uint clientId, out WsPackSession session)
        {
            return m_sessions.TryGetValue(clientId, out session);
        }
    }
    
    class StaticContentParams
    {
        public string path;
        public string prefix = "/";
        public string filter = "*.*";
        public TimeSpan? timeout = null;
    }
    
    public class WsServer : TransportServerBase, IAddStaticContent, IGetClientBrowser, IGetHttpHeader
    {
        private WsPackServer m_server;
        private CancellationTokenSource m_cancelSource;

        internal CancellationToken CancellationToken => m_cancelSource.Token;
        
        protected virtual string KeyPath => "server.pfx";
        protected virtual string KeyPass => "qwerty";
        
        private List<StaticContentParams> _staticContentParams = new List<StaticContentParams>();
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            
            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            m_server = new WsPackServer(this, address, port);
            
            foreach (var param in _staticContentParams)
            {
                m_server.AddStaticContent(param.path, param.prefix, param.filter, param.timeout);
            }
            
            m_server.Start();
        }

        public override void Stop()
        {
            if (m_server == null) return;
            
            m_server.Dispose();
            m_server = null;
        }

        public void AddStaticContent(string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null)
        {
            _staticContentParams.Add(new StaticContentParams
            {
                path = path,
                prefix = prefix,
                filter = filter,
                timeout = timeout
            });
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
        public override System.Threading.Tasks.ValueTask SendAsync(uint clientId, ReadOnlyMemory<byte> framedBytes, CancellationToken ct)
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
            session.Close(0);
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

        public string GetClientBrowser(uint clientId)
        {
            if (!m_server.GetSession(clientId, out var session)) return string.Empty;
            
            return session.GetHeader("User-Agent");
        }

        public Dictionary<string, string> GetHttpHeaders(uint clientId)
        {
            if (!m_server.GetSession(clientId, out var session)) return null;
            return session.Headers;
        }

        public string GetHttpHeader(uint clientId, string header)
        {
            if (!m_server.GetSession(clientId, out var session)) return string.Empty;
            return session.GetHeader(header);
        }

        public bool HasHttpHeader(uint clientId, string header)
        {
            if (!m_server.GetSession(clientId, out var session)) return false;
            return session.HasHeader(header);
        }
    }
}