// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Framework.Cache.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Framework.Cache.Memory.Tests
{
    public class MemoryCacheLinkedKeyTests
    {
        public static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(1);

        [Fact]
        public void KeyExpiresWhenSubKeyRemoved()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            var obj = new object();
            var state = new object();
            string key = "myKey";
            string subKey = "otherKey";
            var callbackInvoked = new ManualResetEvent(false);

            var result = cache.Set(key, context =>
            {
                var intermediate = cache.SetAndLink(subKey, state, subContext =>
                {
                    return 1;
                }, out var keyTrigger);
                context.AddExpirationTrigger(keyTrigger);

                context.RegisterPostEvictionCallback((k, value, reason, s) =>
                {
                    callbackInvoked.Set();
                }, state);

                // Create an aggregate value
                return intermediate.ToString() + ";Extra data";
            });

            Assert.Equal("1;Extra data", result);

            cache.Remove(subKey);

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void KeyExpiresWhenSubKeyTimesOut()
        {
            var clock = new TestClock();
            var cache = new MemoryCache(clock, listenForMemoryPressure: false);
            var obj = new object();
            var state = new object();
            string key = "myKey";
            string subKey = "otherKey";
            var callbackInvoked = new ManualResetEvent(false);

            var result = cache.Set(key, context =>
            {
                var intermediate = cache.SetAndLink(subKey, state, subContext =>
                {
                    subContext.SetAbsoluteExpiration(TimeSpan.FromSeconds(10));
                    return 1;
                }, out var keyTrigger);
                context.AddExpirationTrigger(keyTrigger);

                context.RegisterPostEvictionCallback((k, value, reason, s) =>
                {
                    callbackInvoked.Set();
                }, state);

                // Create an aggregate value
                return intermediate.ToString() + ";Extra data";
            });

            Assert.Equal("1;Extra data", result);

            clock.Add(TimeSpan.FromSeconds(15));

            // We notice the timeout when trying to access the key
            var found = cache.TryGetValue(key, out result);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");
        }

        [Fact]
        public void KeyExpiresWhenSubKeyTriggersActively()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            var obj = new object();
            var state = new object();
            string key = "myKey";
            string subKey = "otherKey";
            var callbackInvoked = new ManualResetEvent(false);
            var subTrigger = new BaseExpirationTrigger(supportsActiveExpirationCallbacks: true);

            var result = cache.Set(key, context =>
            {
                var intermediate = cache.SetAndLink(subKey, state, subContext =>
                {
                    subContext.AddExpirationTrigger(subTrigger);
                    return 1;
                }, out var keyTrigger);
                context.AddExpirationTrigger(keyTrigger);

                context.RegisterPostEvictionCallback((k, value, reason, s) =>
                {
                    callbackInvoked.Set();
                }, state);

                // Create an aggregate value
                return intermediate.ToString() + ";Extra data";
            });

            Assert.Equal("1;Extra data", result);

            subTrigger.Expire();

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void KeyExpiresWhenSubKeyTriggersPassively()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            var obj = new object();
            var state = new object();
            string key = "myKey";
            string subKey = "otherKey";
            var callbackInvoked = new ManualResetEvent(false);
            var subTrigger = new BaseExpirationTrigger(supportsActiveExpirationCallbacks: false);

            var result = cache.Set(key, context =>
            {
                var intermediate = cache.SetAndLink(subKey, state, subContext =>
                {
                    subContext.AddExpirationTrigger(subTrigger);
                    return 1;
                }, out var keyTrigger);
                context.AddExpirationTrigger(keyTrigger);

                context.RegisterPostEvictionCallback((k, value, reason, s) =>
                {
                    callbackInvoked.Set();
                }, state);

                // Create an aggregate value
                return intermediate.ToString() + ";Extra data";
            });

            Assert.Equal("1;Extra data", result);

            subTrigger.Expire();

            // We notice the timeout when trying to access the key
            var found = cache.TryGetValue(key, out result);
            Assert.False(found, "Found");

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");
        }
    }
}