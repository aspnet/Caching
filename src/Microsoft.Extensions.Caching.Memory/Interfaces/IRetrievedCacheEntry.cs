// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Represents an entry retrieved from the <see cref="IMemoryCache"/> implementation.
    /// </summary>
    public interface IRetrievedCacheEntry
    {
        /// <summary>
        /// Gets the key of the cache entry.
        /// </summary>
        object Key { get; }

        /// <summary>
        /// Gets the value of the cache entry.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Gets the absolute expiration date of the cache entry.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; }

        /// <summary>
        /// Gets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        TimeSpan? SlidingExpiration { get; }

        DateTimeOffset LastAccessed { get; }

        bool IsExpired { get; }

        bool CheckExpired(DateTimeOffset utcNow);

        void SetExpired(EvictionReason reason);
    }
}