// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    // LRU
    public class LRUMemoryCacheEvictionStrategy : DefaultMemoryCacheEvictionStrategy
    {
        private readonly int MaximumEntries;

        public LRUMemoryCacheEvictionStrategy(int maximumEntries)
        {
            MaximumEntries = maximumEntries;
        }

        public override IEnumerable<CacheEntry> GetEntriesToEvict(IEnumerable<CacheEntry> entries, DateTimeOffset now)
        {
            var expiredEntries = base.GetEntriesToEvict(entries, now);
            var removalTarget = entries.Count() - MaximumEntries;

            if (removalTarget <= expiredEntries.Count())
            {
                return expiredEntries;
            }

            var addtionalEntriesToEvict = new List<CacheEntry>(expiredEntries);

            foreach (var entry in entries.OrderBy(e => e.LastAccessed))
            {
                if (addtionalEntriesToEvict.Count > removalTarget)
                {
                    break;
                }
                entry.SetExpired(EvictionReason.Capacity);
                addtionalEntriesToEvict.Add(entry);
            }

            return addtionalEntriesToEvict;
        }
    }
}
