# GoPlay Server Framework

GoPlay是一个足够简单，开箱即用的TCP长连接RPC开发框架。

## Sample

### Server

```csharp
[Processor("echo")]
class EchoProcessor : ProcessorBase
{
    [Request("echo")]
    public PbString Echo(Header header, PbString str)
    {
        return new PbString
        {
            Value = $"Server reply: {str.Value}"
        };
    }
```

### Client

```csharp
var (status, resp) = await client.Echo_Echo(new PbString{
   Value = "Hello"
});
if (status.Code == Status.Success) {
   Console.Writeline(resp.Value);
}
```

## 概念

### Request

- 客户端向服务端发起请求，并获得服务端回复

### Notify

- 客户端向服务端发起请求，不要求服务端返回

### Push

- 服务端主动推送数据给客户端
