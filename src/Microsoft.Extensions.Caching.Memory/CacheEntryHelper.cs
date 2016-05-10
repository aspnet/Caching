// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETSTANDARD1_3 || NETCORE50
#else
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

using System;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryHelper
    {
#if NETSTANDARD1_3 || NETCORE50
        private static readonly AsyncLocal<CacheEntryStack> _scopes = new AsyncLocal<CacheEntryStack>();

        internal static CacheEntryStack Scopes
        {
            get { return _scopes.Value; }
            set { _scopes.Value = value; }
        }
#else
        private const string CacheEntryDataName = "CacheEntry.Scopes";

        internal static CacheEntryStack Scopes
        {
            get
            {
                var handle = CallContext.LogicalGetData(CacheEntryDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as CacheEntryStack;
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
                return scopes.Peek();
            }
        }

        internal static IDisposable EnterScope(CacheEntry entry)
        {
            var scopes = GetOrCreateScopes();

            var bookmark = new CacheEntryStackBookmark(scopes);
            Scopes = scopes.Push(entry);

            return bookmark;
        }

        static CacheEntryStack GetOrCreateScopes()
        {
            var scopes = Scopes;
            if (scopes == null)
            {
                scopes = CacheEntryStack.Empty;
                Scopes = scopes;
            }

            return scopes;
        }

        sealed class CacheEntryStackBookmark : IDisposable
        {
            readonly CacheEntryStack _bookmark;

            public CacheEntryStackBookmark(CacheEntryStack bookmark)
            {
                _bookmark = bookmark;
            }

            public void Dispose()
            {
                Scopes = _bookmark;
            }
        }
    }

    class CacheEntryStack
    {
        private readonly CacheEntryStack _previous;
        private readonly CacheEntry _entry;

        private CacheEntryStack()
        {
        }

        CacheEntryStack(CacheEntryStack previous, CacheEntry entry)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            _previous = previous;
            _entry = entry;
        }

        public static CacheEntryStack Empty { get; } = new CacheEntryStack();

        public CacheEntryStack Push(CacheEntry c)
        {
            return new CacheEntryStack(this, c);
        }

        public CacheEntry Peek()
        {
            return _entry;
        }
    }
}