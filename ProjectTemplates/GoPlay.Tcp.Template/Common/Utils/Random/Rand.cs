using System.Numerics;
using GoPlay.Common.Data;
using Item = GoPlay.Common.Data.Item;

namespace GoPlayProj.Utils
{
    public interface IValueWeight<T>
    {
        RandomItem<T> ToRandomItem();
    }
    
    [Serializable]
    public class RandomItem<TValue>
    {
        /// <summary>
        /// 值
        /// </summary>
        public TValue Value;
        
        /// <summary>
        /// 权重
        /// </summary>
        public float Weight;
    }
    
    [Serializable]
    public class RandomItemWithMax<TValue> : RandomItem<TValue>
    {
        /// <summary>
        /// 被抽取的数量限制
        /// </summary>
        public int Max;
    }
    
    [Serializable]
    public class RandomItemWithMinMax<TValue> : RandomItemWithMax<TValue>
    {
        /// <summary>
        /// 被抽取的数量限制最小值
        /// </summary>
        public int Min;
    }
    
    [Serializable]
    public class RandomItemWithOrderMinMax<TValue> : RandomItemWithMinMax<TValue>
    {
        /// <summary>
        /// 指定顺序
        /// 顺序从1开始，0表示任意排序
        /// </summary>
        public int Order;
    }


    public static class Rand
    {
        private static Random _random;
        
        private static RandBoxMuller _randBoxMuller;
        public static RandBoxMuller RandBoxMuller
        {
            get
            {
                if (_randBoxMuller == null) _randBoxMuller = new RandBoxMuller();
                return _randBoxMuller;
            }
        }

        static Rand()
        {
            if (_random == null) _random = new Random((int)DateTime.UtcNow.Ticks);
        }

        public static void SetSeed(int seed)
        {
            _random = new Random(seed);
        }
        
        public static void SetSeed(long seed)
        {
            SetSeed((int)(seed & uint.MaxValue));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="totalWeight"></param>
        /// <returns></returns>
        public static T RandomOne<T>(IEnumerable<RandomItem<T>> collection, float totalWeight)
        {
            var list = collection;
            if (!list.Any()) return default(T);

            var weight = 0f;
            foreach (var item in list)
            {
                weight += item.Weight;
                item.Weight = weight;
            }

            totalWeight = Math.Max(weight, totalWeight);

            var val = Range(0, totalWeight);
            foreach (var item in list)
            {
                if (item.Weight <= 0) continue;
                if (val <= item.Weight) return item.Value;
            }

            return default(T);
        }

        /// <summary>
        /// 每个元素带数量限制的随机
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="totalWeight"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RandomOneWithLimit<T>(IEnumerable<RandomItemWithMax<T>> collection, float totalWeight)
        {
            var item = RandomOne(collection.Where(o => o.Max > 0).Select(o => new RandomItem<RandomItemWithMax<T>>
            {
                Value = o,
                Weight = o.Weight
            }), totalWeight);
            item.Max--;

            return item.Value;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T RandomOne<T>(IEnumerable<RandomItem<T>> collection)
        {
            var list = collection;
            if (!list.Any()) return default(T);

            var totalWeight = list.Sum(o => o.Weight);
            var val = Range(0, totalWeight);
            var curWeight = 0f;
            foreach (var item in list)
            {
                if (item.Weight <= 0) continue;
                curWeight += item.Weight;

                if (val <= curWeight) return item.Value;
            }

            return list.Last().Value;
        }

        /// <summary>
        /// 每个元素带数量限制的随机
        /// </summary>
        /// <param name="collection"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RandomOneWithLimit<T>(IEnumerable<RandomItemWithMax<T>> collection)
        {
            var item = RandomOne(collection.Where(o => o.Max > 0).Select(o => new RandomItem<RandomItemWithMax<T>>
            {
                Value = o,
                Weight = o.Weight
            }));
            if (item == null) return default(T);
            
            item.Max--;
            return item.Value;
        }
        
        /// <summary>
        /// 从集合随机取一个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T RandomOne<T>(IEnumerable<T> collection)
        {
            var list = collection;
            if (!list.Any()) return default(T);

            var idx = Range(0, list.Count()-1);
            return list.ElementAt(idx);
        }
        
        /// <summary>
        /// 从集合随机取一个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static T RandomOne<T>(IEnumerable<T> collection, IEnumerable<float> weights)
        {
            if(collection.Count() != weights.Count()) throw new ArgumentException("count not match");
            return RandomOne(collection.Select((o, i) => new RandomItem<T>
            {
                Value = o,
                Weight = weights.ElementAt(i),
            }));
        }
        
        /// <summary>
        /// 从集合随机取一个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="getWeight"></param>
        /// <returns></returns>
        public static T RandomOne<T>(IEnumerable<T> collection, Func<T, float> getWeight)
        {
            return RandomOne(collection.Select((o, i) => new RandomItem<T>
            {
                Value = o,
                Weight = getWeight(o),
            }));
        }

        public static T RandomOne<T>(IEnumerable<T> collection, IEnumerable<T> ignores)
        {
            var list = collection.Where(o => ignores == null || !ignores.Contains(o));
            return RandomOne(list);
        }

        public static T RandomPopOne<T>(ICollection<T> collection)
        {
            var result = RandomOne(collection);
            collection.Remove(result);
            return result;
        }
        
        public static T RandomPopOne<T>(ICollection<T> collection, Func<T, float> getWeight)
        {
            var result = RandomOne(collection, getWeight);
            collection.Remove(result);
            return result;
        }

        /// <summary>
        /// 通过percent取带权重队列中的一个值
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="percent">[0, 1]</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LerpOne<T>(IEnumerable<RandomItem<T>> collection, float percent)
        {
            var list = collection;
            if (!list.Any()) return default(T);

            var totalWeight = list.Sum(o => o.Weight);
            var val = totalWeight * percent;
            var curWeight = 0f;
            foreach (var item in list)
            {
                if (item.Weight <= 0) continue;
                curWeight += item.Weight;

                if (val <= curWeight) return item.Value;
            }

            return list.Last().Value;
        }
        
        /// <summary>
        /// 通过percent取带权重队列中的一个值
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="weights"></param>
        /// <param name="percent">[0, 1]</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LerpOne<T>(IEnumerable<T> collection, IEnumerable<float> weights, float percent)
        {
            if (collection.Count() != weights.Count()) throw new ArgumentException("count not match");
            return LerpOne(collection.Select((o, i) => new RandomItem<T>
            {
                Value = o,
                Weight = weights.ElementAt(i),
            }), percent);
        }
        
        /// <summary>
        /// 打乱顺序
        /// </summary>
        /// <returns>The shuffle.</returns>
        /// <param name="srcArr">Source arr.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static List<T> RandomShuffle<T> (List<T> srcArr)
        {
            var retArr = new List<T>();
            var count = srcArr.Count;
            for (var i = 0; i < count; ++i)
            {
                var obj = RandomPopOne(srcArr);
                retArr.Add(obj);
            }

            return retArr;
        }

        /// <summary>
        /// 从列表中抽取不重复的count个
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> RandomCount<T>(IEnumerable<T> collection, int count)
        {
            if (collection.Count() < count) throw new Exception($"Rand.RandomCount: collection.Count({collection.Count()}) < count({count})");
            var list = new List<T>(collection);
            for (var i = 0; i < count; i++)
            {
                yield return RandomPopOne(list);
            }
        }
        
        /// <summary>
        /// 从列表中抽取可重复的count个
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> RandomCountRepeatable<T>(IEnumerable<T> collection, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return RandomOne(collection);
            }
        }
        
        /// <summary>
        /// 从带最大最小值限制的列表中抽取count个
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> RandomCount<T>(ICollection<RandomItemWithMinMax<T>> collection, int count)
        {
            foreach (var item in collection)
            {
                while (count > 0 && item.Min > 0 && item.Max > 0)
                {
                    count--;
                    item.Min--;
                    item.Max--;
                    yield return item.Value;
                }
            }
            if (count <= 0) yield break;

            var list = collection.Where(o => o.Max > 0);
            while (count > 0 && list.Any())
            {
                var item = RandomOneWithLimit(list);
                if (item == null) break;

                count--;
                yield return item;
            }
        }
        
        /// <summary>
        /// 从带 顺序/最大值/最小值 限制的列表中抽取count个
        /// 顺序从1开始，0表示任意排序
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> RandomCount<T>(ICollection<RandomItemWithOrderMinMax<T>> collection, int count)
        {
            var list = collection.Select(o => new RandomItemWithMinMax<Tuple<T, int>>
            {
                Value = new Tuple<T, int>(o.Value, Math.Clamp(o.Order, 0, count)),
                Weight = o.Weight,
                Min = o.Min,
                Max = o.Max
            }).ToList();

            var tupleResult = RandomCount(list, count).ToList();
            var orderedResults = tupleResult.Where(o => o.Item2 > 0).OrderBy(o => o.Item2).ToList();
            var otherResults = RandomShuffle(tupleResult.Where(o => o.Item2 <= 0).ToList());

            for (var i = 1; i <= count; i++)
            {
                var item = orderedResults.FirstOrDefault(o => o.Item2 == i);
                if (item != null)
                {
                    orderedResults.Remove(item);
                }
                else if (otherResults.Any())
                {
                    item = otherResults.First();
                    otherResults.Remove(item);
                }

                if (item == null) continue;

                yield return item.Item1;
            }
        }
        
        /// <summary>
        /// 带保底的概率抽取
        /// </summary>
        /// <param name="randGroups">抽取配置</param>
        /// <param name="rewardStatis">保底次数统计</param>
        /// <param name="getRewardStatisKey">保底key</param>
        /// <returns></returns>
        public static (int, Dictionary<int, int>) RandomGuaranteedId(RandGroupWithGuaranteed[] randGroups, Dictionary<int, int> rewardStatis, Func<int, int> getRewardStatisKey)
        {
            var result = 0;
            
            //计算保底
            foreach (var group in randGroups)
            {
                var key = getRewardStatisKey(group.Id);
                if (!rewardStatis.ContainsKey(key)) continue;

                if (rewardStatis[key] >= group.Guaranteed)
                {
                    result = group.Id;
                    break;
                }
            }

            //非保底抽取
            if (result == 0)
            {
                var item = RandomOne(randGroups, o => o.Weight);
                result = item.Id;
            }

            //保底数值 +1
            foreach (var group in randGroups)
            {
                if (group.Id == result) continue;
                
                var key = getRewardStatisKey(group.Id);
                if (!rewardStatis.ContainsKey(key))
                {
                    rewardStatis[key] = 0;
                }

                rewardStatis[key]++;
            }
            
            return (result, rewardStatis);
        }
        
        /// <summary>
        /// 值,权重;值,权重;
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static int RandomConfInt(string conf, string arrSplit = "|", string randSplit = ";") {
            bool formatError = false;
            var arr = conf.Split(arrSplit.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItem<int>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                
                var subArr = s.Split(randSplit.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 2) {
                    formatError = true;
                    continue;
                }
                
                if (int.TryParse(subArr[0].Trim(), out int v) && float.TryParse(subArr[1].Trim(), out float w)) {
                    list.Add(new RandomItem<int>
                    {
                        Value = v,
                        Weight = w,
                    }); 
                }
                else {
                    formatError = true;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfInt] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            return RandomOne(list);
        }
        
        /// <summary>
        /// 值,权重;值,权重;
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static long RandomConfLong(string conf, string arrSplit = "|", string randSplit = ";") {
            bool formatError = false;
            var arr = conf.Split(arrSplit.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItem<long>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                
                var subArr = s.Split(randSplit.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 2) {
                    formatError = true;
                    continue;
                }
                
                if (long.TryParse(subArr[0].Trim(), out long v) && float.TryParse(subArr[1].Trim(), out float w)) {
                    list.Add(new RandomItem<long>
                    {
                        Value = v,
                        Weight = w,
                    }); 
                }
                else {
                    formatError = true;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfInt] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            return RandomOne(list);
        }
        
        /// <summary>
        /// 值1,值2,权重;值1,值2,权重;
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static Tuple<int, int> RandomConfInt2(string conf) {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItem<Tuple<int, int>>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                
                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 2) {
                    formatError = true;
                    continue;
                }
                
                if (int.TryParse(subArr[0].Trim(), out int v1) &&
                    int.TryParse(subArr[1].Trim(), out int v2) &&
                    float.TryParse(subArr[2].Trim(), out float w)) {
                    list.Add(new RandomItem<Tuple<int, int>>
                    {
                        Value = new Tuple<int, int>(v1, v2),
                        Weight = w,
                    }); 
                }
                else {
                    formatError = true;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfInt] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            return RandomOne(list);
        }
        
        /// <summary>
        /// 最小值,最大值,权重;最小值,最大值,权重;
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static int RandomConfIntFromRange(string conf)
        {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItem<Vector2Int>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                
                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 3) {
                    formatError = true;
                    continue;
                }
                
                if (int.TryParse(subArr[0].Trim(), out int min) && 
                    int.TryParse(subArr[1].Trim(), out int max) && 
                    float.TryParse(subArr[2].Trim(), out float w)) {
                    
                    list.Add(new RandomItem<Vector2Int>
                    {
                        Value = new Vector2Int(min, max),
                        Weight = w,
                    }); 
                }
                else {
                    formatError = true;
                }
            }
            
            if (formatError) {
                var errMsg = $"[RandomConfIntFromRange] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            
            var val = RandomOne(list);
            return Range(val);
        }

        /// <summary>
        /// 值,权重,最小值,最大值;值,权重,最小值,最大值;
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<int> RandomConfIntWithMinMax(string conf, int count)
        {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItemWithMinMax<int>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;

                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 4) {
                    formatError = true;
                    continue;
                }

                //支持 1001-1010,100,0,1;
                if (subArr[0].Contains("-"))
                {
                    var val = subArr[0].Trim();
                    int start = 0;
                    int end = 0;
                    var idx = val.IndexOf('-');
                    if (!int.TryParse(val.Substring(0, idx), out start) ||
                        !int.TryParse(val.Substring(idx + 1, val.Length - idx - 1), out end) ||
                        start > end)
                    {
                        formatError = true;
                        break;
                    }

                    if (float.TryParse(subArr[1].Trim(), out float w1) &&
                        int.TryParse(subArr[2].Trim(), out int min1) &&
                        int.TryParse(subArr[3].Trim(), out int max1)) {

                        for (int i = start; i <= end; i++)
                        {
                            list.Add(new RandomItemWithMinMax<int>
                            {
                                Value = i,
                                Weight = w1,
                                Min = min1,
                                Max = max1,
                            });
                        }
                    }
                    
                    continue;
                }
                
                if (int.TryParse(subArr[0].Trim(), out int v) && 
                    float.TryParse(subArr[1].Trim(), out float w) &&
                    int.TryParse(subArr[2].Trim(), out int min) &&
                    int.TryParse(subArr[3].Trim(), out int max)) {
                    
                    list.Add(new RandomItemWithMinMax<int>
                    {
                        Value = v,
                        Weight = w,
                        Min = min,
                        Max = max,
                    }); 
                }
                else {
                    formatError = true;
                    break;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfIntWithMinMax] conf Format Error : [{conf}]";
                throw new Exception(errMsg);
            }
            return RandomCount(list, count);
        }
        
        /// <summary>
        /// 值,顺序,权重,最小值,最大值;值,顺序,权重,最小值,最大值;
        /// 顺序从1开始，0表示任意排序
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<int> RandomConfIntWithOrderMinMax(string conf, int count)
        {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItemWithOrderMinMax<int>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;

                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 5) {
                    formatError = true;
                    continue;
                }

                //支持 1001-1010,0,100,0,1;
                if (subArr[0].Contains("-"))
                {
                    var val = subArr[0].Trim();
                    int start = 0;
                    int end = 0;
                    var idx = val.IndexOf('-');
                    if (!int.TryParse(val.Substring(0, idx), out start) ||
                        !int.TryParse(val.Substring(idx + 1, val.Length - idx - 1), out end) ||
                        start > end)
                    {
                        formatError = true;
                        break;
                    }

                    if (int.TryParse(subArr[1].Trim(), out var o1) &&
                        float.TryParse(subArr[2].Trim(), out var w1) &&
                        int.TryParse(subArr[3].Trim(), out var min1) &&
                        int.TryParse(subArr[4].Trim(), out var max1)) {

                        for (int i = start; i <= end; i++)
                        {
                            list.Add(new RandomItemWithOrderMinMax<int>
                            {
                                Value = i,
                                Order = o1,
                                Weight = w1,
                                Min = min1,
                                Max = max1,
                            });
                        }
                    }
                    
                    continue;
                }
                
                if (int.TryParse(subArr[0].Trim(), out var v) &&
                    int.TryParse(subArr[1].Trim(), out var o) &&
                    float.TryParse(subArr[2].Trim(), out var w) &&
                    int.TryParse(subArr[3].Trim(), out var min) &&
                    int.TryParse(subArr[4].Trim(), out var max)) {
                    
                    list.Add(new RandomItemWithOrderMinMax<int>
                    {
                        Value = v,
                        Order = o,
                        Weight = w,
                        Min = min,
                        Max = max,
                    }); 
                }
                else {
                    formatError = true;
                    break;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfIntWithMinMax] conf Format Error : [{conf}]";
                throw new Exception(errMsg);
            }
            return RandomCount(list, count);
        }

        /// <summary>
        /// 根据配置获取一组(count个)随机整数值
        /// 配置格式(字符串) : 值,权重,最小值,最大值;值,权重,最小值,最大值;
        /// </summary>
        /// <returns>The conf int with minimum max exclude.</returns>
        /// <param name="conf">Conf.</param>
        /// <param name="count">Count.</param>
        /// <param name="alreadyHave">已经存在的,需要排除</param>
        public static IEnumerable<int> RandomConfIntWithMinMaxExclude(string conf, int count, List<int> alreadyHave = null)
        {
            if (alreadyHave == null) alreadyHave = new List<int>();
            var formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.None);
            var list = new List<RandomItemWithMinMax<int>>();
            foreach (var arg in arr)
            {
                if (string.IsNullOrEmpty(arg)) continue;

                var subArr = arg.Split(",".ToCharArray(), StringSplitOptions.None);
                if (subArr.Length < 4)
                {
                    formatError = true;
                    continue;
                }

                var val = int.Parse(subArr[0]);
                var weight = float.Parse(subArr[1]);
                var min = int.Parse(subArr[2]);
                var max = int.Parse(subArr[3]);

                list.Add(new RandomItemWithMinMax<int>
                {
                    Value = val,
                    Weight = weight,
                    Min = min,
                    Max = max,
                });
            }

            if (formatError)
            {
                throw new Exception("randomConfIntWithMinMax formatError!!!");
            }

            var configCount = list.Count;
            foreach (var i in alreadyHave)
            {
                var val = alreadyHave[i];
                for (var confIndex = 0; confIndex < configCount; ++confIndex)
                {
                    if (list[confIndex].Value == val)
                    {
                        list[confIndex].Min--;
                        list[confIndex].Max--;
                        break;
                    }
                }
            }

            return RandomCount(list, count);
        }


        /// <summary>
        /// 值,权重;值,权重;
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static float RandomConfFloat(string conf)
        {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItem<float>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 2) {
                    formatError = true;
                    continue;
                }
                
                if (float.TryParse(subArr[0].Trim(), out float v) && float.TryParse(subArr[1].Trim(), out float w)) {
                    list.Add(new RandomItem<float>
                    {
                        Value = v,
                        Weight = w,
                    }); 
                }
                else {
                    formatError = true;
                }
            }

            if (formatError) {
                var errMsg = $"[RandomConfFloat] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            return RandomOne(list);
        }

        /// <summary>
        /// 值,权重,最小值,最大值;值,权重,最小值,最大值;
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<float> RandomConfFloatWithMinMax(string conf, int count)
        {
            bool formatError = false;
            var arr = conf.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var list = new List<RandomItemWithMinMax<float>>();
            foreach (var s in arr)
            {
                if (string.IsNullOrEmpty(s.Trim())) continue;
                var subArr = s.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subArr.Length < 4) {
                    formatError = true;
                    continue;
                }
                
                if (float.TryParse(subArr[0].Trim(), out float v) && 
                    float.TryParse(subArr[1].Trim(), out float w) &&
                    int.TryParse(subArr[2].Trim(), out int min) &&
                    int.TryParse(subArr[3].Trim(), out int max)) {
                    
                    list.Add(new RandomItemWithMinMax<float>
                    {
                        Value = v,
                        Weight = w,
                        Min = min,
                        Max = max,
                    }); 
                }
                else {
                    formatError = true;
                }
            }
            
            if (formatError) {
                var errMsg = $"[RandomConfFloatWithMinMax] conf Format Error : [{conf}]";
                throw new Exception(errMsg); 
            }
            return RandomCount(list, count);
        }

        /// <summary>
        /// 值;最小值;最大值|值;最小值;最大值|
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<Item> RandomConfItems(string conf)
        {
            if (string.IsNullOrEmpty(conf)) yield break;
            
            var arr = conf.Split("|", StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in arr)
            {
                var subArr = item.Split(";", StringSplitOptions.RemoveEmptyEntries);
                var id = int.Parse(subArr[0]);
                var min = int.Parse(subArr[1]);
                var max = int.Parse(subArr[2]);

                yield return new Item
                {
                    Id = id,
                    Count = Range(min, max),
                };
            }
        }
        
        /// <summary>
        /// 随机枚举
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RandomEnum<T>()
        {
            var list = Enum.GetValues(typeof(T)).OfType<T>();
            return RandomOne(list);
        }

        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Range(double min, double max)
        {
            return min + _random.NextSingle() * (max - min);
        }
        
        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Range(float min, float max)
        {
            return min + _random.NextSingle() * (max - min);
        }

        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Range(int min, int max)
        {
            return _random.Next(min, max + 1);
        }
        
        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static long Range(long min, long max)
        {
            return min + (long)((max-min) * Range(0f, 1f));
        }

        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector2 Range(Vector2 min, Vector2 max)
        {
            var val = new Vector2(
                Range(min.X, max.X),
                Range(min.Y, max.Y)
            );
            return val;
        }

        /// <summary>
        /// [min, max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector3 Range(Vector3 min, Vector3 max)
        {
            var val = new Vector3(
                Range(min.X, max.X),
                Range(min.Y, max.Y),
                Range(min.Z, max.Z)
            );
            return val;
        }
        
        /// <summary>
        /// [bounds.min, bounds.max]
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        // public static Vector3 Range(Bounds bounds)
        // {
        //     return Range(bounds.min, bounds.max);
        // }

        /// <summary>
        /// [minMax.x, minMax.y]
        /// </summary>
        /// <param name="minMax"></param>
        /// <returns></returns>
        public static float Range(Vector2 minMax)
        {
            return Range(minMax.X, minMax.Y);
        }

        /// <summary>
        /// [minMax.x, minMax.y]
        /// </summary>
        /// <param name="minMax"></param>
        /// <returns></returns>
        public static int Range(Vector2Int minMax)
        {
            return Range(minMax.x, minMax.y);
        }

        /// <summary>
        /// 概率返回True
        /// </summary>
        /// <param name="rate">[0, 1]</param>
        /// <returns></returns>
        public static bool True(float rate=0.5f)
        {
            return Range(0f, 1f) <= rate;
        }
        
        /// <summary>
        /// 概率返回True
        /// </summary>
        /// <param name="rate">[0, 10000]</param>
        /// <returns></returns>
        public static bool TrueInt(int rate=5000)
        {
            return Range(0, 10000) <= rate;
        }

        /// <summary>
        /// 正态分布随机数
        /// </summary>
        /// <returns></returns>
        public static double Normal(double mu, double sigma)
        {
            return RandBoxMuller.Rand(mu, sigma);
        }

        /// <summary>
        /// 限制取值范围的正态分布随机数
        /// </summary>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Normal(double mu, double sigma, double min, double max)
        {
            var val = Normal(mu, sigma);
            if (val >= min && val <= max) return val;

            var delta = Math.Min(Math.Abs(mu - min), Math.Abs(mu - max));
            delta = Math.Min(sigma, delta);

            return Range(mu - delta, mu + delta);
        }
    }
}