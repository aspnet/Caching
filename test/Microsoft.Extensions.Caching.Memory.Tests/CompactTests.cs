// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CompactTests
    {
        private MemoryCache CreateCache(ISystemClock clock = null)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
            });
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

        [Fact]
        public void DoNotAddWhenMaximumEntriesCountExceeded()
        {
            var testClock = new TestClock();
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                Clock = testClock,
                EntryCountLimit = 19
            });

            for (var i = 0; i < 19; i++)
            {
                cache.Set($"key{i}", $"value{i}");
                testClock.Add(TimeSpan.FromSeconds(1));
            }

            // There should be 19 items in the cache
            Assert.Equal(19, cache.Count);

            cache.Set("key19", "value19");

            // Wait 1 second for compaction to complete
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // There should be 18 items in the cache, the new entry isn't added and the oldest is evicted
            Assert.Equal(18, cache.Count);
            Assert.Null(cache.Get("key19"));
            Assert.Null(cache.Get("key0"));
        }

        [Fact]
        public void NoCompactionWhenNoMaximumEntriesCountSpecified()
        {
            var cache = CreateCache();

            for (var i = 0; i < 19; i++)
            {
                cache.Set($"key{i}", $"value{i}");
            }

            // There should be 19 items in the cache
            Assert.Equal(19, cache.Count);

            cache.Set("key19", "value19");

            // There should be 10 items in the cache
            Assert.Equal(20, cache.Count);
        }
    }
}