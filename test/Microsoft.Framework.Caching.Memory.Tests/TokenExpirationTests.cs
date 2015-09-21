// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Framework.Caching.Memory.Infrastructure;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.Framework.Caching.Memory
{
    public class TokenExpirationTests
    {
        private IMemoryCache CreateCache()
        {
            return CreateCache(new SystemClock());
        }

        private IMemoryCache CreateCache(ISystemClock clock)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                CompactOnMemoryPressure = false,
            });
        }

        [Fact]
        public void SetWithTokenRegistersForNotificaiton()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var token = new TestChangeToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(token));

            Assert.True(token.IsExpiredWasCalled);
            Assert.True(token.ActiveChangeCallbacksWasCalled);
            Assert.NotNull(token.Registration);
            Assert.NotNull(token.Registration.RegisteredCallback);
            Assert.NotNull(token.Registration.RegisteredState);
            Assert.False(token.Registration.Disposed);
        }

        [Fact]
        public void SetWithLazyTokenDoesntRegisterForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var token = new TestChangeToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(token));

            Assert.True(token.IsExpiredWasCalled);
            Assert.True(token.ActiveChangeCallbacksWasCalled);
            Assert.Null(token.Registration);
        }

        [Fact]
        public void FireTokenRemovesItem()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var token = new TestChangeToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(token)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            token.Fire();

            var found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemOnNextAccess()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var token = new TestChangeToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(token)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            token.HasChanged = true;

            found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemInBackground()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var token = new TestChangeToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(token)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            clock.Add(TimeSpan.FromMinutes(2));
            token.HasChanged = true;
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            found = cache.TryGetValue(key, out value);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesTokenRegistration()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var token = new TestChangeToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(token)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            cache.Remove(key);

            Assert.NotNull(token.Registration);
            Assert.True(token.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void AddExpiredTokenPreventsCaching()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var token = new TestChangeToken() { HasChanged = true };
            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(token)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            Assert.Same(value, result); // The created item should be returned, but not cached.

            Assert.True(token.IsExpiredWasCalled);
            Assert.False(token.ActiveChangeCallbacksWasCalled);
            Assert.Null(token.Registration);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }
    }
}