using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Transports
{
    public abstract class TransportServerBase
    {
        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, Exception> OnError;
        public event Action<(uint, byte[])> OnDataReceived;

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
    }
}