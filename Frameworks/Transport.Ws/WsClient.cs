using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Transports;
using NetCoreServer;

namespace GoPlay.Core.Transport.Ws
{
    class PackClient : NetCoreServer.WsClient
    {
        private SocketError LastError;
        
        private WsClient m_client;
        private CancellationToken m_token;
        private string m_host;
        private int m_port;
        private bool m_isUpgraded;
        private bool m_wsConnected;

        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;
        
        public bool IsUpgraded => m_isUpgraded;
        public bool WsConnected => m_wsConnected;
        
        public PackClient(WsClient client, string host, IPAddress address, int port, CancellationToken token) : base(address, port)
        {
            m_client = client;
            m_host = host;
            m_port = port;
            m_token = token;
            m_isUpgraded = false;
            m_wsConnected = false;
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", m_host);
            request.SetHeader("Origin", $"https://{m_host}:{m_port}");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "GoPlay.Net");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();

            m_isUpgraded = true;
        }

        public override void OnWsConnected(HttpResponse response)
        {
            // Console.WriteLine($"WebSocket client connected a new session with Id {Id}");
            m_wsConnected = true;
            m_client.InvokeOnConnected();
        }

        public override void OnWsDisconnected()
        {
            // Console.WriteLine($"WebSocket client disconnected a session with Id {Id}");
            m_wsConnected = false;
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
            m_client.InvokeOnDisconnected();
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
            while (m_stashLen - pos >= sizeof(ushort))
            {
                var len = BinaryPrimitives.ReadUInt16LittleEndian(
                    new ReadOnlySpan<byte>(m_stash, pos, sizeof(ushort)));
                if (m_stashLen - pos - sizeof(ushort) < len) break;

                // Step 3.15（客户端侧对称）：span 直 push，省掉 new byte[len]+BlockCopy+BlockingCollection 一跳。
                // 生命周期契约：frameSpan 只在 DispatchSpan 返回前有效；Client&lt;T&gt; span handler 同步
                // ParseRaw(ReadOnlySpan<byte>)，body 需 async 持有的在 ParseRaw 内 ToArray 拷出。
                // socket 完成回调线程未处理异常会 failfast，tempated 吞 disconnect 期间的异常。
                try
                {
                    if (m_token.IsCancellationRequested) return;
                    var frameSpan = new ReadOnlySpan<byte>(m_stash, pos + sizeof(ushort), len);
                    m_client.DispatchSpan(frameSpan);
                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }

                pos += sizeof(ushort) + len;
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
            LastError = error;
        }
    }
    
    public class WsClient : TransportClientBase
    {
        private PackClient m_client;
        private CancellationTokenSource m_cancelSource;

        protected virtual string KeyPath => "client.pfx";
        protected virtual string KeyPass => "qwerty";

        public override bool IsConnected => m_client?.IsConnected ?? false;

        /// <summary>
        /// WsClient 走 IOCP 回调 + DrainStash 直 push 到 Client&lt;T&gt;，消除 pull-over-push 双缓冲。
        /// </summary>
        public override bool SupportPush => true;

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            m_cancelSource = new CancellationTokenSource();
            
            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (m_client != null) m_client.Dispose();
                
                try
                {
                    m_client = new PackClient(this, host, address, port, m_cancelSource.Token);

                    m_client.ConnectAsync();
                    var startTime = DateTime.UtcNow;
                    while (!m_client.IsConnected || !m_client.IsUpgraded || !m_client.WsConnected)
                    {
                        Thread.Yield();
                        var ts = DateTime.UtcNow.Subtract(startTime);
                        if (ts > timeout) throw new Exception("Connect timeout!");
                    }

                    return;
                }
                catch
                {
                    continue;
                }
            }

            throw new Exception("host can't be reached!");
        }

        public override void Disconnect()
        {
            if (m_client == null) return;
            
            m_cancelSource.Cancel();
            m_cancelSource.Dispose();
            m_cancelSource = null;
            
            m_client.Dispose();
            m_client = null;
        }

        /// <summary>
        /// 供 inner <see cref="PackClient"/> 的 IOCP 回调线程调用，把一帧 inner bytes 直接推给 Client 的 span handler。
        /// 基类 <see cref="TransportClientBase.InvokeOnDataReceivedSpan"/> 是 protected，inner class 无法直接访问，这里转发。
        /// </summary>
        internal void DispatchSpan(ReadOnlySpan<byte> data)
        {
            InvokeOnDataReceivedSpan(data);
        }

        /// <summary>
        /// Push transport 不走 pull 路径；Client&lt;T&gt; 在 Connect 里按 <see cref="SupportPush"/>=true 分支
        /// 只启动 SendLoop+TimeoutLoop。走到本方法说明上层 Connect 路径错走到 pull 分支。
        /// </summary>
        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            throw new NotSupportedException(
                "WsClient is a push transport (SupportPush=true); Recv() is unused. " +
                "Check that Client<T>.Connect correctly branches on Transport.SupportPush.");
        }

        public override ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            if (cancelSource.IsCancellationRequested) return new ValueTask();
            
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)data.Length);
                    bw.Write(data);
                    
                    m_client.SendBinaryAsync(ms.ToArray());
                }
            }
            
            return new ValueTask();
        }

        /// <summary>
        /// 零拷贝发送：与 <see cref="NcClient"/> 同契约。
        /// <paramref name="data"/> 已是完整 wire frame（含 outer ushort 前缀），直接给底层
        /// <c>SendBinaryAsync(ReadOnlySpan&lt;byte&gt;)</c>，跳过 byte[] 老路径的 MemoryStream + ToArray 分配。
        /// </summary>
        public override ValueTask Send(ReadOnlyMemory<byte> data, CancellationTokenSource cancelSource)
        {
            if (cancelSource.IsCancellationRequested) return new ValueTask();

            m_client.SendBinaryAsync(data.Span);
            return new ValueTask();
        }

        public override void Dispose()
        {
            m_cancelSource?.Dispose();
            m_client?.Dispose();
        }
        
        internal new void InvokeOnConnected()
        {
            base.InvokeOnConnected();
        }

        internal new void InvokeOnDisconnected()
        {
            base.InvokeOnDisconnected();
        }
    }
}