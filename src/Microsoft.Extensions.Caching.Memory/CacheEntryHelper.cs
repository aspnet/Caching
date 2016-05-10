// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETSTANDARD1_3 || NETCORE50
#else
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryHelper
    {
#if NETSTANDARD1_3 || NETCORE50
        private static readonly AsyncLocal<ImmutableStack<CacheEntry>> _scopes = new AsyncLocal<ImmutableStack<CacheEntry>>();

        internal static ImmutableStack<CacheEntry> Scopes
        {
            get { return _scopes.Value; }
            set { _scopes.Value = value; }
        }
#else
        private const string CacheEntryDataName = "CacheEntry.Scopes";

        internal static ImmutableStack<CacheEntry> Scopes
        {
            get
            {
                var handle = CallContext.LogicalGetData(CacheEntryDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as ImmutableStack<CacheEntry>;
            }
            set
            {
                CallContext.LogicalSetData(CacheEntryDataName, new ObjectHandle(value));
            }
        }
#endif

        internal static CacheEntry Current
        {
            get
            {
                var scopes = GetOrCreateScopes();
                return scopes.IsEmpty ? null : scopes.Peek();
            }
        }

        internal static IDisposable EnterScope(CacheEntry entry)
        {
            var scopes = GetOrCreateScopes();

            var bookmark = new ScopesStackBookmark(scopes);
            Scopes = scopes.Push(entry);

            return bookmark;
        }

        static ImmutableStack<CacheEntry> GetOrCreateScopes()
        {
            var scopes = Scopes;
            if (scopes == null)
            {
                scopes = ImmutableStack<CacheEntry>.Empty;
                Scopes = scopes;
            }

            return scopes;
        }

        sealed class ScopesStackBookmark : IDisposable
        {
            readonly ImmutableStack<CacheEntry> _bookmark;

            public ScopesStackBookmark(ImmutableStack<CacheEntry> bookmark)
            {
                _bookmark = bookmark;
            }

            public void Dispose()
            {
                Scopes = _bookmark;
            }
        }
    }
}