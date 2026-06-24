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
    /// - 专属 <see cref="Channel{T}"/> 缓冲出站 <see cref="Package"/>（同 session 内严格保序）
    /// - **不再独占线程**：自身只是一个"可调度单元"，由共享的 <see cref="SessionSendPump"/>
    ///   用固定 N 条 worker 线程驱动 drain + 小包聚合（字节阈值触发 flush）
    /// - 集成 IsBlockSendByFilter / PostSendFilter 逐包决策
    /// - 通过 <see cref="TransportServerBase.SendAsync"/> 把聚合后的字节一次性推给 transport
    ///
    /// <para>
    /// 旧版每 session 一条 <c>LongRunning</c> 线程，连接数 = OS 线程数，几千连接即撞内存/调度墙；
    /// 且该线程在第一个 <c>await</c> 后其实把 continuation 甩回 ThreadPool、自己空转阻塞。
    /// 现版改为 M:N：线程数与连接数解耦（详见 <see cref="SessionSendPump"/>）。
    /// </para>
    /// </summary>
    internal sealed class SessionSender
    {
        /// <summary>
        /// 聚合触发阈值：累计字节数达到后立刻 flush。
        /// 数值向 NIC 典型发送单元靠拢，避免过大导致延迟、过小丧失聚合收益。
        /// </summary>
        internal const int DefaultFlushBytesThreshold = 16 * 1024;

        /// <summary>
        /// 队列容量：与旧版 Server.m_sendQueue 保持一致的 back-pressure 语义。
        /// </summary>
        private const int DefaultCapacity = ushort.MaxValue;

        // 单飞标志：0 = 不在就绪队列且无 worker 在处理；1 = 已入就绪队列 / 正在被某 worker 处理。
        // 保证"同一 sender 同一时刻只被一条 worker serve"，从而维持 per-client 顺序与无锁互斥。
        private int _scheduled;
        // 1 = StopAsync 已超时，要求 pump 丢弃残留、尽快收尾。
        private int _abort;
        // StopAsync 已请求停止（writer 已 complete）。drain 干净后据此把 _drainedTcs 置完成。
        private volatile bool _stopRequested;

        private readonly uint _clientId;
        private readonly Server _server;
        private readonly TransportServerBase _transport;
        private readonly SessionSendPump _pump;
        private readonly Channel<Package> _outgoing;
        private readonly int _flushBytesThreshold;
        private readonly CancellationTokenSource _stopCts;
        private readonly TaskCompletionSource<bool> _drainedTcs;

        public int QueueCount => _outgoing.Reader.Count;

        public SessionSender(uint clientId, Server server, TransportServerBase transport, SessionSendPump pump,
                             int capacity = DefaultCapacity,
                             int flushBytesThreshold = DefaultFlushBytesThreshold)
        {
            _clientId = clientId;
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _pump = pump ?? throw new ArgumentNullException(nameof(pump));
            _flushBytesThreshold = Math.Max(1, flushBytesThreshold);

            _outgoing = Channel.CreateBounded<Package>(new BoundedChannelOptions(capacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
            _stopCts = new CancellationTokenSource();
            _drainedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// 把自己投进 pump 就绪队列（去重）。入队/归还后若仍有待发数据都会调它。
        /// </summary>
        private void Schedule()
        {
            if (Interlocked.Exchange(ref _scheduled, 1) == 0)
            {
                _pump.Schedule(this);
            }
        }

        /// <summary>
        /// 停止发送器：先 Complete writer 让残留被 pump drain 完，<paramref name="drainTimeout"/> 内
        /// 等 drain 完成；超时则置 abort 让 pump 丢弃残留并唤醒可能在等位的 <see cref="WriteSlowAsync"/>。
        /// </summary>
        public async Task StopAsync(TimeSpan drainTimeout)
        {
            _stopRequested = true;
            _outgoing.Writer.TryComplete();
            // 确保 pump 至少再 serve 一次：drain 干净后 OnServeCompleted 会把 _drainedTcs 置完成。
            Schedule();

            try
            {
                if (drainTimeout <= TimeSpan.Zero)
                {
                    Volatile.Write(ref _abort, 1);
                    return;
                }

                var completed = await Task.WhenAny(_drainedTcs.Task, Task.Delay(drainTimeout)).ConfigureAwait(false);
                if (completed != _drainedTcs.Task)
                {
                    Volatile.Write(ref _abort, 1);
                    SafeCancelStop();
                    Schedule(); // 踢一脚让 pump 走 abort 分支收尾
                }
            }
            catch (OperationCanceledException) { /* IGNORE */ }
            catch (AggregateException err) when (err.InnerException is OperationCanceledException) { /* IGNORE */ }
            finally
            {
                SafeCancelStop();
                _stopCts.Dispose();
            }
        }

        /// <summary>
        /// 由 <see cref="Server.Send"/> / Kick 等路径调用。
        /// Channel 已满则异步等位，避免在调度器线程上同步阻塞。
        /// </summary>
        public bool Enqueue(Package pack)
        {
            if (_outgoing.Writer.TryWrite(pack))
            {
                Schedule();
                return true;
            }

            // 满了：异步等位，保留 back-pressure 语义
            _ = WriteSlowAsync(pack);
            return false;
        }

        /// <summary>
        /// 枚举当前 Channel 中缓存的包（快照，供监控/诊断）。
        /// 注意此方法会移除 channel 中的包，仅在监控/停机路径上使用。
        /// </summary>
        public List<Package> DrainSnapshot()
        {
            var list = new List<Package>();
            while (_outgoing.Reader.TryRead(out var p)) list.Add(p);
            return list;
        }

        internal void ReportError(Exception err)
        {
            _server.OnErrorEvent(_clientId, err);
        }

        private async Task WriteSlowAsync(Package pack)
        {
            try
            {
                await _outgoing.Writer.WriteAsync(pack, _stopCts.Token).ConfigureAwait(false);
                Schedule();
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (ChannelClosedException) { /* writer 已 complete，停机中，丢弃 */ }
            catch (Exception err)
            {
                _server.OnErrorEvent(_clientId, err);
            }
        }

        /// <summary>
        /// 一个 serve turn：由 pump worker 调用，把**当前已缓冲**的包按 flush 阈值分批发出，
        /// drain 到瞬时空为止即返回。turn 之间靠就绪队列轮转，保证公平、不饿死其它 sender。
        /// <para>
        /// <paramref name="writer"/> / <paramref name="postSend"/> 是 worker 复用的暂存，进入时假定干净，
        /// 返回时也保持干净（异常路径由 worker 兜底 reset）。
        /// </para>
        /// </summary>
        internal async ValueTask ServeAsync(ArrayBufferWriter<byte> writer, List<Package> postSend, CancellationToken pumpToken)
        {
            var reader = _outgoing.Reader;

            while (!pumpToken.IsCancellationRequested)
            {
                if (Volatile.Read(ref _abort) != 0)
                {
                    // 丢弃残留，尽快收尾。
                    while (reader.TryRead(out _)) { }
                    return;
                }

                // 聚合：把当前可读的包尽量一次写入 buffer，直到达到 flush 阈值或瞬时读空。
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

                    if (pack.IsLastChunk) postSend.Add(pack);

                    if (writer.WrittenCount >= _flushBytesThreshold) break;
                }

                if (writer.WrittenCount == 0) return; // 当前已无可发数据，让位

                try
                {
                    await _transport.SendAsync(_clientId, writer.WrittenMemory, pumpToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }
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
                for (var i = 0; i < postSend.Count; i++)
                {
                    try { _server.PostSendFilter(postSend[i]); }
                    catch (Exception err) { _server.OnErrorEvent(_clientId, err); }
                }
                postSend.Clear();
            }
        }

        /// <summary>
        /// 由 pump worker 在一个 serve turn 结束后调用：归还单飞标志，并处理
        /// "serve 与归还之间新到的包"以及"停机 drain 完成"两种收尾。
        /// </summary>
        internal void OnServeCompleted()
        {
            Volatile.Write(ref _scheduled, 0);

            if (Volatile.Read(ref _abort) != 0)
            {
                _drainedTcs.TrySetResult(true);
                return;
            }

            // serve 与归还之间可能有新包到达（那次 Enqueue 看到 _scheduled==1 而跳过了入队）：重新调度。
            if (_outgoing.Reader.Count > 0)
            {
                Schedule();
                return;
            }

            // 已请求停止且已 drain 干净：通知 StopAsync 完成。
            if (_stopRequested)
            {
                _drainedTcs.TrySetResult(true);
            }
        }

        private void SafeCancelStop()
        {
            try { _stopCts.Cancel(); }
            catch (ObjectDisposedException) { /* 已销毁 */ }
        }
    }
}
