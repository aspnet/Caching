// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace MemoryCacheSample
{
    // TODO: remove this
    public class LRUMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        private readonly IMemoryCacheEvictionStrategy _evictExpiredStrategy;
        private readonly int _entryLimit;

        public LRUMemoryCacheEvictionStrategy(int entryLimit)
        {
            _entryLimit = entryLimit;
            _evictExpiredStrategy = new MemoryCacheEvictionStrategy();
        }

        public int Evict(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries, DateTimeOffset utcNow)
        {
            var expiredCount = _evictExpiredStrategy.Evict(entries, utcNow);
            var removalTarget = entries.Count - expiredCount - _entryLimit; // assume underflow is handled

            if (removalTarget <= 0)
            {
                return expiredCount;
            }

            var removedEntries = 0;
            foreach (var entry in entries.OrderBy(e => e.Value.LastAccessed))
            {
                if (!entry.Value.IsExpired)
                {
                    entry.Value.SetExpired(EvictionReason.Capacity);
                    removedEntries++;
                }
                if (removedEntries == removalTarget)
                {
                    break;
                }
            }

            return removalTarget + expiredCount;
        }
    }
}
