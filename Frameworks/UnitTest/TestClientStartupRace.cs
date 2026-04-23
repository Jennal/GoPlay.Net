using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;
using NUnit.Framework;

namespace UnitTest
{
    /// <summary>
    /// 缺陷 3（Connect 启动序列竞态）的独立回归测试。
    ///
    /// <para>场景：真实环境里缺陷 3 的具体表现是 "底层已连通，server 已把 HandshakeResp
    /// 放回网络 → IOCP 回调在 Client 线程池任务（SendLoop/RecvLoop）尚未运行之前触发"。
    /// 真连接下这个时间窗极小且被底层 socket buffer 掩盖，难以复现。
    /// 这里用 <see cref="FakeTransport"/> 把整个窗口"放大"：</para>
    ///
    /// <para>
    /// 1. Connect 调用 <c>Transport.ConnectAsync</c> → FakeTransport 立刻 IsConnected=true 返回。<br/>
    /// 2. Connect 进入 "启动 loop + ready 屏障" 阶段。<br/>
    /// 3. Connect 调用 <c>SendHandShake()</c> → 最终走 <c>FakeTransport.Send(ReadOnlyMemory)</c>。<br/>
    /// 4. FakeTransport 在 Send 被调那一刻检查：
    ///    <list type="bullet">
    ///      <item>span handler 是否已绑定（<see cref="TransportClientBase.SetDataReceivedSpanHandler"/> 已被 Connect 调用）</item>
    ///      <item>也就是说 Client 端 push 路径已就绪，server HandshakeResp 一到就能被消费</item>
    ///    </list>
    /// 5. FakeTransport 立即回 push 一份合成的 HandshakeResp，让 Client.Connect 返回 true。
    /// </para>
    ///
    /// <para>断言失败就意味着缺陷 3 回归：Client 在 push 路径就绪前就发了 HandshakeReq，
    /// 真实环境下 HandshakeResp 到达时会被 silently dropped。</para>
    /// </summary>
    public class TestClientStartupRace
    {
        /// <summary>
        /// Push 语义的 FakeTransport，完全跑在内存里，没有实际 socket。
        /// 对 <see cref="TestClientStartupRace"/> 必须有 parameterless 构造器
        /// （<c>Client&lt;T&gt;</c> 的 <c>where T : TransportClientBase, new()</c>）。
        /// </summary>
        public class FakeTransport : TransportClientBase
        {
            public override bool SupportPush => true;

            // 屏障原语：Client 调 SetDataReceivedSpanHandler(非 null) 的那一瞬 Set。
            // Test 主线程也可从这里直接查是否已完成。
            public static volatile bool HandlerAttachedAtSendTime;
            public static volatile bool SendObservedHandshakeReq;
            public static Exception? AssertionError;

            private bool m_connected;

            public override bool IsConnected => m_connected;

            // 同步版本走 busy spin 更贴近真 transport；这里直接同步置位即可。
            public override void Connect(string host, int port, TimeSpan timeout)
            {
                m_connected = true;
                InvokeOnConnected();
            }

            public override Task ConnectAsync(string host, int port, TimeSpan timeout)
            {
                // 不用 Task.Run：保持同步 path，避免引入无关的调度延迟到测试里。
                m_connected = true;
                InvokeOnConnected();
                return Task.CompletedTask;
            }

            public override void Disconnect()
            {
                m_connected = false;
                InvokeOnDisconnected();
            }

            public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
            {
                throw new NotSupportedException("FakeTransport is push-only");
            }

            public override ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
            {
                // Client 走 Zero-copy Send(ReadOnlyMemory) 路径，不会调这里
                throw new NotSupportedException("FakeTransport: byte[] Send path not used");
            }

            /// <summary>
            /// 完整 wire frame（含 outer ushort 长度前缀）到达的那一刻：
            /// 1. 按 Span 抽头判断是不是 HandshakeReq；
            /// 2. 断言此时 span handler 已被 attach（即 Connect 已正确穿过 push 就绪屏障）；
            /// 3. 合成一份 HandshakeResp push 回 Client（inner bytes，无 outer 前缀）。
            /// </summary>
            public override ValueTask Send(ReadOnlyMemory<byte> data, CancellationTokenSource cancelSource)
            {
                var inner = data.Span.Slice(sizeof(ushort));
                var pack = Package.ParseRaw(inner);

                if (pack.Header.PackageInfo.Type != PackageType.HankShakeReq)
                {
                    // 非 handshake 的 Send 忽略（比如测试意外时机触发的 Ping）
                    return default;
                }

                // 抓拍 Send 被调时 push handler 的真实绑定状态。用 base 的探针属性，
                // 不依赖 SetDataReceivedSpanHandler 的覆写（base 方法不是 virtual，Client<T>
                // 里持有的是基类静态绑定的调用点，new 覆写不会生效）。
                HandlerAttachedAtSendTime = HasDataReceivedSpanHandler;
                SendObservedHandshakeReq = true;

                // 核心断言（缺陷 3 回归点）：必须能感知到 push handler 已就绪，
                // 否则下面那一份 HandshakeResp 会被 silent drop。
                // 用 field 记录而非 Assert.IsTrue：Send 在 LongRun 线程上，
                // 断言失败的 AssertionException 会被 Send 调用方的 try/catch 吞掉 → 测试看不到真正原因。
                if (!HandlerAttachedAtSendTime)
                {
                    AssertionError = new Exception(
                        "缺陷 3 回归：Client 发 HandshakeReq 时 span handler 尚未 attach，" +
                        "真实环境下 server HandshakeResp 会被 silent drop。");
                    return default;
                }

                // 合成 HandshakeResp push 回 Client
                var resp = new RespHandShake
                {
                    ServerVersion = "FakeTransport/0.1",
                    HeartBeatInterval = 30000,
                    Routes = { },
                };
                var respPack = Package.Create(0, resp, PackageType.HankShakeResp, EncodingType.Protobuf);

                var writer = new ArrayBufferWriter<byte>();
                respPack.WriteTo(writer);
                // WriteTo 写入 [outerLen][headerLen][header][body]；push 路径期望 inner（去 outer）。
                var respInner = writer.WrittenMemory.Slice(sizeof(ushort));

                InvokeOnDataReceivedSpan(respInner.Span);
                return default;
            }

            public override void Dispose()
            {
                m_connected = false;
            }
        }

        [SetUp]
        public void Reset()
        {
            FakeTransport.HandlerAttachedAtSendTime = false;
            FakeTransport.SendObservedHandshakeReq = false;
            FakeTransport.AssertionError = null;
        }

        /// <summary>
        /// 主断言：Connect 必须先绑定 span handler、再发 HandshakeReq。
        /// 这条保证在 Connect 返回 true 且无内部 AssertionError 时成立。
        /// </summary>
        [Test]
        public async Task HandshakeReqEmittedAfterSpanHandlerAttached()
        {
            var client = new Client<FakeTransport>();
            client.RequestTimeout = TimeSpan.FromSeconds(3);

            var ok = await client.Connect("fake", 0);

            Assert.IsNull(FakeTransport.AssertionError, FakeTransport.AssertionError?.Message);
            Assert.IsTrue(FakeTransport.SendObservedHandshakeReq,
                "FakeTransport 未观察到 HandshakeReq：Client 发送管线没有跑通");
            Assert.IsTrue(FakeTransport.HandlerAttachedAtSendTime,
                "span handler 从未被 attach：Client.Connect 的 push 路径分支可能被误关");
            Assert.IsTrue(ok, "Connect 返回 false：HandshakeResp 未被 Client 侧消费，push 路径未就绪");

            await client.DisconnectAsync();
        }

        /// <summary>
        /// 压力版本：重复 Connect/Disconnect 多次，任何一次失败都意味着启动时序有抖动窗口。
        /// </summary>
        [Test]
        public async Task RepeatedConnectKeepsHandshakeReqAfterReady()
        {
            for (var i = 0; i < 20; i++)
            {
                FakeTransport.HandlerAttachedAtSendTime = false;
                FakeTransport.SendObservedHandshakeReq = false;
                FakeTransport.AssertionError = null;

                var client = new Client<FakeTransport>();
                client.RequestTimeout = TimeSpan.FromSeconds(3);

                var ok = await client.Connect("fake", 0);
                Assert.IsNull(FakeTransport.AssertionError,
                    $"iteration {i}: {FakeTransport.AssertionError?.Message}");
                Assert.IsTrue(ok, $"iteration {i}: Connect returned false");

                await client.DisconnectAsync();
            }
        }
    }
}
