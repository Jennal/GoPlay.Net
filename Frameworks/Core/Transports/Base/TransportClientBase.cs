using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Transports
{
    public abstract class TransportClientBase : IDisposable
    {
        public event Action OnConnected;
        public event Action OnDisconnected;

        public abstract bool IsConnected { get; }

        public virtual void Connect(string host, int port)
        {
            Connect(host, port, Consts.TimeOut.Connect);
        }
        
        public abstract void Connect(string host, int port, TimeSpan timeout);

        /// <summary>
        /// 异步连接。返回的 Task 完成时连接已建立（或抛出异常 / 触发 OnConnected 事件）。
        ///
        /// <para>
        /// 为什么需要这个方法：同步 <see cref="Connect(string,int,System.TimeSpan)"/> 在子类里
        /// 典型实现是 busy-spin 等 <c>IsConnected=true</c>（见 <c>NcClient</c> / <c>WsClient</c>），
        /// 单个连接下无感；但在 100 个 <c>Task.Run</c> 并发发起 Connect 的压力场景下，
        /// 100 个 ThreadPool worker 全部被 spin-loop 占住 → 底层 socket 完成回调
        /// （该回调也走 ThreadPool，把 <c>IsConnected</c> 置 true）排不上执行 → 10s 内仍
        /// <c>false</c> → 全部超时。本方法允许各 transport 用 <c>TaskCompletionSource</c> +
        /// <c>OnConnected</c> 事件真异步实现，不占 worker。
        /// </para>
        ///
        /// <para>
        /// 默认实现回退到 <see cref="Connect(string,int,System.TimeSpan)"/> 外包一层 <see cref="Task.Run"/>，
        /// 保持向后兼容但<b>不</b>根治饥饿。各官方 transport 建议逐个覆写。
        /// </para>
        /// </summary>
        public virtual Task ConnectAsync(string host, int port, TimeSpan timeout)
        {
            return Task.Run(() => Connect(host, port, timeout));
        }

        public abstract void Disconnect();

        public abstract ValueTask<byte[]> Recv(CancellationTokenSource cancelSource);
        public abstract ValueTask Send(byte[] data, CancellationTokenSource cancelSource);

        /// <summary>
        /// 零拷贝发送：调用方（<see cref="GoPlay.Client.SendLoopAsync"/>）拿
        /// <see cref="System.Buffers.ArrayBufferWriter{T}"/> 承接
        /// <see cref="GoPlay.Core.Protocols.Package.WriteTo"/> 的输出，然后以
        /// <c>writer.WrittenMemory</c> 直接传入这里。
        ///
        /// <para>
        /// <b>wire 契约差异（重要）</b>：
        /// - <see cref="Send(byte[], CancellationTokenSource)"/>：<c>data</c> 是 <b>inner</b>（不含 outer ushort 长度前缀），
        ///   子类内部自行加前缀后写 socket。
        /// - <see cref="Send(ReadOnlyMemory{byte}, CancellationTokenSource)"/>：<c>data</c> 是 <b>完整 wire frame</b>
        ///   （<see cref="GoPlay.Core.Protocols.Package.WriteTo"/> 已经写入 outer ushort 前缀），子类直接 socket 下发。
        /// 两个重载契约不兼容，不能简单互转。
        /// </para>
        ///
        /// <para>
        /// 线程契约：子类必须在<b>返回 ValueTask 之前</b>把 data 拷贝到自己内部的 pending buffer，
        /// 一旦 await 完成，调用方可以立刻复用 / Reset 原 buffer。Server 端
        /// <c>TransportServerBase.SendAsync(clientId, ReadOnlyMemory&lt;byte&gt;, ct)</c> 走的是同一契约。
        /// </para>
        ///
        /// <para>
        /// 默认实现：把 outer 前缀剥掉再 fallback 到 byte[] 重载，保证协议语义正确。
        /// 这条路径会产生一次 <c>ToArray</c> 分配，失去零拷贝效果；所有官方 transport
        /// （NcClient / WsClient / WssClient / TcpClient）都已覆写此方法直接走底层
        /// span 版 SendAsync，不会走到这条 fallback。自定义 transport 建议覆写。
        /// </para>
        /// </summary>
        public virtual ValueTask Send(ReadOnlyMemory<byte> data, CancellationTokenSource cancelSource)
        {
            if (data.Length < sizeof(ushort))
                throw new ArgumentException(
                    "Send(ReadOnlyMemory): wire frame too short; must contain outer ushort length prefix",
                    nameof(data));

            // 剥掉 WriteTo 写入的 outer 前缀，交给 byte[] 版本（其内部会自己重新加前缀），保持净效果一致
            var inner = data.Slice(sizeof(ushort)).ToArray();
            return Send(inner, cancelSource);
        }

        public abstract void Dispose();

        protected void InvokeOnConnected()
        {
            OnConnected?.Invoke();
        }

        protected void InvokeOnDisconnected()
        {
            OnDisconnected?.Invoke();
        }
    }
}