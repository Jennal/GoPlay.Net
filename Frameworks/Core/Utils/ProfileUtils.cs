using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Utils
{
    public static class ProfileUtils
    {
        public static void SummarizePackQueue(RespHandShake handShake, ConcurrentQueue<Package> queue)
        {
            var dict = new Dictionary<string, int>();
            foreach (var pack in queue)
            {
                var pair = handShake.Routes.FirstOrDefault(o => o.Value == pack.Header.PackageInfo.Route);
                var route = pair.Key ?? $"NONE({pack.Header.PackageInfo.Route})";

                if (!dict.ContainsKey(route)) dict[route] = 1;
                else dict[route]++;
            }

            Console.WriteLine($"=============[{queue.Count}]==================");
            foreach (var kv in dict.OrderByDescending(o => o.Value).Take(5))
            {
                Console.WriteLine($"\t {kv.Key} => {kv.Value}");
            }
            Console.WriteLine("===============================================");
        }
    }
}