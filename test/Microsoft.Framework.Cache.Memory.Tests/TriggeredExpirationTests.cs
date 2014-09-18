﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Framework.Cache.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Framework.Cache.Memory
{
    public class TriggeredExpirationTests
    {
        public static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(1);

        [Fact]
        public void SetWithTriggerRegistersForNotificaiton()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.NotNull(trigger.Registration);
            Assert.NotNull(trigger.Registration.RegisteredCallback);
            Assert.NotNull(trigger.Registration.RegisteredState);
            Assert.False(trigger.Registration.Disposed.WaitOne(CallbackTimeout));
        }

        [Fact]
        public void SetWithLazyTriggerDoesntRegisterForNotification()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
        }

        [Fact]
        public void FireTriggerRemovesItem()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });

            trigger.Fire();

            var found = cache.TryGetValue(key, out obj);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");
        }

        [Fact]
        public void ExpiredLazyTriggerRemovesItemOnNextAccess()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            var found = cache.TryGetValue(key, out obj);
            Assert.True(found);

            trigger.IsExpired = true;

            found = cache.TryGetValue(key, out obj);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");
        }

        [Fact]
        public void ExpiredLazyTriggerRemovesItemInBackground()
        {
            var clock = new TestClock();
            var cache = new MemoryCache(clock, listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            var found = cache.TryGetValue(key, out obj);
            Assert.True(found);

            clock.Add(TimeSpan.FromMinutes(2));
            trigger.IsExpired = true;
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");

            found = cache.TryGetValue(key, out obj);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesTriggerRegistration()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            cache.Remove(key);

            Assert.NotNull(trigger.Registration);
            Assert.True(trigger.Registration.Disposed.WaitOne(CallbackTimeout));
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");
        }

        [Fact]
        public void AddExpiredTriggerPreventsCaching()
        {
            var cache = new MemoryCache(new TestClock(), listenForMemoryPressure: false);
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { IsExpired = true };
            var result = cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            Assert.Same(obj, result); // The created item should be returned, but not cached.

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.False(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }
    }
}