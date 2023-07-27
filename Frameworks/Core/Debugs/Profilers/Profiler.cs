using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GoPlay.Core.Debug
{
    public class ProfileStatus
    {
        public string Key;
        public Stopwatch Stopwatch = new Stopwatch();
        public long RunCount;
        public TimeSpan TotalTime = new TimeSpan();

        public TimeSpan Average => TimeSpan.FromTicks(TotalTime.Ticks / RunCount);
        
        public override string ToString()
        {
            return $@"[Profiler Status: {Key}]({RunCount}) ======
    => Total Time: {TotalTime.TotalMilliseconds} ms
    => Average Time: {Average.TotalMilliseconds} ms";
        }
    }
    
    public static class Profiler
    {
        private static ConcurrentDictionary<string, ProfileStatus> s_dict = new ConcurrentDictionary<string, ProfileStatus>();

        public static void Begin(string key)
        {
            if (!s_dict.ContainsKey(key)) s_dict[key] = new ProfileStatus
            {
                Key = key,
            };
            
            var status = s_dict[key];
            if (status.Stopwatch.IsRunning) throw new Exception($"Profiler: key={key}, Begin call twice before End Calls!");
            
            status.RunCount++;
            status.Stopwatch.Restart();
        }

        public static void End(string key)
        {
            if (!s_dict.ContainsKey(key)) throw new Exception($"Profiler: key={key}, End call without Begin!");

            var status = s_dict[key];
            if (!status.Stopwatch.IsRunning) throw new Exception($"Profiler: key={key}, End call without Begin!");
            
            status.Stopwatch.Stop();
            status.TotalTime = status.TotalTime.Add(status.Stopwatch.Elapsed);
        }

        public static string Statistics(string key)
        {
            if (!s_dict.ContainsKey(key)) throw new Exception($"Profiler: key={key} has no data!");
            return s_dict[key].ToString();
        }

        public static string Statistics()
        {
            var sb = new StringBuilder();
            foreach (var status in s_dict)
            {
                sb.AppendLine(status.Value.ToString());
            }

            return sb.ToString();
        }

        public static string StatisPrefix(string prefix)
        {
            var keys = s_dict.Keys.Where(o => o.StartsWith(prefix)).ToList();
            if (keys.Count <= 0) throw new Exception($"Profiler: key.StartWith = {prefix} has no data!");

            var countTotal = 0L;
            var countAvg = 0L;

            var tsTotal = 0L;
            var tsAvg = 0L;
            var tsMin = long.MaxValue;
            var tsMax = long.MinValue;
            var tsVar = 0L; //方差

            foreach (var key in keys)
            {
                var item = s_dict[key];

                countTotal += item.RunCount;
                tsTotal += item.Average.Ticks;

                tsMin = Math.Min(tsMin, item.Average.Ticks);
                tsMax = Math.Max(tsMax, item.Average.Ticks);
            }

            countAvg = countTotal / keys.Count;
            tsAvg = tsTotal / keys.Count;

            var avgTs = TimeSpan.FromTicks(tsAvg);
            tsVar = (long)keys.Select(o => Math.Abs(s_dict[o].Average.TotalMilliseconds * s_dict[o].Average.TotalMilliseconds - avgTs.TotalMilliseconds * avgTs.TotalMilliseconds)).Average();

            return $@"[Profiler Status: {prefix}*](all:{countTotal}, avg:{countAvg}) ======
    => Average Time: {TimeSpan.FromTicks(tsAvg).TotalMilliseconds} ms
    => Min Time: {TimeSpan.FromTicks(tsMin).TotalMilliseconds} ms
    => Max Time: {TimeSpan.FromTicks(tsMax).TotalMilliseconds} ms
    => Var ms: {tsVar}";
        }
        
        public static void Clear()
        {
            s_dict.Clear();
        }
    }
}