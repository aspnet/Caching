﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Cache.Memory
{
    public static class CacheExtensions
    {
        public static object Set(this IMemoryCache cache, string key, object obj)
        {
            return cache.Set(key, obj, context => context.State);
        }

        public static T Set<T>(this IMemoryCache cache, string key, T obj)
        {
            return (T)cache.Set(key, (object)obj);
        }

        public static object Set(this IMemoryCache cache, string key, Func<ICacheAddContext, object> create)
        {
            return cache.Set(key, state: null, create: create);
        }

        public static T Set<T>(this IMemoryCache cache, string key, Func<ICacheAddContext, T> create)
        {
            return (T)cache.Set(key, create, context =>
            {
                var myCreate = (Func<ICacheAddContext, T>)context.State;
                return (object)myCreate(context);
            });
        }

        public static T Set<T>(this IMemoryCache cache, string key, object state, Func<ICacheAddContext, T> create)
        {
            return (T)cache.Set(key, state, context =>
            {
                return (object)create(context);
            });
        }

        public static object Get(this IMemoryCache cache, string key)
        {
            object value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static T Get<T>(this IMemoryCache cache, string key)
        {
            T value = default(T);
            cache.TryGetValue<T>(key, out value);
            return value;
        }

        public static bool TryGetValue<T>(this IMemoryCache cache, string key, out T value)
        {
            if (cache.TryGetValue(key, out object obj))
            {
                value = (T)obj;
                return true;
            }
            value = default(T);
            return false;
        }

        public static object GetOrAdd(this IMemoryCache cache, string key, object state, Func<ICacheAddContext, object> create)
        {
            if (cache.TryGetValue(key, out var obj))
            {
                return obj;
            }
            return cache.Set(key, state, create);
        }

        public static T GetOrAdd<T>(this IMemoryCache cache, string key, Func<ICacheAddContext, T> create)
        {
            if (cache.TryGetValue(key, out T obj))
            {
                return obj;
            }
            return cache.Set(key, create);
        }

        public static T GetOrAdd<T>(this IMemoryCache cache, string key, object state, Func<ICacheAddContext, T> create)
        {
            if (cache.TryGetValue(key, out T obj))
            {
                return obj;
            }
            return cache.Set(key, state, create);
        }

        public static object GetOrAddAndLink(this IMemoryCache cache, string key, object state, Func<ICacheAddContext, object> create, out IExpirationTrigger link)
        {
            if (cache.TryGetValueAndLink(key, out var result, out link))
            {
                return result;
            }
            return cache.SetAndLink(key, state, create, out link);
        }
    }
}