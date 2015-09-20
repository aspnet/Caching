// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Xunit;

namespace Microsoft.Framework.Caching.MongoDB
{
    // TODO: Disabled due to CI failure
    // These tests require Redis server to be started on the machine. Make sure to change the value of
    // "RedisTestConfig.RedisPort" accordingly.
    // public
    class TimeExpirationTests
    {
        [Fact]
        public void AbsoluteExpirationInThePastThrows()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
            ExceptionAssert.ThrowsArgumentOutOfRange(
                () =>
                {
                    cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(expected));
                },
                nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                "The absolute expiration value must be in the future.",
                expected.ToString(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void AbsoluteExpirationExpires()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

            byte[] result = cache.Get(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 4 && (result != null); i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                result = cache.Get(key);
            }

            Assert.Null(result);
        }

        [Fact]
        public void AbsoluteSubSecondExpirationExpiresImmidately()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

            var result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeRelativeExpirationThrows()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroRelativeExpirationThrows()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(
                () =>
                {
                    cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
                },
                nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
                "The relative expiration value must be positive.",
                TimeSpan.Zero);
        }

        [Fact]
        public void RelativeExpirationExpires()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 4 && (result != null); i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                result = cache.Get(key);
            }
            Assert.Null(result);
        }

        [Fact]
        public void RelativeSubSecondExpirationExpiresImmediately()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

            var result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeSlidingExpirationThrows()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
            }, nameof(DistributedCacheEntryOptions.SlidingExpiration), "The sliding expiration value must be positive.", TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroSlidingExpirationThrows()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(
                () =>
                {
                    cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.Zero));
                },
                nameof(DistributedCacheEntryOptions.SlidingExpiration),
                "The sliding expiration value must be positive.",
                TimeSpan.Zero);
        }

        [Fact]
        public void SlidingExpirationExpiresIfNotAccessed()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingSubSecondExpirationExpiresImmediately()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(0.25)));

            var result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccess()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = cache.Get(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
        {
            var cache = MongoDBTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(1))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(3)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = cache.Get(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            result = cache.Get(key);
            Assert.Null(result);
        }
    }
}