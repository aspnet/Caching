// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    // Just remove expired entries
    public class DefaultMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        public IEnumerable<CacheEntry> GetEntriesToEvict(IEnumerable<CacheEntry> entries, DateTimeOffset now)
        {
            var entriesToEvict = new List<CacheEntry>();

            foreach (var entry in entries)
            {
                if (entry.CheckExpired(now))
                {
                    entriesToEvict.Add(entry);
                }
            }

            return entriesToEvict;
        }
    }
}
