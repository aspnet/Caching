// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    public class DefaultMemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        public virtual IEnumerable<CacheEntry> GetEntriesToEvict(IEnumerable<CacheEntry> entries, DateTimeOffset now)
        {
            List<CacheEntry> entriesToEvict = null;

            foreach (var entry in entries)
            {
                if (entry.CheckExpired(now))
                {
                    if (entriesToEvict == null)
                    {
                        entriesToEvict = new List<CacheEntry>();
                    }

                    entriesToEvict.Add(entry);
                }
            }

            return entriesToEvict ?? Enumerable.Empty<CacheEntry>();
        }
    }
}
