// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheEvictionStrategy : IMemoryCacheEvictionStrategy
    {
        public void Evict(MemoryCache cache, DateTimeOffset utcNow)
        {
            foreach (var entry in cache)
            {
                entry.Value.CheckExpired(utcNow);
            }
        }
    }
}
