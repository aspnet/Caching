
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory.Tests
{
    public class ContainsTests
    {
        [Fact]
        public void CacheContainsReturnTrue()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("key", "value");
            var result = cache.Contains("key");

            Assert.True(result);
        }

        [Fact]
        public void CacheNoContainsReturnFalse()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Remove("key");
            var result = cache.Contains("key");

            Assert.False(result);
        }
    }
}
