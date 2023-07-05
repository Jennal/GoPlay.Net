# GoPlay Server Framework

GoPlay源于网络游戏开发的需求，基于C#编写的一套简单易用的RPC框架，但不限于游戏使用，可以用于解决任何实时网络通讯的问题。

## How To Use

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
//Echo_Echo是自动代码生成的函数
var (status, resp) = await client.Echo_Echo(new PbString{
   Value = "Hello"
});
Assert.AreEqual(StatusCode.Success, status.Code);
Assert.AreEqual("Serv reply: Hello", resp.Value);
```

## 概念

### Request

- 客户端向服务端发起请求，并获得服务端回复

### Notify

- 客户端向服务端发起请求，不要求服务端返回

### Push

- 服务端主动推送数据给客户端

## The MIT License

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.