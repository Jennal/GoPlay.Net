using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;
using GoPlay.Core.Utils;

namespace GoPlay.Core.Senders
{
    /// <summary>
    /// 每个 client session 一个发送器：
    /// - 专属 <see cref="Channel{T}"/> 缓冲出站 <see cref="Package"/>
    /// - async 循环 drain + 小包聚合（字节阈值 / 时间阈值二选一触发 flush）
    /// - 集成 IsBlockSendByFilter / PostSendFilter 逐包决策
    /// - 通过 <see cref="TransportServerBase.SendAsync"/> 把聚合后的字节一次性推给 transport
    ///
    /// 同一 session 内消息保持入队顺序；跨 session 天然并行，不再经由全局单线程 SendLoop 串行化。
    /// </summary>
    internal sealed class SessionSender
    {
        /// <summary>
        /// 聚合触发阈值：累计字节数达到后立刻 flush。
        /// 数值向 NIC 典型发送单元靠拢，避免过大导致延迟、过小丧失聚合收益。
        /// </summary>
        private const int DefaultFlushBytesThreshold = 16 * 1024;

        /// <summary>
        /// 队列容量：与旧版 Server.m_sendQueue 保持一致的 back-pressure 语义。
        /// </summary>
        private const int DefaultCapacity = ushort.MaxValue;

        private readonly uint _clientId;
        private readonly Server _server;
        private readonly TransportServerBase _transport;
        private readonly Channel<Package> _outgoing;
        private readonly int _flushBytesThreshold;
        private readonly CancellationTokenSource _stopCts;
        private Task _runTask;

        public int QueueCount => _outgoing.Reader.Count;

        public SessionSender(uint clientId, Server server, TransportServerBase transport,
                             int capacity = DefaultCapacity,
                             int flushBytesThreshold = DefaultFlushBytesThreshold)
        {
            _clientId = clientId;
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _flushBytesThreshold = Math.Max(1, flushBytesThreshold);

            _outgoing = Channel.CreateBounded<Package>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
            _stopCts = new CancellationTokenSource();
        }

        public void Start(CancellationToken serverToken)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _stopCts.Token);
            // 100 client 同时连入时，100 个 SessionSender 同时 Task.Run(RunAsync) 排 ThreadPool 队列；
            // 而 server 的 handshake resp 靠这里的 RunAsync 第一轮 drain channel 发出——排队导致
            // handshake resp 延迟 >10s，client 侧 RequestTimeout 触发 Connect 返回 false。
            // 改 TaskUtil.LongRun（LongRunning 专用线程），与 Client.SendLoop / RecvLoop / TimeoutLoop 对称，
            // 每 session 一个发送线程，换取 handshake 阶段零排队。
            _runTask = TaskUtil.LongRun(
                () => RunAsync(linked.Token).GetAwaiter().GetResult(),
                linked.Token);
        }

        /// <summary>
        /// 停止发送器：先 Complete writer 让 loop 自然退出，超时兜底再 Cancel。
        /// </summary>
        public async Task StopAsync(TimeSpan drainTimeout)
        {
            _outgoing.Writer.TryComplete();
            if (_runTask == null) return;

            try
            {
                var completed = await Task.WhenAny(_runTask, Task.Delay(drainTimeout)).ConfigureAwait(false);
                if (completed != _runTask)
                {
                    _stopCts.Cancel();
                    try { await _runTask.ConfigureAwait(false); } catch { /* best-effort */ }
                }
            }
            catch (OperationCanceledException) { /* IGNORE */ }
            catch (AggregateException err) when (err.InnerException is OperationCanceledException) { /* IGNORE */ }
        }

        /// <summary>
        /// 由 <see cref="Server.Send"/> / Kick 等路径调用。
        /// Channel 已满则异步等位，避免在调度器线程上同步阻塞。
        /// </summary>
        public bool Enqueue(Package pack)
        {
            if (_outgoing.Writer.TryWrite(pack)) return true;

            // 满了：异步等位，保留 back-pressure 语义
            _ = WriteSlowAsync(pack);
            return false;
        }

        /// <summary>
        /// 枚举当前 Channel 中缓存的包（快照，供监控/诊断）。
        /// Channel 本身不暴露 ToList，这里仅聚合当前计数与一次性拷贝读者侧可见包；
        /// 注意此方法会移除 channel 中的包，仅在监控/停机路径上使用。
        /// </summary>
        public List<Package> DrainSnapshot()
        {
            var list = new List<Package>();
            while (_outgoing.Reader.TryRead(out var p)) list.Add(p);
            return list;
        }

        private async Task WriteSlowAsync(Package pack)
        {
            try
            {
                await _outgoing.Writer.WriteAsync(pack, _stopCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception err)
            {
                _server.OnErrorEvent(_clientId, err);
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            var reader = _outgoing.Reader;
            var writer = new ArrayBufferWriter<byte>(_flushBytesThreshold);
            // flush 一批前临时收集 last-chunk 集合，用于批送成功后触发 PostSendFilter
            var postSendList = new List<Package>();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!await reader.WaitToReadAsync(ct).ConfigureAwait(false)) break;
                }
                catch (OperationCanceledException) { break; }

                // 聚合：把当前可读的包尽量一次写入 buffer
                while (reader.TryRead(out var pack))
                {
                    if (pack.IsLastChunk && _server.IsBlockSendByFilter(pack)) continue;

                    try
                    {
                        pack.WriteTo(writer);
                    }
                    catch (Exception err)
                    {
                        _server.OnErrorEvent(_clientId, err);
                        continue;
                    }

                    if (pack.IsLastChunk) postSendList.Add(pack);

                    if (writer.WrittenCount >= _flushBytesThreshold) break;
                }

                if (writer.WrittenCount == 0) continue;

                try
                {
                    await _transport.SendAsync(_clientId, writer.WrittenMemory, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception err)
                {
                    _server.OnErrorEvent(_clientId, err);
                }
                finally
                {
#if NET8_0_OR_GREATER
                    writer.ResetWrittenCount();
#else
                    writer.Clear();
#endif
                }

                // flush 成功后再触发 post filter；避免 filter 副作用领先于真实写入
                for (var i = 0; i < postSendList.Count; i++)
                {
                    try { _server.PostSendFilter(postSendList[i]); }
                    catch (Exception err) { _server.OnErrorEvent(_clientId, err); }
                }
                postSendList.Clear();
            }
        }
    }
}
