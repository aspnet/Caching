// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.MemoryCache
{
    public interface IMemoryCache : IEnumerable<KeyValuePair<string, object>>, IDisposable
    {
        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the keys for each item in the cache.
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// Retrieves an item with the given key from the cache, or creates and adds it if not preasent.
        /// </summary>
        /// <param name="key">A string identifiing the entry. This is case sensitive.</param>
        /// <param name="state">Application state that will be passed to the createtion factory.</param>
        /// <param name="create">A factory that will create and configure the entry if it is not found.</param>
        /// <returns>The object that was retrieved or created.</returns>
        object GetOrAdd(string key, object state, Func<CacheAddContext, object> create);

        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">A string identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Modifies an existing item in the cache, or creates it if not preasent.
        /// </summary>
        /// <param name="key">A string identifiing the entry. This is case sensitive.</param>
        /// <param name="state">Application state that will be passed to the createtion factory.</param>
        /// <param name="update">A factory that will update the prior value or create a new one.</param>
        /// <returns>The resulting value.</returns>
        object UpdateOrAdd(string key, object state, Func<object, CacheAddContext, object> update);

        /// <summary>
        /// Conditionallay removes the object associated with the given key.
        /// </summary>
        /// <param name="key">A string identifiing the entry. This is case sensitive.</param>
        /// <param name="state">Application state that will be passed to the removal factory.</param>
        /// <param name="validator">A callback that is given the state and value and returns true if that value should be removed.</param>
        /// <returns>True if the key was found and removed.</returns>
        bool TryRemove(string key, object state, Func<object, object, bool> validator);
    }
}