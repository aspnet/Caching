// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    public class DefaultMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy<CacheEntry>
    {
        /// Remove at least the given percentage (0.10 for 10%) of the total entries (or estimated memory?), according to the following policy:
        /// 1. Remove all expired items.
        /// 2. Bucket by CacheItemPriority.
        /// 3. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public IEnumerable<CacheEntry> GetEntriesToEvict(IEnumerable<CacheEntry> entries, DateTimeOffset now)
        {
            // For testing, only remove expired entries
            var percentage = 0D;

            var entriesToRemove = new List<CacheEntry>();
            var lowPriEntries = new List<CacheEntry>();
            var normalPriEntries = new List<CacheEntry>();
            var highPriEntries = new List<CacheEntry>();

            // Sort items by expired & priority status
            foreach (var entry in entries)
            {
                if (entry.CheckExpired(now))
                {
                    entriesToRemove.Add(entry);
                }
                else
                {
                    switch (entry.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.NeverRemove:
                            break;
                        default:
                            throw new NotSupportedException("Not implemented: " + entry.Priority);
                    }
                }
            }

            int removalCountTarget = (int)(entries.Count() * percentage);

            ExpirePriorityBucket(removalCountTarget, entriesToRemove, lowPriEntries);
            ExpirePriorityBucket(removalCountTarget, entriesToRemove, normalPriEntries);
            ExpirePriorityBucket(removalCountTarget, entriesToRemove, highPriEntries);

            return entriesToRemove;
        }

        /// Policy:
        /// 1. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        private void ExpirePriorityBucket(int removalCountTarget, List<CacheEntry> entriesToRemove, List<CacheEntry> priorityEntries)
        {
            // Do we meet our quota by just removing expired entries?
            if (removalCountTarget <= entriesToRemove.Count)
            {
                // No-op, we've met quota
                return;
            }
            if (entriesToRemove.Count + priorityEntries.Count <= removalCountTarget)
            {
                // Expire all of the entries in this bucket
                foreach (var entry in priorityEntries)
                {
                    entry.SetExpired(EvictionReason.Capacity);
                }
                entriesToRemove.AddRange(priorityEntries);
                return;
            }

            // Expire enough entries to reach our goal
            // TODO: Refine policy

            // LRU
            foreach (var entry in priorityEntries.OrderBy(entry => entry.LastAccessed))
            {
                entry.SetExpired(EvictionReason.Capacity);
                entriesToRemove.Add(entry);
                if (removalCountTarget <= entriesToRemove.Count)
                {
                    break;
                }
            }
        }
    }
}
