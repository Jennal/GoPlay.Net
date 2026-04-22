namespace GoPlay.Statistics;

public class ProcessorStatus
{
    public string Name;
    public TaskStatus Status;
    public int PackageQueueCount;
    public int BroadcastQueueCount;

    /// <summary>
    /// 广播队列自上次采样窗口以来的峰值深度。
    /// <para>
    /// 单独暴露 Peak 而不是只给 <see cref="BroadcastQueueCount"/>，是为了让外部能捕获
    /// 两次 status 采样之间出现的"瞬时尖峰"——否则仅用瞬时值做监控，窗口内的短暂堆积会被漏掉。
    /// </para>
    /// <para>
    /// 这个值由谁重置取决于调用方如何获取 Status：
    /// <list type="bullet">
    ///   <item>若调用方希望"滑动窗口峰值"，应使用 <see cref="Server.GetProcessorQueueStatus"/>
    ///     的变体 / 组合 <c>SampleAndResetBroadcastPeakDepth</c>，形成"读完即清"语义。</item>
    ///   <item>若调用方只是偶尔看一眼，默认不 reset，此值语义是"自 Runner 启动以来历史峰值"。</item>
    /// </list>
    /// </para>
    /// </summary>
    public int BroadcastPeakDepth;
}