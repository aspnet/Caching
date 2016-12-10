// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;
using Xunit;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CompactTests
    {
        private MemoryCache CreateCache(ISystemClock clock = null)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                CompactOnMemoryPressure = false,
            });
        }

        private MemoryCache CreateCacheCompactOnMemoryPressure(ISystemClock clock = null)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                CompactOnMemoryPressure = true,
            });
        }

        private MemoryCache CreateCacheCompactOnMemoryPressureCustom(ISystemClock clock = null)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                CompactOnMemoryPressure = true,
                CustomCompactOnMemoryPressureDelegate = CustomDelegateCompact
            });
        }


        private void CustomDelegateCompact(MemoryCache cache)
        {
            if(cache.Count > 100)
            {
                cache.Compact(0.5);
            }
        }


        [Fact]
        public void CompactCacheOnMemoryPressureDefault()
        {
            var cache = CreateCacheCompactOnMemoryPressure();
            int numberM = 100;
            for (int i = 0; i < numberM; i++)
            {
                cache.Set<int>(i.ToString(), i);
            }

            Assert.True(cache.Count == numberM);
            int numberOfGen2 = GC.CollectionCount(2);

            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(1, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.WaitForPendingFinalizers();

            int numberOfGen2B = GC.CollectionCount(2);

            for (int i = 0; i < numberOfGen2B-numberOfGen2; i++)
            {
                numberM = numberM - (int)((double) numberM * 0.1);
            }

            Assert.True(cache.Count <= numberM);
        }


        [Fact]
        public void CompactCacheOnMemoryPressureCustom()
        {
            var cache = CreateCacheCompactOnMemoryPressureCustom();
            int numberM = 100;
            int number2M = 200;
            for (int i = 0; i < numberM; i++)
            {
                cache.Set<int>(i.ToString(), i);
            }

            Assert.True(cache.Count == numberM);

            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(1, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.WaitForPendingFinalizers();

            Assert.True(cache.Count == numberM);

            for (int i = numberM; i < number2M; i++)
            {
                cache.Set<int>(i.ToString(), i);
            }

            Assert.True(cache.Count == number2M);

            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(1, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(100);
            GC.WaitForPendingFinalizers();

            Assert.True(cache.Count <= numberM);


        }


        [Fact]
        public void CompactEmptyNoOps()
        {
            var cache = CreateCache();
            cache.Compact(0.10);
        }

        [Fact]
        public void Compact100PercentClearsAll()
        {
            var cache = CreateCache();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            Assert.Equal(2, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Compact100PercentClearsAllButNeverRemoveItems()
        {
            var cache = CreateCache();
            cache.Set("key1", "Value1", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
            cache.Set("key2", "Value2", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(2, cache.Count);
            Assert.Equal("Value1", cache.Get("key1"));
            Assert.Equal("Value2", cache.Get("key2"));
        }

        [Fact]
        public void CompactPrioritizesLowPriortyItems()
        {
            var cache = CreateCache();
            cache.Set("key1", "Value1", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Low));
            cache.Set("key2", "Value2", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Low));
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.5);
            Assert.Equal(2, cache.Count);
            Assert.Equal("value3", cache.Get("key3"));
            Assert.Equal("value4", cache.Get("key4"));
        }

        [Fact]
        public void CompactPrioritizesLRU()
        {
            var testClock = new TestClock();
            var cache = CreateCache(testClock);
            cache.Set("key1", "value1");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key2", "value2");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key3", "value3");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.90);
            Assert.Equal(1, cache.Count);
            Assert.Equal("value4", cache.Get("key4"));
        }
    }
}