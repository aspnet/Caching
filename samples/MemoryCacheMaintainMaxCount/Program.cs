// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace MemoryCacheMaintainMaxCount
{
    public class Program
    {
        private const string Key = "MyKey";
        private static readonly Random Random = new Random();
        private static MemoryCacheEntryOptions _cacheEntryOptions;

        public static void Main()
        {
            // The of the sample is to demonstrate how to implement the typical memory cache use case:
            // 1. The is background process the feeds to cache.
            // 2. You consider that the cache size should be for example ~ 10000.
            // 3. You consider that entry in the cache has a life time in 6 hours.
            // 4. You expect that the Memory cache will trigger max count check when GC #2 is collected.

            // # Step -1 
            // We a delegate that simple check the number and call compact cache or ...
            int maxCount = 10000;
            Action<MemoryCache> CompactOnMemoryPressureDelegate = (memoryCache) =>
                 {
                     if (memoryCache.Count > 10000)
                     {
                         memoryCache.CompactByRemovalCountTarget(memoryCache.Count - 10000);
                     }
                 };

            // # Step - 2
            // We need a place it into options
            MemoryCacheOptions memoryCacheOptions = new MemoryCacheOptions
            {
                CompactOnMemoryPressure = true,
                CustomCompactOnMemoryPressureDelegate = CompactOnMemoryPressureDelegate
            };

            _cacheEntryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
            }; 
            MemoryCache cache = new MemoryCache(memoryCacheOptions);

            // # Step - 3
            // Fill the cache with 
            for (int i = 0;i < maxCount+5000; i++)
            {
                cache.Set<int>($"Key_{i}", Random.Next(maxCount), _cacheEntryOptions);
            }

            Console.WriteLine($"Cache filled with count{cache.Count}");

            // # Step - 4
            Console.WriteLine($"Make garbage collections");
            Object g2 = new object();
            GC.Collect();
            GC.Collect();
            g2 = null;
            GC.Collect(2);
            Console.WriteLine($"Cache filled with count{cache.Count}");
            Thread.Sleep(15000);
            Console.WriteLine($"Cache after GC collection has filled with count{cache.Count}");
            Console.ReadLine();
            Console.WriteLine("Shutting down");

        }

        private static void SetKey(IMemoryCache cache, string value)
        {
            Console.WriteLine("Setting: " + value);
            cache.Set(Key, value, _cacheEntryOptions);
        }

        private static MemoryCacheEntryOptions GetCacheEntryOptions()
        {
            return new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(7))
                .SetSlidingExpiration(TimeSpan.FromSeconds(3))
                .RegisterPostEvictionCallback(AfterEvicted, state: null);
        }

        private static void AfterEvicted(object key, object value, EvictionReason reason, object state)
        {
            Console.WriteLine("Evicted. Value: " + value + ", Reason: " + reason);
        }

        private static void PeriodicallySetKey(IMemoryCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    SetKey(cache, "A");
                }
            });
        }

        private static void PeriodicallyReadKey(IMemoryCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    if (Random.Next(3) == 0) // 1/3 chance
                    {
                        // Allow values to expire due to sliding refresh.
                        Console.WriteLine("Read skipped, random choice.");
                    }
                    else
                    {
                        Console.Write("Reading...");
                        object result;
                        if (!cache.TryGetValue(Key, out result))
                        {
                            result = cache.Set(Key, "B", _cacheEntryOptions);
                        }
                        Console.WriteLine("Read: " + (result ?? "(null)"));
                    }
                }
            });
        }

        private static void PeriodicallyRemoveKey(IMemoryCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    Console.WriteLine("Removing...");
                    cache.Remove(Key);
                }
            });
        }
    }
}
