# GoPlay Server Framework

[![NuGet](https://img.shields.io/nuget/v/GoPlay.Server)](https://www.nuget.org/packages/GoPlay.Server)

GoPlay originated from the demand for online game development. It is a simple, easy-to-use, low learning curve, and out-of-the-box RPC framework based on C#, but not limited to game usage. It can be used to solve any real-time network communication problems.

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
//Echo_Echo is a function generated automatically by code.
var (status, resp) = await client.Echo_Echo(new PbString{
   Value = "Hello"
});
Assert.AreEqual(StatusCode.Success, status.Code);
Assert.AreEqual("Serv reply: Hello", resp.Value);
```

## Concepts

### Request

- Client sends a request to the server and expects a response from the server.

### Notify

- Client sends a request to the server without requiring a response from the server.

### Push

- The server actively pushes data to the client.

## Thanks

Thanks to JetBrains for providing an open source license for GoPlay.Net.
[![Thanks to JetBrains to provide opensource license for GoPlay.Net](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://jb.gg/OpenSourceSupport)

## The MIT License

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.