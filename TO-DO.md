# GoPlay Server Framework

## TO-DO

- [] ~~统一 `Client` 和 `Server` 的 `Recv`、`Send` 代码~~
- [x] `SessionManager` 用于管理客户端状态信息
- [] ~~使用Expression创建Delegate并缓存来优化Route执行效率~~
https://stackoverflow.com/questions/840261/passing-arguments-to-c-sharp-generic-new-of-templated-type
- [] Tool的导出ClientExtension，增加Protobuf Data Namespace的自动识别

### Client

- [x] `Connected` 和 `Disconnected` 事件
- [x] `Recv` 线程和 `Send` 线程的异常保护
- [x] 无法连接时的错误处理
- [x] `Recv` 阻塞，导致 `Disconnect` 调用无法结束
- [x] `HeartBeat` 实现
- [x] `AddListener` 改为监听任意包，而不仅仅是 `Push` 包
- [x] 增加 `WaitFor` 方法，相当于 `AddListenerOnce` ，但是利用 `Task<T>` 返回
- [x] `AddListener` 的回调增加 `Package` 和 `Package<T>` 的支持
- [x] `AddListener` 的回调增加 `MainThreadActionRunner`，让回调在主线程中运行
- [x] 增加断线重连的支持

### Server

- [x] `Recv` 线程和 `Send` 线程的异常保护
- [x] host增加`*`的支持
- [x] 新客户端连接事件
- [x] 客户端断线事件
- [x] `Processor` 增加客户端连接和断线的回调
- [x] `Kick` 方法测试
- [x] `Stop` 测试
- [x] `OnRecv` 改为直接调用`Processor`而不是`Router`
- [x] `HeartBeat` 实现

### Cluster

#### Cluster-Master

- [] 必须先启动，用于管理和缓存服务器列表
- [] 其他`Cluster`服务器启动时，都要先连接`Master`服务器，并报告以下的信息
   - 服务器名称
   - 服务器类型
   - 服务器支持的`Route`列表
   - 服务器权重
- [] 连接所有`Cluster`服务器，并确保他们的活跃，如果有服务器失去连接，则更新列表，并推送给列表中的所有活跃服务器
- [] 提供的`Route`
   - 获取服务器列表
   - 推送服务器列表更新

#### Cluster-Gate

- [] 从`Master`获取服务器列表
- [] 提供的Route
   - 根据权重分配`Connector`的IP和端口

#### Cluster-Connector

- [] `Session` 数据缓存
   - 当客户端连接时，建立缓存
   - 当客户端失去连接时，删除缓存
   - 当转发请求的`Response`或`Push`带`Session`数据时，更新缓存
- [] 连接所有的`Logic`服务器
   - 缓存`Route`到`服务器`的映射和权重列表
   - 利用映射列表，处理`HandShake`
   - 把客户端的请求根据映射和权重列表，转发给对应的`Logic`服务器
   - 把`Logic`服务器的`Response`和`Push`转发给对应的客户端
- [] 客户端处理
   - 给客户端的`Session`填上`ClientGuid`，用于后续转发的映射
   - 当有新客户端连接/断开连接时，更新`ClientGuid`列表，并推送给所有的`Logic`服务器

#### Cluster-Logic

- [] 缓存所有`ClientGuid`到`Connector`的映射表

#### Cluster-Processor

- [] 对`Client`的封装，提供远程`Processor`