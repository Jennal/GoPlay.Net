using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Transports
{
    /// <summary>
    /// Span 版收包回调（客户端版）：子类（<c>NcClient</c> / <c>WsClient</c> / <c>WssClient</c>）
    /// 在 <c>DrainStash</c> 同步循环内把 stash 里的一帧以 <see cref="ReadOnlySpan{T}"/> 直接推给
    /// 上层（<see cref="GoPlay.Client{T}"/>），省掉 transport 层 <c>new byte[len] + BlockCopy</c>
    /// 以及一次 <c>BlockingCollection&lt;byte[]&gt;</c> 过境。
    ///
    /// <para><b>生命周期契约</b>：<paramref name="data"/> 只在 handler 返回前有效 —— 通常是 transport
    /// <c>DrainStash</c> 同步循环内部，handler 内走 <see cref="GoPlay.Core.Protocols.Package.ParseRaw(ReadOnlySpan{byte})"/>
    /// 同步解码；body 需要 async 持有的那一份在 <c>ParseRaw</c> 里 <c>.ToArray()</c> 拷出独立 byte[]。
    /// 不要在 handler 里 await / 入队原始 span。</para>
    /// </summary>
    public delegate void ClientDataReceivedSpanHandler(ReadOnlySpan<byte> data);

    public abstract class TransportClientBase : IDisposable
    {
        public event Action OnConnected;
        public event Action OnDisconnected;

        public abstract bool IsConnected { get; }

        // Span 版不能用 event（ref struct 受 event 访问器限制），且同一 transport 只会有一个 Client 订阅者，单字段更简单。
        private ClientDataReceivedSpanHandler m_onDataReceivedSpan;

        /// <summary>
        /// 绑定 span 版收包 handler。<see cref="GoPlay.Client{T}.Connect"/> 在启动 SendLoop 之前调用；
        /// 传 <c>null</c> 表示解绑。对于 <see cref="SupportPush"/>=<c>false</c> 的 transport（如 TCP），
        /// Client 走传统 pull 路径（<see cref="Recv"/>），不会调用本方法。
        /// </summary>
        public void SetDataReceivedSpanHandler(ClientDataReceivedSpanHandler handler)
        {
            m_onDataReceivedSpan = handler;
        }

        /// <summary>
        /// 子类探针：span handler 是否已绑定。用于测试 / 诊断，验证 <see cref="GoPlay.Client{T}.Connect"/>
        /// 已正确穿过 push 就绪屏障（缺陷 3 的核心不变量）。生产路径不应依赖此属性做分支。
        /// </summary>
        protected bool HasDataReceivedSpanHandler => m_onDataReceivedSpan != null;

        /// <summary>
        /// 是否支持 IOCP 回调直接 push 到 <see cref="GoPlay.Client{T}"/>。
        /// 默认 <c>false</c>，Client 退回 <see cref="Recv"/> pull 模式（供 <c>TcpClient</c> 等没有 IOCP
        /// 回调、只能 spin 式 <c>Socket.Select</c> 的 transport 使用）。
        /// Nc/Ws/Wss 覆写为 <c>true</c>，消除 pull-over-push 双缓冲。
        /// </summary>
        public virtual bool SupportPush => false;

        /// <summary>
        /// 子类 <c>DrainStash</c> 内同步调用，把已切好的 inner 帧直接推给 Client。
        /// 未绑定 span handler 时 silently drop（对 push-only transport 不会发生，push transport 的
        /// 上层 <see cref="GoPlay.Client{T}"/> 总会在 <see cref="GoPlay.Client{T}.Connect"/> 里
        /// <see cref="SetDataReceivedSpanHandler"/>）。
        /// </summary>
        protected void InvokeOnDataReceivedSpan(ReadOnlySpan<byte> data)
        {
            m_onDataReceivedSpan?.Invoke(data);
        }

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