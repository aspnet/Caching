// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MemoryCacheFileWatchSample.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace MemoryCacheFileWatchSample
{
    public class GreetingMiddleware
    {
        private readonly IGreetingService _greetingService;
        private readonly ILogger<GreetingMiddleware> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;

        public GreetingMiddleware(RequestDelegate next, IMemoryCache memoryCache, ILogger<GreetingMiddleware> logger, IGreetingService greetingService, IHostingEnvironment env)
        {
            _next = next;
            _memoryCache = memoryCache;
            _logger = logger;
            _greetingService = greetingService;
            _env = env;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var greeting = "";
            var cacheKey = "GreetingMiddleware-Invoke";
            var fileProvider = new PhysicalFileProvider(Path.Combine(_env.ContentRootPath, "Files"));
            var token = fileProvider.Watch("example.txt");

            if(!_memoryCache.TryGetValue(cacheKey, out greeting))
            {
                greeting = _greetingService.Greet("world");
                _memoryCache.Set(cacheKey, greeting, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                    //Telling the cache to depend on the IChangeToken from watching examples.txt
                    .AddExpirationToken(token)
                    .RegisterPostEvictionCallback(
                    (echoKey, value, reason, substate) =>
                    {
                        _logger.LogInformation(echoKey + ": '" + value + "' was evicted due to " + reason);
                    }));
                _logger.LogInformation($"{cacheKey} updated from source.");
            }
            else
            {
                _logger.LogInformation($"{cacheKey} retrieved from cache.");
            }

            return httpContext.Response.WriteAsync(greeting);
        }
    }
}
