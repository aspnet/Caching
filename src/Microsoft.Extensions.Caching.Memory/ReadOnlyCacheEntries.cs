// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class ReadOnlyCacheEntries : IReadOnlyCollection<IReadOnlyCacheEntry>
    {
        private readonly ConcurrentDictionary<object, IReadOnlyCacheEntry> _entries;

        internal ReadOnlyCacheEntries(ConcurrentDictionary<object, IReadOnlyCacheEntry> entries)
        {
            _entries = entries;
        }

        public int Count  => _entries.Count;

        public IEnumerator<IReadOnlyCacheEntry> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return entry.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}