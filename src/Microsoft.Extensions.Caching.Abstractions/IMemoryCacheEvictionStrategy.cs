// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    public interface IMemoryCacheEvictionStrategy<TCacheEntry> where TCacheEntry : ICacheEntry
    {
        // doc comments, returns a list of entries entry to evict
        IEnumerable<TCacheEntry> GetEntriesToEvict(IEnumerable<TCacheEntry> entries, DateTimeOffset now);
    }
}
