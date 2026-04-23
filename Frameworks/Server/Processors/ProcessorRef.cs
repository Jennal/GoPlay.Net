using System;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Processors
{
    /// <summary>
    /// Actor 风格的跨 Processor 调用句柄：封装一个目标 Processor 的裸对象 + 它的 <see cref="ProcessorRunner"/>，
    /// 外部只能通过 <see cref="Request{TResult}"/> / <see cref="Request"/> / <see cref="Notify"/> 三个 API
    /// 把 <c>Func&lt;T, Task&gt;</c> 闭包投递到目标 Runner 的邮箱串行执行。
    ///
    /// 语义：
    /// - <c>Request</c>：RPC 语义，等待完成并传回结果 / 异常（异常经 awaiter 抛出）。
    /// - <c>Notify</c>：fire-and-forget，异常就地走 <c>Server.OnErrorEvent</c>，永不逃逸。
    ///
    /// 回环策略：调用方所在 Runner 与目标 Runner 相同（即自己调自己）时，<c>Request</c> 直接内联执行
    /// <c>fn(Target)</c>，避免 mailbox 自等死锁；<c>Notify</c> 仍入队，保持"排队到当前工作之后"的语义。
    ///
    /// 设计为只读 struct，从 <see cref="Server.GetProcessor{TP}"/> 返回时不产生堆分配。
    /// </summary>
    public readonly struct ProcessorRef<T> where T : ProcessorBase
    {
        internal readonly T Target;
        internal readonly ProcessorRunner Runner;

        internal ProcessorRef(T target, ProcessorRunner runner)
        {
            Target = target;
            Runner = runner;
        }

        /// <summary>
        /// 当前 Ref 是否绑定到有效的 Processor（<c>default(ProcessorRef&lt;T&gt;)</c> 返回 false）。
        /// </summary>
        public bool IsValid => Target != null && Runner != null;

        /// <summary>
        /// 把闭包投递到目标 Runner 邮箱，等待执行结果。
        /// 异常经 <see cref="Task{TResult}"/> 的 awaiter 重新抛出。
        /// </summary>
        public Task<TResult> Request<TResult>(Func<T, Task<TResult>> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            // 回环 inline：同一 Runner 上的再入调用直接执行，避免自死锁
            if (ProcessorRunner.Current == Runner) return fn(Target);

            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = Target;
            Runner.Post(async () =>
            {
                try
                {
                    var result = await fn(target).ConfigureAwait(false);
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// 把闭包投递到目标 Runner 邮箱，等待执行完成（无返回值）。
        /// 异常经 <see cref="Task"/> 的 awaiter 重新抛出。
        /// </summary>
        public Task Request(Func<T, Task> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            if (ProcessorRunner.Current == Runner) return fn(Target);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = Target;
            Runner.Post(async () =>
            {
                try
                {
                    await fn(target).ConfigureAwait(false);
                    tcs.TrySetResult(true);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// fire-and-forget：把闭包投递到目标 Runner 邮箱，不等待、不返回 Task。
        /// 任何异常就地走 <see cref="Server.OnErrorEvent"/>，不会逃逸到调用方。
        ///
        /// 即使 <c>ProcessorRunner.Current == Runner</c>（自己调自己）也入队，
        /// 保持和 <c>DeferCall</c> 一致的"排队到当前工作之后"语义。
        /// </summary>
        public void Notify(Func<T, Task> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            var runner = Runner;
            var target = Target;
            runner.Post(async () =>
            {
                try
                {
                    await fn(target).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception err)
                {
                    runner.Server?.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            });
        }

        /// <summary>
        /// 带 <paramref name="routeKey"/> 的 <c>Request</c> 重载：把闭包投递到目标 Runner 邮箱串行执行，
        /// 同时让 Runner 在邮箱分派阶段按 <paramref name="routeKey"/> 查到对应 <see cref="Routers.Route"/>
        /// 的方法级 <see cref="System.Threading.SemaphoreSlim"/>，让跨 Processor 调用也遵守
        /// 方法级 <c>[MaxConcurrency]</c> 限流，和客户端 <c>[Request]</c> 路径共享同一把 sem。
        /// <para>
        /// <paramref name="routeKey"/> 应和 <see cref="Routers.Route.RouteString"/> 完全一致
        /// （<c>"{processorName}.{methodName}".ToLower()</c>）。
        /// 此重载主要由 <c>[ProcessorApi]</c> 驱动的 Source Generator 调用，业务代码一般不直接用。
        /// 传 null 或找不到对应 Route 时行为等价于不带 <paramref name="routeKey"/> 的重载。
        /// </para>
        /// </summary>
        public Task<TResult> Request<TResult>(string routeKey, Func<T, Task<TResult>> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            if (ProcessorRunner.Current == Runner) return fn(Target);

            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = Target;
            Runner.Post(async () =>
            {
                try
                {
                    var result = await fn(target).ConfigureAwait(false);
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }, routeKey);
            return tcs.Task;
        }

        /// <summary>
        /// 带 <paramref name="routeKey"/> 的无返回值 <c>Request</c> 重载，语义见
        /// <see cref="Request{TResult}(string, Func{T, Task{TResult}})"/>。
        /// </summary>
        public Task Request(string routeKey, Func<T, Task> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            if (ProcessorRunner.Current == Runner) return fn(Target);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = Target;
            Runner.Post(async () =>
            {
                try
                {
                    await fn(target).ConfigureAwait(false);
                    tcs.TrySetResult(true);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }, routeKey);
            return tcs.Task;
        }

        /// <summary>
        /// 带 <paramref name="routeKey"/> 的 <c>Notify</c> 重载。
        /// 语义：fire-and-forget，异常就地走 <c>Server.OnErrorEvent</c>；
        /// 并和 <see cref="Request{TResult}(string, Func{T, Task{TResult}})"/> 一样在邮箱分派阶段
        /// 按 <paramref name="routeKey"/> 解析方法级 sem，和客户端路径共享限流。
        /// </summary>
        public void Notify(string routeKey, Func<T, Task> fn)
        {
            EnsureValid();
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            var runner = Runner;
            var target = Target;
            runner.Post(async () =>
            {
                try
                {
                    await fn(target).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception err)
                {
                    runner.Server?.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            }, routeKey);
        }

        private void EnsureValid()
        {
            if (Target == null || Runner == null)
            {
                throw new InvalidOperationException(
                    $"ProcessorRef<{typeof(T).Name}> 未绑定：目标 Processor 未注册或 Runner 未启动。" +
                    " 检查 Server.Register 与 Server.GetProcessor 的调用时机。");
            }
        }
    }
}
