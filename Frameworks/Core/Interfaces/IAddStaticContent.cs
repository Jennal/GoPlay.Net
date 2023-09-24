using System;

namespace GoPlay.Core.Interfaces
{
    public interface IAddStaticContent
    {
        void AddStaticContent(string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null);
    }
}