# ProcessorTplBase TPL DataFlow 实现问题分析

## 📊 问题总览

当前的 `ProcessorTplBase` 实现存在多个严重问题，导致它**没有达到"每个Processor一个线程"的设计目标**。

---

## 🔴 严重问题

### 1. 多线程问题 - 每个 Processor 实际使用 6+ 个线程

**问题描述：**
```csharp
m_packageBlock = new ActionBlock<DataFlowItemBase>(ResolvePackage);
m_broadcastBlock = new ActionBlock<DataFlowItemBase>(ResolveBroadcast);
m_updateBlock = new ActionBlock<DataFlowItemBase>(ResolveUpdate);
m_delayBlock = new ActionBlock<DataFlowItemBase>(ResolveDelayCall);
m_deferBlock = new ActionBlock<DataFlowItemBase>(ResolveDeferCall);
m_fallbackBlock = new ActionBlock<DataFlowItemBase>(ResolveFallback);
```

每个 `ActionBlock` 默认都会从 `TaskScheduler.Default` 获取线程执行。这意味着：
- **预期**：1 个 Processor = 1 个线程
- **实际**：1 个 Processor = 6 个 ActionBlock = 最多 6 个并发线程

**影响：**
- 违背设计初衷
- 可能导致竞态条件（虽然有 BufferBlock 路由，但不同类型消息可能并发执行）
- 资源浪费

**解决方案：**
所有 ActionBlock 应该配置使用同一个单线程 TaskScheduler：
```csharp
var singleThreadScheduler = new ConcurrentExclusiveSchedulerPair(
    TaskScheduler.Default, 
    maxConcurrencyLevel: 1
).ConcurrentScheduler;

var options = new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = 1,
    TaskScheduler = singleThreadScheduler,
    // ...
};
```

或者更简单的方案：**使用单个 ActionBlock 处理所有类型的消息**（见改进版）。

---

### 2. BufferBlock 容量问题和消息丢失风险

**问题代码：**
```csharp
m_bufferBlock.Post(new PackageItem { Package = packRaw });
```

`Post()` 方法返回 `bool`，但代码没有检查返回值：
- `BoundedCapacity = ushort.MaxValue` (65535)
- 当队列满时，`Post()` 返回 `false`，消息被丢弃
- 高负载场景下可能静默丢失消息

**解决方案：**
1. 检查 `Post()` 返回值
2. 使用 `SendAsync()` 并等待（会阻塞直到有空间）
3. 实现重试机制
4. 记录丢失的消息

```csharp
protected virtual async Task<bool> PostWithRetry(DataFlowItemBase item, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        if (m_bufferBlock.Post(item)) return true;
        await Task.Delay(10 * (i + 1)); // 指数退避
    }
    Server.OnErrorEvent(IdLoopGenerator.INVALID, 
        new Exception($"Failed to post message after {maxRetries} retries"));
    return false;
}
```

---

### 3. DelayCall 实现的严重问题

**问题代码：**
```csharp
protected ConcurrentDictionary<Func<Task>, DateTime> m_delayTasksThreadSafe;

public override void DelayCall(TimeSpan delay, Func<Task> func)
{
    var time = DateTime.UtcNow.Add(delay);
    m_delayTasksThreadSafe.AddOrUpdate(func, time, (_, _) => time);
}
```

**问题列表：**

#### 3.1 Lambda 作为 Key 的问题
```csharp
DelayCall(TimeSpan.FromSeconds(1), async () => Console.WriteLine("A"));
DelayCall(TimeSpan.FromSeconds(1), async () => Console.WriteLine("A"));
```
这两个调用会创建**两个不同的 Func 实例**，即使逻辑相同也无法去重。

#### 3.2 内存泄漏
```csharp
protected virtual async Task ResolveDelayCall(DataFlowItemBase arg)
{
    var data = arg as DelayCallItem;
    m_delayTasksThreadSafe.TryRemove(data.Action, out _);
    await data.Action.Invoke();
}
```
如果 `data.Action.Invoke()` 抛出异常，后续的 `TryRemove` 不会执行，导致：
- 任务永远留在字典中
- 内存泄漏
- `DelayTasks` 属性返回错误的数据

#### 3.3 竞态条件
`Server.Timer.cs` 中的代码：
```csharp
foreach (var (execTime, action) in processor.DelayTasks)
{
    if (execTime <= DateTime.UtcNow)
    {
        processor.OnDelayCallReceived(execTime, action);
    }
}
```
在枚举 `DelayTasks` 时，`m_delayTasksThreadSafe` 可能被其他线程修改，导致：
- `OnDelayCallReceived` 中通过 action 找不到对应的 key
- 任务无法正确移除

**解决方案：**
使用 `Guid` 作为 Key：
```csharp
protected ConcurrentDictionary<Guid, (DateTime, Func<Task>)> m_delayTasksThreadSafe;

public override void DelayCall(TimeSpan delay, Func<Task> func)
{
    var id = Guid.NewGuid();
    var time = DateTime.UtcNow.Add(delay);
    m_delayTasksThreadSafe.TryAdd(id, (time, func));
}
```

并在 `DelayCallItem` 中包含 Guid：
```csharp
public class DelayCallItem : DataFlowItemBase
{
    public Guid Id;
    public DateTime ExecuteTime; 
    public Func<Task> Action;
}
```

---

### 4. PropagateCompletion 设置不当

**问题代码：**
```csharp
m_bufferBlock.LinkTo(m_packageBlock, new DataflowLinkOptions
{
    PropagateCompletion = true,
}, data => data is PackageItem);
```

所有 6 个 LinkTo 都设置了 `PropagateCompletion = true`，这会导致：
- 任何一个 ActionBlock 完成，BufferBlock 也会完成
- BufferBlock 完成后，其他 ActionBlock 也会完成
- 可能导致意外的提前终止

**TPL DataFlow 最佳实践：**
- 只在确定要传播完成状态时设置为 `true`
- 通常在线性 pipeline 中使用
- 在分支场景（一个 source 链接到多个 target）中应该手动控制完成

**解决方案：**
```csharp
m_bufferBlock.LinkTo(m_packageBlock, new DataflowLinkOptions
{
    PropagateCompletion = false, // 手动控制完成
}, data => data is PackageItem);
```

并在 `StopThread()` 中手动完成所有 block：
```csharp
m_bufferBlock.Complete();
await m_bufferBlock.Completion;
m_packageBlock.Complete();
await m_packageBlock.Completion;
// ... 其他 blocks
```

---

### 5. 缺少 Server.Update() 和 Server.ResolveBroadcast() 调用

**对比 ProcessorBase：**
```csharp
// ProcessorBase.cs
public virtual bool PackageLoopFrame(...)
{
    Server.Update(this).Wait(cancelToken);
    Server.ResolveBroadCast(this, broadcastQueue).Wait(cancelToken);
    DoDeferCalls().Wait(cancelToken);
    DoDelayCalls().Wait(cancelToken);
    // ...
}
```

**ProcessorTplBase 中缺失：**
- `Server.Update(this)` 调用 - 这个方法检查是否需要触发 Update
- `Server.ResolveBroadCast(this, broadcastQueue)` - 处理广播队列
- 这些逻辑被内联到各个 Resolve 方法中，但**可能不一致**

**影响：**
- Update 时机可能不准确
- 错误处理可能不一致
- 与 ProcessorBase 行为不一致

---

## ⚠️ 次要问题

### 6. 异常处理过于宽泛

```csharp
catch (AggregateException err)
{
    if (err.InnerException is OperationCanceledException) return;
    if (err.InnerException is TaskCanceledException) return;
    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
}
catch (Exception err)
{
    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
}
```

问题：
- 所有异常都被捕获并记录，但处理继续
- 某些严重错误（如 OutOfMemoryException）应该终止处理
- 没有区分可恢复和不可恢复的错误

建议：
```csharp
catch (OperationCanceledException)
{
    // 正常取消，不记录
}
catch (Exception err) when (err is not OutOfMemoryException && 
                             err is not StackOverflowException)
{
    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
}
// 让致命异常向上传播
```

---

### 7. Update 机制效率问题

每次 Update 都要：
1. `Server.Timer` 调用 `OnUpdateReceived()`
2. 创建 `UpdateItem` 对象
3. Post 到 BufferBlock
4. BufferBlock 路由到 updateBlock
5. updateBlock 执行

**开销：**
- 对象分配
- 线程切换
- 队列操作

**对比 ProcessorBase：**
直接在同一线程中调用 `IUpdate.OnUpdate()`，没有额外开销。

**建议：**
如果 Update 频繁且轻量，考虑：
- 使用专用的 Timer
- 或者在 ActionBlock 的处理循环中检查 Update 时间

---

### 8. GetStatus() 不准确

```csharp
public override ProcessorStatus GetStatus()
{
    return new ProcessorStatus
    {
        Name = GetName(),
        Status = TaskStatus.Running, // 总是返回 Running？
        PackageQueueCount = m_bufferBlock?.Count ?? 0,
        BroadcastQueueCount = 0, // 应该从 BufferBlock 统计
    };
}
```

问题：
- `Status` 总是返回 `Running`
- `BroadcastQueueCount` 总是 0（实际在 BufferBlock 中）
- 无法反映真实状态

---

## 📋 TPL DataFlow 最佳实践总结

### ✅ 应该做的

1. **明确线程模型**
   ```csharp
   var options = new ExecutionDataflowBlockOptions
   {
       MaxDegreeOfParallelism = 1, // 单线程
       TaskScheduler = customScheduler, // 专用 scheduler
   };
   ```

2. **处理背压**
   ```csharp
   if (!block.Post(item))
   {
       await block.SendAsync(item); // 或实现重试
   }
   ```

3. **正确管理生命周期**
   ```csharp
   // 停止时
   block.Complete();
   await block.Completion;
   ```

4. **使用合适的容量**
   ```csharp
   BoundedCapacity = DataflowBlockOptions.Unbounded // 或合理的上限
   ```

5. **正确处理取消**
   ```csharp
   var cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
   var options = new DataflowBlockOptions
   {
       CancellationToken = cts.Token
   };
   ```

### ❌ 不应该做的

1. **不要为每个消息类型创建单独的 ActionBlock**（除非需要并行）
2. **不要忽略 Post() 的返回值**
3. **不要在高并发场景使用 PropagateCompletion = true 的分支**
4. **不要使用非唯一的 Key（如 Func<Task>）**
5. **不要在异常处理后继续而不检查状态**

---

## 🎯 推荐的架构设计

### 方案 A：单 ActionBlock（推荐）

```csharp
// 单个 ActionBlock 处理所有类型
var actionBlock = new ActionBlock<DataFlowItemBase>(
    async item => await ProcessItem(item),
    new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = 1,
        BoundedCapacity = 1,
    }
);

m_bufferBlock.LinkTo(actionBlock);
```

**优点：**
- 真正的单线程
- 简单明了
- 消息按到达顺序处理

**缺点：**
- 失去了类型过滤的性能优化

---

### 方案 B：共享 TaskScheduler 的多 ActionBlock

```csharp
var scheduler = new ConcurrentExclusiveSchedulerPair(
    TaskScheduler.Default, 1
).ConcurrentScheduler;

var options = new ExecutionDataflowBlockOptions
{
    TaskScheduler = scheduler,
    MaxDegreeOfParallelism = 1,
};

m_packageBlock = new ActionBlock<DataFlowItemBase>(ResolvePackage, options);
m_broadcastBlock = new ActionBlock<DataFlowItemBase>(ResolveBroadcast, options);
// ...
```

**优点：**
- 类型过滤优化
- 仍然是单线程执行

**缺点：**
- 更复杂
- 需要正确管理 TaskScheduler 生命周期

---

### 方案 C：混合方案（当前 ProcessorBase）

保持当前的 `BlockingCollection` + 单线程循环：

**优点：**
- 简单、可靠
- 已经验证
- 完全控制执行顺序

**缺点：**
- 不使用 TPL DataFlow 的高级特性
- 手动管理队列

---

## 💡 建议

### 短期（修复当前实现）
1. **立即修复 DelayCall 的 Key 问题**（严重bug）
2. **添加 Post 返回值检查**（防止消息丢失）
3. **修复 PropagateCompletion 设置**
4. **添加单元测试验证单线程行为**

### 长期（重新设计）
1. **评估是否真的需要 TPL DataFlow**
   - 如果只是为了单线程处理，`BlockingCollection` 已经足够
   - TPL DataFlow 适合复杂的 pipeline 和并行处理

2. **如果保留 TPL DataFlow，采用方案 A（单 ActionBlock）**
   - 简单、可靠
   - 符合设计目标

3. **考虑性能测试**
   - 对比 ProcessorBase vs ProcessorTplBase
   - 在高负载下测试

---

## 📚 参考资源

- [TPL Dataflow (Task Parallel Library)](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library)
- [ConcurrentExclusiveSchedulerPair](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.concurrentexclusiveschedulerpair)
- [Best Practices for Dataflow](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library#best-practices)

---

## 🔧 测试建议

创建单元测试验证：
1. 所有消息在同一线程执行（记录 Thread.CurrentThread.ManagedThreadId）
2. 消息按顺序处理
3. DelayCall 正确执行和清理
4. 高负载下不丢失消息
5. 正确的停止和清理

```csharp
[Fact]
public async Task ProcessorTplBase_Should_Use_Single_Thread()
{
    var threadIds = new ConcurrentBag<int>();
    var processor = new TestProcessor();
    
    // 发送多个消息
    for (int i = 0; i < 100; i++)
    {
        processor.OnPackageReceived(CreateTestPackage());
    }
    
    await Task.Delay(1000); // 等待处理
    
    // 验证只有一个线程
    Assert.Single(threadIds.Distinct());
}
```
