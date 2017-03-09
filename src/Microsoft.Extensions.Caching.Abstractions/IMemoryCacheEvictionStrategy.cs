// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    public interface IMemoryCacheEvictionStrategy<TCacheEntry> where TCacheEntry : ICacheEntry
    {
        // doc comments, bool returns whether any entry was removed
        bool Compact(IEnumerable<TCacheEntry> entries);
    }
}
