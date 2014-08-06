// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNet.MemoryCache
{
    public static class CacheExtensions
    {
        public static object Set(this IMemoryCache cache, string key, object obj)
        {
            return cache.UpdateOrAdd(key, obj, (priorValue, context) => context.State);
        }

        public static object Get(this IMemoryCache cache, string key)
        {
            object value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static object GetOrAdd(this IMemoryCache cache, string key, object obj)
        {
            return cache.GetOrAdd(key, obj, context => obj);
        }

        public static object Remove(this IMemoryCache cache, string key)
        {
            object removedValue = null;
            cache.TryRemove(key, state: null, validator: (state, value) =>
            {
                removedValue = value;
                return true;
            });
            return removedValue;
        }

        public static bool Contains(this IMemoryCache cache, string key)
        {
            return cache.Keys.Contains(key, StringComparer.Ordinal);
        }
        public static T Set<T>(this IMemoryCache cache, string key, T obj)
        {
            return (T)cache.Set(key, (object)obj);
        }

        public static T Get<T>(this IMemoryCache cache, string key)
        {
            T value = default(T);
            cache.TryGetValue<T>(key, out value);
            return value;
        }

        public static bool TryGetValue<T>(this IMemoryCache cache, string key, out T value)
        {
            object obj = null;
            if (cache.TryGetValue(key, out obj))
            {
                value = (T)obj;
                return true;
            }
            value = default(T);
            return false;
        }

        public static T GetOrAdd<T>(this IMemoryCache cache, string key, object state, Func<CacheAddContext, T> create)
        {
            return (T)cache.GetOrAdd(key, state, context =>
            {
                return (object)create(context);
            });
        }

        public static T UpdateOrAdd<T>(this IMemoryCache cache, string key, object state, Func<T, CacheAddContext, T> update)
        {
            return (T)cache.UpdateOrAdd(key, state, (priorValue, context) =>
            {
                return (object)update((T)priorValue, context);
            });
        }

        public static T Remove<T>(this IMemoryCache cache, string key)
        {
            return (T)cache.Remove(key);
        }

        public static bool TryRemove<T>(this IMemoryCache cache, string key, object state, Func<object, T, bool> validator)
        {
            return cache.TryRemove(key, state, (subState, value) =>
            {
                return validator(subState, (T)value);
            });
        }

        public static bool TryRemove<S, T>(this IMemoryCache cache, string key, S state, Func<S, T, bool> validator)
        {
            return cache.TryRemove<T>(key, (object)state, (subState, value) =>
            {
                return validator((S)subState, value);
            });
        }
    }
}