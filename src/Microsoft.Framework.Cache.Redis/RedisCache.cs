﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Framework.Cache.Distributed;
using StackExchange.Redis;

namespace Microsoft.Framework.Cache.Redis
{
    public class RedisCache : IDistributedCache
    {
        // KEYS[1] = = key
        // ARGV[1] = absolute-expiration - ticks as long (-1 for none)
        // ARGV[2] = sliding-expiration - ticks as long (-1 for none)
        // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
        // ARGV[4] = data - byte[]
        // this order should not change LUA script depends on it
        private const string SetScript = (@" 
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3]) 
                end
                return 1");
        private const string AbsoluteExpirationKey = "absexp";
        private const string SlidingExpirationKey = "sldexp";
        private const string DataKey = "data";
        private const long NotPresent = -1;

        private readonly ConnectionMultiplexer _connection;
        private readonly IDatabase _cache;

        private readonly string _instance;

        public RedisCache([NotNull] string configuration, string instanceName)
        {
            // TODO: This may be slow and error prone, should we do it in the constructor?
            _connection = ConnectionMultiplexer.Connect(configuration);
            _cache = _connection.GetDatabase();
            // This allows partitioning a single backend cache for use with multiple apps/services.
            _instance = instanceName ?? string.Empty;
        }

        public byte[] Set([NotNull] string key, object state, [NotNull] Func<ICacheContext, byte[]> create)
        {
            var context = new CacheContext(key) { State = state };
            var value = create(context);
            var result = _cache.ScriptEvaluate(SetScript, new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                    context.AbsoluteExpiration?.Ticks ?? NotPresent,
                    context.SlidingExpiration?.Ticks ?? NotPresent,
                    context.GetExpirationInSeconds() ?? NotPresent,
                    value
                });
            // TODO: Error handling
            return value;
        }

        public bool TryGetValue([NotNull] string key, out byte[] value)
        {
            value = GetAndRefresh(key, getData: true);
            return value != null;
        }

        public void Refresh([NotNull] string key)
        {
            var ignored = GetAndRefresh(key, getData: false);
        }

        private byte[] GetAndRefresh(string key, bool getData)
        {
            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            RedisValue[] results;
            if (getData)
            {
                results = _cache.HashMemberGet(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = _cache.HashMemberGet(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey);
            }
            // TODO: Error handling
            if (results.Length >= 2)
            {
                // Note we always get back two results, even if they are all null.
                // These operations will no-op in the null scenario.
                DateTimeOffset? absExpr;
                TimeSpan? sldExpr;
                MapMetadata(results, out absExpr, out sldExpr);
                Refresh(key, absExpr, sldExpr);
            }
            if (results.Length >= 3)
            {
                return results[2];
            }
            return null;
        }

        private void MapMetadata(RedisValue[] results, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingExpiration)
        {
            absoluteExpiration = null;
            slidingExpiration = null;
            long? absoluteExpirationTicks = (long?)results[0];
            if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NotPresent)
            {
                absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
            }
            long? slidingExpirationTicks = (long?)results[1];
            if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NotPresent)
            {
                slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
            }
        }

        private void Refresh([NotNull] string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
        {
            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            TimeSpan? expr = null;
            if (sldExpr.HasValue)
            {
                if (absExpr.HasValue)
                {
                    var relExpr = absExpr.Value - DateTimeOffset.Now;
                    expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
                }
                else
                {
                    expr = sldExpr;
                }
                _cache.KeyExpire(_instance + key, expr);
                // TODO: Error handling
            }
        }

        public void Remove([NotNull] string key)
        {
            _cache.KeyDelete(_instance + key);
            // TODO: Error handling
        }
    }
}