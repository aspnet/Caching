// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.Cache.Memory.Infrastructure;

namespace Microsoft.Framework.Cache.Distributed
{
    public class LocalCache : IDistributedCache
    {
        private readonly MemoryCache _memCache;

        public LocalCache()
        {
            _memCache = new MemoryCache();
        }

        public LocalCache(ISystemClock clock, bool listenForMemoryPressure)
        {
            _memCache = new MemoryCache(clock, listenForMemoryPressure);
        }

        public byte[] Set(string key, object state, Func<ICacheContext, byte[]> create)
        {

            return _memCache.Set<byte[]>(key, state, context =>
            {
                var subContext = new LocalContextWrapper(context);
                return create(subContext);
            });
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _memCache.TryGetValue(key, out value);
        }

        public void Refresh(string key)
        {
            object value;
            _memCache.TryGetValue(key, out value);
        }

        public void Remove(string key)
        {
            _memCache.Remove(key);
        }
    }
}