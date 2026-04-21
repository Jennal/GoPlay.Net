using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Transports;
using TcpClient = NetCoreServer.TcpClient;

namespace GoPlay.Core.Transport.NetCoreServer
{
    class PackClient : TcpClient
    {
        private NcClient m_client;
        
        public BlockingCollection<byte[]> RecvChannel = new BlockingCollection<byte[]>(byte.MaxValue);
        public SocketError LastError;

        private CancellationToken m_token;

        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;
        
        public PackClient(NcClient client, IPAddress address, int port, CancellationToken token) : base(address, port)
        {
            m_client = client;
            m_token = token;
        }

        protected override void OnConnected()
        {
            m_client.InvokeOnConnected();
        }

        protected override void OnDisconnected()
        {
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
            m_client.InvokeOnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
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

                var packData = new byte[len];
                System.Buffer.BlockCopy(m_stash, pos + sizeof(ushort), packData, 0, len);
                pos += sizeof(ushort) + len;

                // socket 完成回调线程上执行，未处理异常会让进程 failfast（net8 下尤其明显）。
                // Disconnect 期间 token 被 cancel / channel 被 dispose，丢弃未消费数据即可。
                try
                {
                    if (m_token.IsCancellationRequested) return;
                    RecvChannel.Add(packData, m_token);
                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }
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

        protected override void Dispose(bool disposingManagedResources)
        {
            base.Dispose(disposingManagedResources);
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
            RecvChannel.Dispose();
        }
    }
    
    public class NcClient : TransportClientBase
    {
        private PackClient m_client;
        private CancellationTokenSource m_cancelSource;

        public override bool IsConnected => m_client?.IsConnected ?? false;

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            // 兼容保留：少数场景调用方仍走同步路径。内部委托给 async 版本再 GetAwaiter().GetResult()，
            // 避免两套实现分叉。高并发场景请走 ConnectAsync（Client<T> 已切到它）。
            try
            {
                ConnectAsync(host, port, timeout).GetAwaiter().GetResult();
            }
            catch (AggregateException ae) when (ae.InnerException != null)
            {
                throw ae.InnerException;
            }
        }

        /// <summary>
        /// 真异步 Connect：用 <see cref="TaskCompletionSource{TResult}"/> 监听 <c>OnConnected</c> 事件，
        /// <see cref="CancellationTokenSource.CancelAfter"/> 负责 timeout。整条等待链不占 ThreadPool worker，
        /// 从根上消除 "100 并发 Task.Run + 同步 Connect → worker 饥饿 → socket 回调无线程可用" 的
        /// 死锁类 flaky（<c>BenchmarkMultiClientRequest</c> 是典型场景）。
        /// </summary>
        public override async Task ConnectAsync(string host, int port, TimeSpan timeout)
        {
            m_cancelSource = new CancellationTokenSource();

            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (m_client != null) m_client.Dispose();

                // 每个地址一次独立的 TCS：多地址尝试（IPv4 多条 A 记录）互不污染。
                // RunContinuationsAsynchronously：防止 OnConnected 触发线程被后续 await 续跑拖住，
                // 保持事件回调线程干净（与 NetCoreServer IOCP 线程池语义一致）。
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Action onConn = () => tcs.TrySetResult(true);
                OnConnected += onConn;

                using var timeoutCts = new CancellationTokenSource();
                using var _ = timeoutCts.Token.Register(() => tcs.TrySetResult(false));

                try
                {
                    m_client = new PackClient(this, address, port, m_cancelSource.Token);
                    m_client.ConnectAsync();

                    // race guard：PackClient.ConnectAsync 理论上可在 ConnectAsync 返回前同步触发
                    // OnConnected（比如 loopback 极速路径）。订阅先于 ConnectAsync 调用保证 onConn
                    // 能接到；这里冗余补一刀 TrySetResult 纯粹保险。
                    if (m_client.IsConnected) tcs.TrySetResult(true);

                    timeoutCts.CancelAfter(timeout);

                    var ok = await tcs.Task.ConfigureAwait(false);
                    if (!ok)
                    {
                        Console.WriteLine($"Connect timeout: {timeout}");
                        throw new Exception("Connect timeout!");
                    }

                    return;
                }
                catch
                {
                    // 旧同步实现也是吞异常→下一个地址，保持语义一致
                    continue;
                }
                finally
                {
                    OnConnected -= onConn;
                }
            }

            throw new Exception("host can't be reached!");
        }

        public override void Disconnect()
        {
            if (m_client == null) return;
            if (m_cancelSource == null) return;
            
            m_cancelSource.Cancel();
            
            m_client.Dispose();
            m_client = null;
        }

        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            return new ValueTask<byte[]>(m_client.RecvChannel.Take(cancelSource.Token));
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
                    
                    m_client.SendAsync(ms.ToArray());
                }
            }
            
            return new ValueTask();
        }

        /// <summary>
        /// 零拷贝发送：<paramref name="data"/> 已经是完整 wire frame（含 outer ushort 长度前缀，
        /// 由 <see cref="GoPlay.Core.Protocols.Package.WriteTo"/> 直接写入 <see cref="ArrayBufferWriter{T}"/> 得到）。
        /// 直接 forward 到 NetCoreServer 底层 <c>SendAsync(ReadOnlySpan&lt;byte&gt;)</c>，
        /// 完全跳过 byte[] 老路径 <c>Send(byte[])</c> 里的 <see cref="MemoryStream"/> + <see cref="BinaryWriter"/> + <c>ms.ToArray()</c>。
        /// 与 Server 端 <c>NcServer.SendAsync(clientId, ReadOnlyMemory)</c> 走的是同一契约。
        /// </summary>
        public override ValueTask Send(ReadOnlyMemory<byte> data, CancellationTokenSource cancelSource)
        {
            if (cancelSource.IsCancellationRequested) return new ValueTask();

            m_client.SendAsync(data.Span);
            return new ValueTask();
        }

        public override void Dispose()
        {
            m_client?.Dispose();
            m_cancelSource?.Dispose();
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