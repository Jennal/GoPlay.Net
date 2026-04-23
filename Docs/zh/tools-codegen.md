# 工具与代码生成

> English version: [tools-codegen.md](../en/tools-codegen.md)

GoPlay 的"像本地函数一样调用"体验，是靠三层代码生成拼出来的：

1. **`goplay` CLI** —— 扫描服务端 Processor，为客户端生成扩展方法 / 协议常量；Excel 导出 / Proto Enum 导出。
2. **Roslyn 源生成器** —— 编译期为 `[ProcessorApi]` 方法生成 `ProcessorRef<T>` 扩展。
3. **Roslyn 静态分析器** —— 编译期拦截非法的跨 Processor 访问和 `[MaxConcurrency]` 配置。

另外还有**项目模板**（`dotnet new goplay-tcp / goplay-ws`）让你一条命令起一个完整工程。

## 一、goplay CLI（`GoPlay.Tools` 包）

### 安装

```bash
dotnet tool install -g GoPlay.Tools
goplay info
```

命令名固定为 `goplay`（见 [Tools/Main/Main.csproj](../../Tools/Main/Main.csproj) 的 `ToolCommandName`）。

### 子命令一览

```text
goplay info            显示进程信息（调试用）
goplay extension       扫描 Processor 生成客户端/服务端扩展
goplay config          Excel → cs/yaml/json 数据表导出
goplay excel2proto     Excel → .proto enum
```

### `goplay extension` —— 生成客户端代理

命令示例（来自模板 [gen_ext.sh](../../ProjectTemplates/GoPlay.Tcp.Template/scripts/gen_ext.sh)）：

```bash
goplay extension \
  -i  $DIR \
  -ob $DIR/Client.Extension/ClientExtensions.be.cs \
  -of $DIR/Client.Extension/ClientExtensions.fe.cs \
  -b  ProcessorBase,GoPlayProjProcessor \
  -tb $DIR/scripts/liquids/server.liquid \
  -tf $DIR/scripts/liquids/server.liquid \
  -nb GoPlay.Core.Protocols \
  -nf GoPlay.Core.Protocols
```

关键参数：
- `-i` 输入 Solution 目录（会分析 `.sln`）
- `-of` / `-ob` 分别输出 FrontEnd / BackEnd 用的扩展文件
- `-tf` / `-tb` 自定义 [Liquid](https://shopify.github.io/liquid/) 模板（也可输出 TypeScript、Unity C# 等目标语言）
- `-b` 用来过滤基类，只扫描从这些基类派生的 Processor
- `-nf` / `-nb` 生成代码里额外 `using` 的命名空间
- `-igt` / `-igm` 类 / 方法忽略名单

生成示例（默认 C# 模板）：

```csharp
public static class ClientExtensions
{
    public static Task<(Status, PbString)> Echo_Request(this Client client, PbString data)
        => client.Request<PbString, PbString>("echo.request", data);

    public static void Echo_Notify(this Client client, PbString data)
        => client.Notify("echo.notify", data);
}

public static class ProtocolConsts
{
    public const string Push_EchoPush = "echo.push";
}
```

要生成 TypeScript 等非 C# 客户端，写一份 `.liquid` 模板即可，底层扫描逻辑一致（详见 [Tools/Generator.Extension/Processors2Extension.cs](../../Tools/Generator.Extension/Processors2Extension.cs)）。

### `goplay config` —— Excel 数据表导出

```bash
goplay config \
  -i Excels/ \
  -oc GoPlayProj/Common/Configs/ \
  -od data/ \
  -p s \
  -t yaml
```

- 输入一个 Excel 目录，输出对应 `.cs`（类型定义 + Manager）+ `.yaml`/`.json`。
- 支持数组分隔符（`-s`）、平台切分（`-p s|c`，服务端/客户端字段不同）、强制重新导出（`-f`）、清理旧文件（`-c`）。
- 支持自定义 Liquid 模板（`-tc` / `-tm` / `-te`）。
- 内部用 EPPlus 读 Excel，YamlDotNet 写 YAML；实现位于 [Tools/Generator.Config/](../../Tools/Generator.Config/)。

### `goplay excel2proto` —— 把 Excel 枚举直接导出成 `.proto`

适合把运营表里的枚举列同步到协议：

```bash
goplay excel2proto -i Excels/ -oc proto/
```

## 二、Roslyn 源生成器：ProcessorRef 扩展

`Tools/Generator.ProcessorRef` 是随 `GoPlay.Server` 一起安装的 Roslyn 源生成器。它会在编译期扫描所有标了 `[ProcessorApi]` 的方法，自动生成 `ProcessorRef<T>` 的同名扩展方法 —— 业务侧就可以直接 `Server.GetProcessor<Xxx>().TheMethod(...)`。

参见：
- 标注接口：[Frameworks/Core/Attributes/ProcessorApiAttribute.cs](../../Frameworks/Core/Attributes/ProcessorApiAttribute.cs)
- 生成器：[Tools/Generator.ProcessorRef/ProcessorRefGenerator.cs](../../Tools/Generator.ProcessorRef/ProcessorRefGenerator.cs)
- 用法细节：[processor-model.md](./processor-model.md#跨-processor-调用processorref)

### 典型约束

- 必须是 Processor 的 `public` 实例方法（非 static / private / internal）。
- 参数不能有 `ref` / `out` / `in`（跨 mailbox 传引用无意义）。
- 推荐返回 `Task` / `Task<T>`；如果标了 `Fire = true`，也可以返回 `void`。

## 三、静态分析器（编译期保护）

### Analyzer.ProcessorIsolation

- 禁止业务代码直接持有别的 Processor 对象 / 调它的私有字段（绕过 mailbox 会破坏串行语义）。
- 对 `Server.GetProcessorUnsafe<T>()` 直接用也会警告（该 API 本身已 `[Obsolete]`）。

### Analyzer.MaxConcurrency

- 校验 `[MaxConcurrency(N)]` 的 `N >= 1`。
- 校验方法级 `N <= ` 类级 `N`。
- 非法配置直接在编译期报错，避免运行期才发现。

两份分析器都通过 NuGet 随 `GoPlay.Server` 引入，不需要业务单独安装。

## 四、项目模板（`GoPlay.Templates` 包）

### 安装与使用

```bash
dotnet new install GoPlay.Templates

dotnet new goplay-tcp -n MyGame         # 基于 NcServer
dotnet new goplay-ws  -n MyGame         # 基于 WsServer

dotnet new uninstall GoPlay.Templates   # 清理
```

### 展开后的多工程结构

```text
MyGame/
  GoPlayProj.sln
  Main/                   # 程序入口（System.CommandLine + HostBuilder）
  ProcessorsBase/         # 业务 Processor 公共父类 GoPlayProjProcessor
  Processors.Logic/       # 业务 Processor（EchoProcessor / TimeProcessor 起手）
  Processors.Admin/       # 管理端 Processor
  Processors.DbSaver/     # 持久化 Processor（示范 [ProcessorApi]）
  Client.Extension/       # goplay extension 的输出目录
  Common/                 # RunArgs / AppConfig / SessionManager 扩展
  UnitTests/              # NUnit 端到端测试
  scripts/
    gen_ext.sh            # 运行 goplay extension
    gen_proto.sh          # 编译 .proto
    gen_config.sh         # 运行 goplay config
    gen_db.sh             # DB schema 生成（如启用）
    liquids/              # 自定义模板
  app.json / app.test.json
```

### 脚本职责

- `gen_ext.sh`：每次新增/改动 Processor 后跑，重新生成 `Client.Extension/*.cs`。
- `gen_proto.sh`：改了 `.proto` 后跑，调用 `protoc` 出 C# 代码。
- `gen_config.sh`：运营表（Excel）改动后跑，出 `.cs` + `.yaml`。

这些脚本都是 **bash**，Windows 用 Git Bash 或 WSL 执行；内容非常薄（基本就是 `goplay` 命令的一行封装）。

## 五、本地开发发布工具链

打算贡献框架本身时：

```bash
# 构建并打包框架所有 NuGet（本地 ./packages 目录）
bash Frameworks/Scripts/build_nupkg.sh

# 发布到 nuget.org（需要 NUGET_KEY 环境变量）
bash Frameworks/Scripts/publish_nupkg.sh

# 发布 goplay CLI
bash Tools/publish_nuget.sh

# 把 goplay CLI 装到本地（开发期）
bash Tools/publish_local.sh
```

文件位置见：[Frameworks/Scripts/](../../Frameworks/Scripts/)、[Tools/publish_nuget.sh](../../Tools/publish_nuget.sh)、[Tools/publish_local.sh](../../Tools/publish_local.sh)。
