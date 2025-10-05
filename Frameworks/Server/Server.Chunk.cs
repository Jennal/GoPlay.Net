using System.Collections.Concurrent;
using GoPlay.Core.Protocols;

namespace GoPlay
{
    public partial class Server<T>
    {
        protected ConcurrentDictionary<string, List<Package>> chunkCache = new ConcurrentDictionary<string, List<Package>>();
        
        public virtual Package ResolveChunk(Package pack)
        {
            var key = GetChunkKey(pack);
            var list = chunkCache.GetOrAdd(key, _ => new List<Package>());
            list.Add(pack);
            
            //未接收完全
            if (list.Count < pack.Header.PackageInfo.ChunkCount) return pack;
            
            //接收完全
            var p = Package.Join(list);
            
            //清理缓存
            list.Clear();
            chunkCache.TryRemove(key, out _);
            
            return p;
        }
        
        protected string GetChunkKey(Package pack)
        {
            return $"{pack.Header.ClientId}_{pack.Header.PackageInfo.Route}_{pack.Header.PackageInfo.Id}_{pack.Header.PackageInfo.ChunkCount}";
        }
    }
}