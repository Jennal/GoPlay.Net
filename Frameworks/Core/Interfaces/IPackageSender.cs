using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Interfaces
{
    public interface IPackageSender
    {
        void Send(Package package);
    }
}