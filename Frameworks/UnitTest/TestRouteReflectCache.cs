using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using NUnit.Framework;

namespace UnitTest
{
    [TestFixture]
    [NonParallelizable]
    public class TestRouteReflectCache
    {
        private const int RoundCount = 16;
        private const int DistinctTypeCount = 512;
        private const int RepeatedAccessPerWorker = 256;

        private static readonly string[] CacheFieldNames =
        {
            "s_dictParseFromRawMethods",
            "s_dictDataFields",
            "s_dictReturnTypes",
        };

        private static readonly Type[] BaseTypes =
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(char),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(object),
            typeof(PbString),
            typeof(PbInt),
            typeof(PbLong),
            typeof(PbFloat),
            typeof(PbBool),
            typeof(Header),
            typeof(Status),
            typeof(PackageInfo),
            typeof(ReqHankShake),
            typeof(RespHandShake),
        };

        private static readonly Type[] FamilyTypes =
        {
            typeof(Family0),
            typeof(Family1),
            typeof(Family2),
            typeof(Family3),
        };

        [TearDown]
        public void TearDown()
        {
            ClearCaches();
        }

        [Test]
        [Timeout(60000)]
        public async Task TestConcurrentColdCacheAccessDoesNotCorruptReflectCaches()
        {
            var dataTypes = CreateDistinctDataTypes(DistinctTypeCount);
            var hotType = typeof(CacheKey<HotFamily, PbString, PbLong>);
            var workerCount = Math.Min(128, Math.Max(32, Environment.ProcessorCount * 8));

            for (var round = 0; round < RoundCount; round++)
            {
                ClearCaches();

                var failures = new ConcurrentQueue<Exception>();
                var ready = new CountdownEvent(workerCount);
                var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var nextColdTypeIndex = -1;

                async Task Worker(int workerId)
                {
                    ready.Signal();
                    await start.Task.ConfigureAwait(false);

                    try
                    {
                        // All workers hit the same missing key first. This maximizes the
                        // check-then-create race window for all three caches.
                        ValidateCacheEntry(hotType, workerId);

                        // Every generated Type is inserted exactly once per round, but the
                        // insertions happen concurrently and force the dictionaries to resize.
                        while (true)
                        {
                            var index = Interlocked.Increment(ref nextColdTypeIndex);
                            if (index >= dataTypes.Length) break;

                            ValidateCacheEntry(dataTypes[index], workerId + index);
                        }

                        // Keep reading overlapping keys while other workers may still be
                        // finishing cold inserts. This covers concurrent reads and writes.
                        for (var i = 0; i < RepeatedAccessPerWorker; i++)
                        {
                            var index = (workerId * 397 + i * 17 + round * 31) % dataTypes.Length;
                            ValidateCacheEntry(dataTypes[index], workerId + i + round);
                        }
                    }
                    catch (Exception err)
                    {
                        failures.Enqueue(new InvalidOperationException(
                            $"Reflect cache worker failed. Round={round}, Worker={workerId}", err));
                    }
                }

                var workers = Enumerable.Range(0, workerCount)
                    .Select(Worker)
                    .ToArray();

                Assert.That(ready.CurrentCount, Is.EqualTo(0),
                    "Every worker should be waiting at the common start gate.");

                start.TrySetResult(true);
                await Task.WhenAll(workers).ConfigureAwait(false);

                if (!failures.IsEmpty)
                {
                    var details = string.Join(
                        Environment.NewLine + Environment.NewLine,
                        failures.Take(8).Select(o => o.ToString()));
                    Assert.Fail($"Concurrent reflect-cache access produced {failures.Count} failure(s).{Environment.NewLine}{details}");
                }

                var expectedCount = dataTypes.Length + 1; // distinct generated types + hotType
                foreach (var fieldName in CacheFieldNames)
                {
                    Assert.That(GetCacheCount(fieldName), Is.EqualTo(expectedCount),
                        $"Cache {fieldName} lost or duplicated entries in round {round}.");
                }

                // Verify the final published cache contents after all competing writers stop.
                ValidateCacheEntry(hotType, round);
                foreach (var dataType in dataTypes)
                {
                    ValidateCacheEntry(dataType, round);
                }
            }
        }

        private static void ValidateCacheEntry(Type dataType, int orderSeed)
        {
            MethodInfo parseMethod;
            FieldInfo dataField;
            Type returnType;

            // Change call order so GetDataField's nested GetReturnType access races with
            // direct GetReturnType calls instead of always following one fixed sequence.
            switch (Math.Abs(orderSeed % 3))
            {
                case 0:
                    parseMethod = Route.GetParseFromRawMethod(dataType);
                    dataField = Route.GetDataField(dataType);
                    returnType = Route.GetReturnType(dataType);
                    break;
                case 1:
                    dataField = Route.GetDataField(dataType);
                    returnType = Route.GetReturnType(dataType);
                    parseMethod = Route.GetParseFromRawMethod(dataType);
                    break;
                default:
                    returnType = Route.GetReturnType(dataType);
                    parseMethod = Route.GetParseFromRawMethod(dataType);
                    dataField = Route.GetDataField(dataType);
                    break;
            }

            var expectedReturnType = typeof(Package<>).MakeGenericType(dataType);
            if (returnType != expectedReturnType)
            {
                throw new InvalidOperationException(
                    $"Unexpected return type for {dataType}: {returnType} != {expectedReturnType}.");
            }

            if (dataField.Name != nameof(Package<object>.Data) ||
                dataField.DeclaringType != expectedReturnType ||
                dataField.FieldType != dataType)
            {
                throw new InvalidOperationException(
                    $"Unexpected Data field for {dataType}: " +
                    $"Name={dataField.Name}, DeclaringType={dataField.DeclaringType}, FieldType={dataField.FieldType}.");
            }

            var genericArguments = parseMethod.GetGenericArguments();
            if (!parseMethod.IsStatic ||
                parseMethod.DeclaringType != typeof(Package) ||
                parseMethod.Name != nameof(Package.ParseFromRaw) ||
                parseMethod.ContainsGenericParameters ||
                genericArguments.Length != 1 ||
                genericArguments[0] != dataType ||
                parseMethod.ReturnType != expectedReturnType)
            {
                throw new InvalidOperationException(
                    $"Unexpected ParseFromRaw method for {dataType}: {parseMethod}.");
            }
        }

        private static Type[] CreateDistinctDataTypes(int count)
        {
            var result = new List<Type>(count);
            var genericDefinition = typeof(CacheKey<,,>);

            foreach (var familyType in FamilyTypes)
            {
                foreach (var leftType in BaseTypes)
                {
                    foreach (var rightType in BaseTypes)
                    {
                        result.Add(genericDefinition.MakeGenericType(familyType, leftType, rightType));
                        if (result.Count == count) return result.ToArray();
                    }
                }
            }

            throw new InvalidOperationException($"Unable to generate {count} distinct cache key types.");
        }

        private static void ClearCaches()
        {
            foreach (var fieldName in CacheFieldNames)
            {
                var field = GetCacheField(fieldName);
                var freshCache = Activator.CreateInstance(field.FieldType)
                                 ?? throw new InvalidOperationException(
                                     $"Unable to create a fresh cache instance for {fieldName}.");
                field.SetValue(null, freshCache);
            }
        }

        private static int GetCacheCount(string fieldName)
        {
            var cache = GetCache(fieldName);
            var countProperty = cache.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
            if (countProperty?.GetValue(cache) is not int count)
            {
                throw new InvalidOperationException($"Cache {fieldName} does not expose an integer Count property.");
            }

            return count;
        }

        private static object GetCache(string fieldName)
        {
            var field = GetCacheField(fieldName);
            return field.GetValue(null)
                   ?? throw new InvalidOperationException($"Route cache field is null: {fieldName}.");
        }

        private static FieldInfo GetCacheField(string fieldName)
        {
            return typeof(Route).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                   ?? throw new InvalidOperationException($"Route cache field not found: {fieldName}.");
        }

        private sealed class CacheKey<TFamily, TLeft, TRight>
        {
        }

        private sealed class HotFamily
        {
        }

        private sealed class Family0
        {
        }

        private sealed class Family1
        {
        }

        private sealed class Family2
        {
        }

        private sealed class Family3
        {
        }
    }
}

