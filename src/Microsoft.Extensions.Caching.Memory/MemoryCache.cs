// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// An implementation of <see cref="IMemoryCache"/> using a dictionary to
    /// store its entries.
    /// </summary>
    public partial class MemoryCache : IMemoryCache
    {
        private readonly ConcurrentDictionary<object, IReadOnlyCacheEntry> _entries;
        private bool _disposed;

        // We store the delegates locally to prevent allocations
        // every time a new CacheEntry is created.
        private readonly Action<CacheEntry> _setEntry;
        private readonly Action<CacheEntry> _entryExpirationNotification;

        private readonly ISystemClock _clock;
        private readonly IMemoryCacheEvictionStrategy _evictionStrategy;
        private readonly IMemoryCacheEvictionTrigger _evictionTrigger;

        /// <summary>
        /// Creates a new <see cref="MemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            var options = optionsAccessor.Value;
            _entries = new ConcurrentDictionary<object, IReadOnlyCacheEntry>();
            _setEntry = SetEntry;
            _entryExpirationNotification = EntryExpired;

            _clock = options.Clock ?? new SystemClock();
            _evictionStrategy = options.EvictionStrategy ?? new MemoryCacheEvictionStrategy();
            _evictionTrigger = options.EvictionTrigger ?? new MemoryCacheEvictionTrigger();
            _evictionTrigger.SetEvictionCallback(ExecuteCacheEviction);
        }

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        private ICollection<KeyValuePair<object, IReadOnlyCacheEntry>> EntriesCollection => _entries;

        /// <inheritdoc />
        public ICacheEntry CreateEntry(object key)
        {
            CheckDisposed();

            return new CacheEntry(
                key,
                _setEntry,
                _entryExpirationNotification
            );
        }

        private void SetEntry(CacheEntry entry)
        {
            if (_disposed)
            {
                // No-op instead of throwing since this is called during CacheEntry.Dispose
                return;
            }

            var utcNow = _clock.UtcNow;

            DateTimeOffset? absoluteExpiration = null;
            if (entry.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow + entry.AbsoluteExpirationRelativeToNow;
            }
            else if (entry.AbsoluteExpiration.HasValue)
            {
                absoluteExpiration = entry.AbsoluteExpiration;
            }

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (absoluteExpiration.HasValue)
            {
                if (!entry.AbsoluteExpiration.HasValue || absoluteExpiration.Value < entry.AbsoluteExpiration.Value)
                {
                    entry.AbsoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            IReadOnlyCacheEntry priorEntry;
            if (_entries.TryGetValue(entry.Key, out priorEntry))
            {
                ((CacheEntry)priorEntry).SetExpired(EvictionReason.Replaced);
            }

            if (!entry.CheckExpired(utcNow))
            {
                var entryAdded = false;

                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = _entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = _entries.TryUpdate(entry.Key, entry, priorEntry);

                    if (!entryAdded)
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = _entries.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                if (priorEntry != null)
                {
                    ((CacheEntry)priorEntry).InvokeEvictionCallbacks();
                }
            }
            else
            {
                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    RemoveEntry(priorEntry);
                }
            }

            _evictionTrigger.Resume(new ReadOnlyCacheEntries(_entries));
        }

        /// <inheritdoc />
        public bool TryGetValue(object key, out object result)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            CheckDisposed();

            result = null;
            var utcNow = _clock.UtcNow;
            var found = false;

            IReadOnlyCacheEntry retrievedEntry;
            if (_entries.TryGetValue(key, out retrievedEntry))
            {
                var entry = (CacheEntry)retrievedEntry;

                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (entry.CheckExpired(utcNow) && entry.EvictionReason != EvictionReason.Replaced)
                {
                    // TODO: For efficiency queue this up for batch removal
                    RemoveEntry(entry);
                }
                else
                {
                    found = true;
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    // When this entry is retrieved in the scope of creating another entry,
                    // that entry needs a copy of these expiration tokens.
                    entry.PropagateOptions(CacheEntryHelper.Current);
                }
            }

            _evictionTrigger.Resume(new ReadOnlyCacheEntries(_entries));

            return found;
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            CheckDisposed();
            IReadOnlyCacheEntry entry;
            if (_entries.TryRemove(key, out entry))
            {
                var cacheEntry = (CacheEntry)entry;
                cacheEntry.SetExpired(EvictionReason.Removed);
                cacheEntry.InvokeEvictionCallbacks();
            }

            _evictionTrigger.Resume(new ReadOnlyCacheEntries(_entries));
        }

        private void RemoveEntry(IReadOnlyCacheEntry entry)
        {
            if (EntriesCollection.Remove(new KeyValuePair<object, IReadOnlyCacheEntry>(entry.Key, entry)))
            {
                ((CacheEntry)entry).InvokeEvictionCallbacks();
            }
        }

        private void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            RemoveEntry(entry);
            _evictionTrigger.Resume(new ReadOnlyCacheEntries(_entries));
        }

        private int ExecuteCacheEviction()
        {
            var evictCount = _evictionStrategy.Evict(new ReadOnlyCacheEntries(_entries), _clock.UtcNow); // TODO: anything else eviction strategies need?
            
            foreach (var entry in _entries)
            {
                if (entry.Value.IsExpired)
                {
                    RemoveEntry(entry.Value);
                }
            }

            return evictCount;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                _disposed = true;

                _evictionTrigger.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryCache).FullName);
            }
        }
    }
}