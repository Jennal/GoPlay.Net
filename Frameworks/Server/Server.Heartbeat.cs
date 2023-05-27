using System.Collections.Generic;
using GoPlay.Services.Core.Interfaces;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services
{
    public partial class Server<T>
    {
        protected virtual void OnPing(Package pack)
        {
            var resp = new Package
            {
                Header = pack.Header.Clone(),
            };
            resp.Header.ClientId = pack.Header.ClientId;
            resp.Header.PackageInfo.Type = PackageType.Pong;
            Send(resp);
        }
        
        protected virtual void OnPong(Package pack)
        {
            
        }
    }
}