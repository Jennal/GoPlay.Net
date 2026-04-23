using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Transports
{
    /// <summary>
    /// Span 版收包回调：把 stash 里的一帧直接以 <see cref="ReadOnlySpan{T}"/> 形式交给上层（Server），
    /// 省掉 transport 层 <c>new byte[len] + BlockCopy</c> 这一份分配。
    ///
    /// <para><b>生命周期契约</b>：<paramref name="data"/> 只在 handler 返回前有效——通常是 transport
    /// DrainStash 同步循环内部，handler 内走 <see cref="GoPlay.Core.Protocols.Package.ParseRaw(ReadOnlySpan{byte})"/>
    /// 同步解码，body 需要 async 持有的那一份在 ParseRaw 里 <c>.ToArray()</c> 拷出独立 byte[]。
    /// 不要在 handler 里 await / 入队原始 span。</para>
    /// </summary>
    public delegate void DataReceivedSpanHandler(uint clientId, ReadOnlySpan<byte> data);

    public abstract class TransportServerBase
    {
        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, Exception> OnError;
        public event Action<(uint, byte[])> OnDataReceived;

        // Span 版不能用 event（delegate 签名含 ref struct 仍受 event 访问器语法的限制，
        // 且语义上同一 transport 只会有一个 Server 订阅者，用单字段更简单）
        private DataReceivedSpanHandler m_onDataReceivedSpan;

        /// <summary>
        /// 绑定 span 版收包 handler。Server 构造时调用，替代向 <see cref="OnDataReceived"/> 订阅。
        /// 传 null 表示解绑（退回 <see cref="OnDataReceived"/> byte[] 路径）。
        /// </summary>
        public void SetDataReceivedSpanHandler(DataReceivedSpanHandler handler)
        {
            m_onDataReceivedSpan = handler;
        }

        public virtual bool SupportPush => true;
        
        public abstract void Start(string host, int port, CancellationTokenSource cancelSource=null);
        public abstract void Stop();

        // public abstract (uint, byte[]) Recv();
        public abstract void Send(uint clientId, byte[] data);

        /// <summary>
        /// 新发送入口：接收已经 framed 的 wire bytes（即每个 pack 都带 ushort 外层长度前缀），
        /// 允许 transport 子类把多个 pack 聚合到一次 socket 写入，实现零拷贝/小包聚合。
        ///
        /// 默认实现（兼容老 transport）：把 framed 字节还原成逐个 inner pack 的 byte[]，
        /// 调用老的 <see cref="Send(uint, byte[])"/>，保留原有"逐包加前缀"行为。
        /// 性能敏感 transport（Ws、Nc 等）应该 override 这个方法，直接把 framed 字节推给底层 session。
        /// </summary>
        public virtual ValueTask SendAsync(uint clientId, ReadOnlyMemory<byte> framedBytes, CancellationToken ct)
        {
            var span = framedBytes.Span;
            while (span.Length >= sizeof(ushort))
            {
                var innerLen = BinaryPrimitives.ReadUInt16LittleEndian(span);
                span = span.Slice(sizeof(ushort));
                if (span.Length < innerLen) break; // 坏帧：上游 SessionSender 保证不会走到这
                var innerBytes = span.Slice(0, innerLen).ToArray();
                Send(clientId, innerBytes);
                span = span.Slice(innerLen);
            }
            return default;
        }

        public abstract string GetClientIp(uint clientId);

        public abstract void DisconnectClient(uint clientId, Exception err);
        
        protected void InvokeOnClientConnected(uint clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }
        
        protected void InvokeOnClientDisconnected(uint clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }

        protected void InvokeOnError(uint clientId, Exception err)
        {
            OnError?.Invoke(clientId, err);
        }
        
        public void InvokeOnDataReceived(uint clientId, byte[] data)
        {
            OnDataReceived?.Invoke((clientId, data));
        }

        /// <summary>
        /// Span 版收包 dispatch：transport 子类 DrainStash 直接以 stash 切片调用此方法，
        /// 省掉一次整帧 <c>new byte[len]</c> 分配。
        ///
        /// 若 <see cref="SetDataReceivedSpanHandler"/> 未被调用（如老的自定义 transport 宿主），
        /// 回退到 <see cref="InvokeOnDataReceived"/> byte[] 路径（data.ToArray() 后发 event），
        /// 保证老代码 0 改动仍然可用。
        /// </summary>
        public void InvokeOnDataReceivedSpan(uint clientId, ReadOnlySpan<byte> data)
        {
            var handler = m_onDataReceivedSpan;
            if (handler != null)
            {
                handler(clientId, data);
                return;
            }

            // Fallback：没人绑 span handler → 回退 byte[] event 路径，语义等价 Step 3.13 及以前
            InvokeOnDataReceived(clientId, data.ToArray());
        }
    }
}