
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace MemoryCacheSample
{
    class CapacityMemoryCacheEvictionTrigger : IMemoryCacheEvictionTrigger
    {
        private Func<int> _evictionCallback;
        private int _entryLimit;
        private object _evictionlock = new object();

        public CapacityMemoryCacheEvictionTrigger(int entryLimit)
        {
            _entryLimit = entryLimit;
        }

        public void Dispose() { }

        public void Resume(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries)
        {
            if (entries.Count > _entryLimit)
            {
                Task.Factory.StartNew(
                    state => StartEviction((IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>>)state),
                    entries,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
        }

        public void SetEvictionCallback(Func<int> evictionCallback)
        {
            _evictionCallback = evictionCallback;
        }

        private void StartEviction(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries)
        {
            // Don't run too often
            if (Monitor.TryEnter(_evictionlock))
            {
                try
                {
                    _evictionCallback();
                }
                finally
                {
                    Monitor.Exit(_evictionlock);
                }
            }
        }
    }
}
