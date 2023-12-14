using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoPlay.Core;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;

namespace GoPlay
{
    public partial class Client<T>
    {
        /// <summary>
        /// 由于ResolveChunk不会在多线程执行，不需要使用ConcurrentDictionary
        /// </summary>
        protected Dictionary<string, List<Package>> chunkCache = new Dictionary<string, List<Package>>();
        
        public virtual Package ResolveChunk(Package pack)
        {
            var key = GetChunkKey(pack);
            if (!chunkCache.ContainsKey(key)) chunkCache[key] = new List<Package>();

            var list = chunkCache[key];
            list.Add(pack);
            
            //未接收完全
            if (list.Count < pack.Header.PackageInfo.ChunkCount) return pack;
            
            //接收完全
            var p = Package.Join(list);
            
            //清理缓存
            list.Clear();
            chunkCache.Remove(key);
            
            return p;
        }
        
        protected string GetChunkKey(Package pack)
        {
            return $"{pack.Header.ClientId}_{pack.Header.PackageInfo.Route}_{pack.Header.PackageInfo.Id}_{pack.Header.PackageInfo.ChunkCount}";
        }
    }
}