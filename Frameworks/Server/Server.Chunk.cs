using GoPlay.Core.Protocols;

namespace GoPlay
{
    public partial class Server<T>
    {

        /// <summary>
        /// 按连接重组分包。缓冲挂在 <see cref="ClientLifetime.ChunkCache"/> 上：
        /// 同一 session 的收包在 IOCP 路径上串行，跨连接互不共享，因此无需 ConcurrentDictionary。
        /// </summary>

        public virtual Package ResolveChunk(Package pack)
        {
            // 连接尚未登记或已断开：无法可靠缓存，原样返回（调用方见 IsChunk 会丢弃）。
            if (!m_liveClients.TryGetValue(pack.Header.ClientId, out var lifetime)) return pack;

            var chunkCache = lifetime.ChunkCache;
            var key = GetChunkKey(pack);

            if (!chunkCache.TryGetValue(key, out var list))
            {
                list = new List<Package>();
                chunkCache[key] = list;
            }

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

        /// <summary>
        /// 连接内唯一键：ClientId 已由 session 隔离，不必再编进 key。
        /// </summary>
        protected string GetChunkKey(Package pack)
        {
            return $"{pack.Header.PackageInfo.Route}_{pack.Header.PackageInfo.Id}_{pack.Header.PackageInfo.ChunkCount}";
        }
    }
}
