// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.MemoryCache
{
    public class CacheAddContext
    {
        public CacheAddContext()
        {
            Priority = CachePreservationPriority.Normal;
        }

        /// <summary>
        /// The state passed into GetOrAdd.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// A relative priority for keeping this object when the cache reaches capacity and starts evicting entries.
        /// </summary>
        public CachePreservationPriority Priority { get; set; }

        /// <summary>
        /// Remove this cache entry after the given time elapses.
        /// </summary>
        public TimeSpan? ExpireAfter { get; set; }

        /// <summary>
        /// Automatically refresh this object's lifetime each time it's accessed. This requires ExpireAfter to be set.
        /// </summary>
        public bool SlidingExpiration { get; set; }

        /// <summary>
        /// Expire this object if the given event occures.
        /// </summary>
        public CancellationToken ExpirationTrigger { get; set; }

        /// <summary>
        /// Gets or sets a callback that will be invoked after this item is evicted from the cache.
        /// </summary>
        public Action<string, object, EvictionReason> OnEvicted { get; set; }
    }
}