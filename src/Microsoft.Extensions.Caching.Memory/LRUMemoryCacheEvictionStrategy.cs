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

        public void Evict(IList<IRetrievedCacheEntry> entries, DateTimeOffset now)
        {
            var removalTarget = entries.Count - MaximumEntries;

            if (removalTarget > 0)
            {
                foreach (var entry in entries.OrderBy(e => e.LastAccessed).Take(removalTarget))
                {
                    entry.SetExpired(EvictionReason.Capacity);
                }
            }
        }
    }
}
