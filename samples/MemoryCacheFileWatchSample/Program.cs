// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace MemoryCacheFileWatchSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            var greeting = "";
            var cacheKey = "cache_key";
            var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Files"));
            var token = fileProvider.Watch("example.txt");

            if (!cache.TryGetValue(cacheKey, out greeting))
            {
               greeting = "Hello world";
               cache.Set(cacheKey, greeting, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                    //Telling the cache to depend on the IChangeToken from watching examples.txt
                    .AddExpirationToken(token)
                    .RegisterPostEvictionCallback(
                    (echoKey, value, reason, substate) =>
                    {
                        Console.Write(echoKey + ": '" + value + "' was evicted due to " + reason);
                    }));
                Console.Write($"{cacheKey} updated from source.");
            }
            else
            {
                Console.Write($"{cacheKey} retrieved from cache.");
            }

            Console.Write(greeting);
        }
    }
}
