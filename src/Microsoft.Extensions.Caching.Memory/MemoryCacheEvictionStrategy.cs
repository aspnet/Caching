// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        public int Evict(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries, DateTimeOffset utcNow)
        {
            var expiredEntries = 0;

            foreach (var entry in entries)
            {
                if (entry.Value.CheckExpired(utcNow))
                {
                    expiredEntries++;
                }
            }

            return expiredEntries;
        }
    }
}
