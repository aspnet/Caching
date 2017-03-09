// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    // LRU
    public class LRUMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        private readonly int MaximumEntries;

        public LRUMemoryCacheEvictionStrategy(int maximumEntries)
        {
            MaximumEntries = maximumEntries;
        }

        public IEnumerable<CacheEntry> GetEntriesToEvict(IEnumerable<CacheEntry> entries, DateTimeOffset now)
        {
            // Remove expired items first

            var removalTarget = entries.Count() - MaximumEntries;

            if (removalTarget <= 0)
            {
                return Enumerable.Empty<CacheEntry>();
            }

            var entriesToEvict = new List<CacheEntry>();

            foreach (var entry in entries.OrderBy(e => e.LastAccessed))
            {
                if (entriesToEvict.Count > removalTarget)
                {
                    break;
                }
                entry.SetExpired(EvictionReason.Capacity);
                entriesToEvict.Add(entry);
            }

            return entriesToEvict;
        }
    }
}
