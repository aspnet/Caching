// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

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

            while (true)
            {
                if (!cache.TryGetValue(cacheKey, out greeting))
                {
                    greeting = "Hello world";
                    cache.Set(cacheKey, greeting, new MemoryCacheEntryOptions()
                         .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                         //Telling the cache to depend on the IChangeToken from watching examples.txt
                         .AddExpirationToken(fileProvider.Watch("example.txt"))
                         .RegisterPostEvictionCallback(
                         (echoKey, value, reason, substate) =>
                         {
                             Console.WriteLine($"{echoKey} : {value} was evicted due to {reason}");
                         }));
                    Console.WriteLine($"{cacheKey} updated from source.");

                }
                else
                {
                    Console.WriteLine($"{cacheKey} retrieved from cache.");
                }

                Console.WriteLine(greeting);
                Console.ReadKey();
                Console.WriteLine();
            }
        }
    }
}
