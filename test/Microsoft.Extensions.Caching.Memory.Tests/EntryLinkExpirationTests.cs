// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class EntryLinkExpirationTests
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
        public void SetPopulates_ExpirationTokens_IntoScopedLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            IEntryLink linkScope1;
            using (linkScope1 = cache.CreateLinkingScope())
            {
                Assert.Same(linkScope1, EntryLinkHelpers.ContextLink);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Equal(1, linkScope1.ExpirationTokens.Count());
            Assert.Null(linkScope1.AbsoluteExpiration);
        }

        [Fact]
        public void SetPopulates_AbsoluteExpiration_IntoScopeLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            var time = new DateTimeOffset(2051, 1, 1, 1, 1, 1, TimeSpan.Zero);

            IEntryLink linkScope1;
            using (linkScope1 = cache.CreateLinkingScope())
            {
                Assert.Same(linkScope1, EntryLinkHelpers.ContextLink);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(time));
            }

            Assert.Equal(0, linkScope1.ExpirationTokens.Count());
            Assert.NotNull(linkScope1.AbsoluteExpiration);
            Assert.Equal(time, linkScope1.AbsoluteExpiration);
        }

        [Fact]
        public void TokenExpires_LinkedEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));

                cache.Set(key1, obj, new MemoryCacheEntryOptions());
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenExpires_ParentScopeEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link1 = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions());

                using (var link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenDoesntExpire_SiblingScopeEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link1 = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions());

                using (var link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
                }

                using (var link3 = cache.CreateLinkingScope())
                {
                    cache.Set(key2, obj, new MemoryCacheEntryOptions());
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            expirationToken.Fire();

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
            Assert.True(cache.TryGetValue(key2, out value));
        }

        [Fact]
        public void AbsoluteExpiration_WorksAcrossLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));

                cache.Set(key1, obj, new MemoryCacheEntryOptions());
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            clock.Add(TimeSpan.FromSeconds(10));

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void AbsoluteExpiration_WorksAcrossNestedLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link1 = cache.CreateLinkingScope())
            {
                cache.Set(key1, obj, new MemoryCacheEntryOptions());

                using (var link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key2, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            clock.Add(TimeSpan.FromSeconds(10));

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key2, out value));
        }

        [Fact]
        public void AbsoluteExpiration_DoesntAffectSiblingLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            string key3 = "myKey3";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var link1 = cache.CreateLinkingScope())
            {
                cache.Set(key1, obj, new MemoryCacheEntryOptions());

                using (var link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key2, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));
                }

                using (var link3 = cache.CreateLinkingScope())
                {
                    cache.Set(key3, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(15)));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));
            Assert.Same(obj, cache.Get(key3));

            clock.Add(TimeSpan.FromSeconds(10));

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key2, out value));
            Assert.True(cache.TryGetValue(key3, out value));
        }

        [Fact]
        public void GetWithImplicitLinkPopulatesExpirationTokens()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            Assert.Null(EntryLinkHelpers.ContextLink);

            IEntryLink link;
            using (link = cache.CreateLinkingScope())
            {
                Assert.Same(link, EntryLinkHelpers.ContextLink);
                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Null(EntryLinkHelpers.ContextLink);

            Assert.Equal(1, link.ExpirationTokens.Count());
            Assert.Null(link.AbsoluteExpiration);
        }

        [Fact]
        public void LinkContextsCanNest()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            Assert.Null(EntryLinkHelpers.ContextLink);

            IEntryLink link1;
            IEntryLink link2;
            using (link1 = cache.CreateLinkingScope())
            {
                Assert.Same(link1, EntryLinkHelpers.ContextLink);

                using (link2 = cache.CreateLinkingScope())
                {
                    Assert.Same(link2, EntryLinkHelpers.ContextLink);

                    var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                    cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
                }

                Assert.Same(link1, EntryLinkHelpers.ContextLink);
            }

            Assert.Null(EntryLinkHelpers.ContextLink);
        }

        [Fact]
        public void NestedLinkContextsCanAggregate()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key2 = "myKey2";
            string key3 = "myKey3";

            var expirationToken2 = new TestExpirationToken() { ActiveChangeCallbacks = true };
            var expirationToken3 = new TestExpirationToken() { ActiveChangeCallbacks = true };

            IEntryLink link1 = null;
            IEntryLink link2 = null;

            using (link1 = cache.CreateLinkingScope())
            {
                cache.Set(key2, obj, new MemoryCacheEntryOptions()
                    .AddExpirationToken(expirationToken2)
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));

                using (link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key3, obj, new MemoryCacheEntryOptions()
                        .AddExpirationToken(expirationToken3)
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(15)));
                }
            }

            Assert.Equal(2, link1.ExpirationTokens.Count());
            Assert.NotNull(link1.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(10), link1.AbsoluteExpiration);

            Assert.Equal(1, link2.ExpirationTokens.Count());
            Assert.NotNull(link2.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(15), link2.AbsoluteExpiration);
        }
    }
}