// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Xunit;

namespace Microsoft.Framework.Caching.Memory
{
    public class MemoryCacheSetAndRemoveTests
    {
        private IMemoryCache CreateCache()
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                CompactOnMemoryPressure = false,
            })
            {
                RunCleanupInBackgroundThread = false
            };
        }

        [Fact]
        public void GetMissingKeyReturnsFalseOrNull()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Get(key);
            Assert.Null(result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void SetAndGetReturnsObject()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            result = cache.Get(key);
            Assert.Same(obj, result);
        }

        [Fact]
        public void SetAndGetWorksWithCaseSensitiveKeys()
        {
            var cache = CreateCache();
            var obj = new object();
            string key1 = "myKey";
            string key2 = "Mykey";

            var result = cache.Set(key1, obj);
            Assert.Same(obj, result);

            result = cache.Get(key1);
            Assert.Same(obj, result);

            result = cache.Get(key2);
            Assert.Null(result);
        }

        [Fact]
        public void SetAlwaysOverwrites()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            var obj2 = new object();
            result = cache.Set(key, obj2);
            Assert.Same(obj2, result);

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }

        [Fact]
        public void SetOverwritesAndInvokesCallbacks()
        {
            var cache = CreateCache();
            var value1 = new object();
            string key = "myKey";
            var callback1Status = new CallbackStatus();
            var callback2Status = new CallbackStatus();

            var options1 = new MemoryCacheEntryOptions();
            options1.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(subValue, value1);
                    Assert.Equal(EvictionReason.Replaced, reason);
                    var localcallbackStatus = (CallbackStatus)state;
                    localcallbackStatus.Invoked = true;
                },
                State = callback1Status
            });

            var result = cache.Set(key, value1, options1);
            Assert.Same(value1, result);

            var value2 = new object();
            var options2 = new MemoryCacheEntryOptions();
            options2.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    // Shouldn't be invoked.
                    var localcallbackStatus = (CallbackStatus)state;
                    localcallbackStatus.Invoked = true;
                },
                State = callback2Status
            });
            result = cache.Set(key, value2, options2);
            Assert.Same(value2, result);
            Assert.True(callback1Status.Invoked, "Callback1");
            Assert.False(callback2Status.Invoked, "Callback2");

            result = cache.Get(key);
            Assert.Same(value2, result);

            Assert.False(callback2Status.Invoked, "Callback2");
        }

        [Fact]
        public void RemoveRemoves()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void RemoveRemovesAndInvokesCallback()
        {
            var cache = CreateCache();
            var value = new object();
            string key = "myKey";
            var callbackStatus = new CallbackStatus();

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, subValue);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localcallbackStatus = (CallbackStatus)state;
                    localcallbackStatus.Invoked = true;
                },
                State = callbackStatus
            });
            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            cache.Remove(key);
            Assert.True(callbackStatus.Invoked, "Callback");

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void RemoveAndReAddFromCallbackWorks()
        {
            var cache = CreateCache();
            var value = new object();
            var obj2 = new object();
            string key = "myKey";
            var callbackStatus = new CallbackStatus();

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(subValue, value);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localcallbackStatus = (CallbackStatus)state;
                    cache.Set(key, obj2);
                    localcallbackStatus.Invoked = true;
                },
                State = callbackStatus
            });

            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            cache.Remove(key);
            Assert.True(callbackStatus.Invoked, "Callback");

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }

        class CallbackStatus
        {
            public bool Invoked { get; set; }
        }
    }
}