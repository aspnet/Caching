// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.Cache.Memory
{
    [AssemblyNeutral]
    public interface IMemoryCache : IDisposable
    {
        /// <summary>
        /// Create or overwrite an entry in the cache.
        /// </summary>
        /// <param name="key">A string identifying the entry. This is case sensitive.</param>
        /// <param name="state">Application state that will be passed to the creation factory.</param>
        /// <param name="create">A factory that will create and configure the entry.</param>
        /// <returns>The object that was created.</returns>
        object Set(string key, object state, Func<ICacheAddContext, object> create);

        /// <summary>
        /// Create or overwrite an entry in the cache, and registers for an expiration notification.
        /// </summary>
        /// <param name="key">A string identifying the entry. This is case sensitive.</param>
        /// <param name="state">Application state that will be passed to the creation factory.</param>
        /// <param name="create">A factory that will create and configure the entry.</param>
        /// <param name="link">An expiration trigger that will be signaled when the given entry expires.</param>
        /// <returns>The object that was created.</returns>
        object SetAndLink(string key, object state, Func<ICacheAddContext, object> create, out IExpirationTrigger link);

        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">A string identifying the entry. This is case sensitive.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Gets the item associated with this key if present, and registers for an expiration notification.
        /// </summary>
        /// <param name="key">A string identifying the entry. This is case sensitive.</param>
        /// <param name="value">The located value or null.</param>
        /// <param name="link">An expiration trigger that will be signaled when the given entry expires.</param>
        /// <returns>True if the key was found and the callback successfully registered.</returns>
        bool TryGetValueAndLink(string key, out object value, out IExpirationTrigger link);

        /// <summary>
        /// Removes the object associated with the given key.
        /// </summary>
        /// <param name="key">A string identifying the entry. This is case sensitive.</param>
        void Remove(string key);
    }
}