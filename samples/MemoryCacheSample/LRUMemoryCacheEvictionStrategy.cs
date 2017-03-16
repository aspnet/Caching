// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    // TODO: remove this
    public class LRUMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        private readonly int MaximumEntries;

        public LRUMemoryCacheEvictionStrategy(int maximumEntries)
        {
            MaximumEntries = maximumEntries;
        }

        public void Evict(MemoryCache cache, DateTimeOffset utcNow)
        {
            var removalTarget = cache.Count - MaximumEntries;

            if (removalTarget > 0)
            {
                foreach (var entry in cache.OrderBy(e => e.Value.LastAccessed).Take(removalTarget))
                {
                    entry.Value.SetExpired(EvictionReason.Capacity);
                }
            }
        }
    }
}
