using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;

namespace GoPlay.Core.Senders
{
    /// <summary>
    /// 共享发送调度器（M:N 模型）：用**固定 N 条** pump 线程服务**任意多个** <see cref="SessionSender"/>，
    /// 取代"每连接一条 LongRunning 线程"的旧模型。
    ///
    /// <para>
    /// 设计要点：
    /// <list type="bullet">
    ///   <item>每个 <see cref="SessionSender"/> 仍保留自己的有界 Channel，**同 client 内严格保序**。</item>
    ///   <item>sender 的 channel 由空变非空时把自己投进 <see cref="_ready"/> 就绪队列；pump worker 取出后
    ///         drain 当前积压并发出，再交还。靠 <see cref="SessionSender"/> 内的单飞标志保证
    ///         **同一 sender 同一时刻只被一条 worker 处理**，顺序与互斥都不破。</item>
    ///   <item>一个慢 client 最多占用一条 worker 一个 serve turn，drain 完当前积压即让位，**不会饿死其它 client**。</item>
    ///   <item>线程数 = O(worker)，与连接数无关：几千上万连接也只占几十条线程，彻底消除 thread-per-connection。</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// worker 用 <see cref="TaskUtil.LongRun"/> 常驻专用线程：这样新连接的 handshake 响应一旦入队，
    /// 就绪队列里立刻有 worker 取走发出，不经 ThreadPool 排队——既解决了当年改 LongRun 想解决的
    /// "handshake 排队 >10s"，又不再为此付出每连接一条线程的代价。
    /// </para>
    /// </summary>
    internal sealed class SessionSendPump
    {
        private readonly Channel<SessionSender> _ready;
        private readonly int _workerCount;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task[] _workers;
        private int _started;

        public CancellationToken Token => _cts.Token;

        public SessionSendPump(int workerCount)
        {
            _workerCount = Math.Max(1, workerCount);
            _ready = Channel.CreateUnbounded<SessionSender>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
        }

        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1) return;

            _workers = new Task[_workerCount];
            for (var i = 0; i < _workerCount; i++)
            {
                _workers[i] = TaskUtil.LongRun(
                    () => WorkerLoop(_cts.Token).GetAwaiter().GetResult(),
                    _cts.Token);
            }
        }

        /// <summary>
        /// 把一个有待发数据的 sender 投进就绪队列。由 <see cref="SessionSender.Schedule"/> 调用，
        /// 已通过单飞标志去重，这里不会重复入队。
        /// </summary>
        public void Schedule(SessionSender sender)
        {
            // Stop 后 channel 被 complete，TryWrite 返回 false：此时丢弃即可（停机路径无需再发）。
            _ready.Writer.TryWrite(sender);
        }

        /// <summary>
        /// 停止调度器：取消 token + complete 就绪队列，worker 自然退出。
        /// 由 <see cref="Server"/> 在所有 sender 都已 StopAsync 之后调用。
        /// </summary>
        public void Stop()
        {
            try { _cts.Cancel(); } catch (ObjectDisposedException) { /* IGNORE */ }
            _ready.Writer.TryComplete();

            if (_workers != null)
            {
                try { Task.WaitAll(_workers, TimeSpan.FromSeconds(2)); }
                catch { /* best-effort：worker 内部已吞掉取消异常 */ }
            }

            try { _cts.Dispose(); } catch (ObjectDisposedException) { /* IGNORE */ }
        }

        private async Task WorkerLoop(CancellationToken token)
        {
            var reader = _ready.Reader;
            // 每条 worker 复用一份发送缓冲与 post-send 暂存，避免每个 serve turn 重新分配（GC 友好）。
            var writer = new ArrayBufferWriter<byte>(SessionSender.DefaultFlushBytesThreshold);
            var postSend = new List<Package>();

            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var sender))
                    {
                        try
                        {
                            await sender.ServeAsync(writer, postSend, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { /* 停机 */ }
                        catch (Exception err)
                        {
                            sender.ReportError(err);
                        }
                        finally
                        {
                            ResetScratch(writer, postSend);
                            // 归还单飞标志；若 serve 与归还之间又有新包到达，OnServeCompleted 会重新入队。
                            sender.OnServeCompleted();
                        }
                    }
                }
            }
            catch (OperationCanceledException) { /* 停机 */ }
        }

        private static void ResetScratch(ArrayBufferWriter<byte> writer, List<Package> postSend)
        {
            if (writer.WrittenCount > 0)
            {
#if NET8_0_OR_GREATER
                writer.ResetWrittenCount();
#else
                writer.Clear();
#endif
            }
            if (postSend.Count > 0) postSend.Clear();
        }
    }
}
