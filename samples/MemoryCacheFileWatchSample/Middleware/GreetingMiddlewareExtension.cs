// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace MemoryCacheFileWatchSample.Middleware
{
    public  static class GreetingMiddlewareExtension
    {
        public static IApplicationBuilder UseGreetingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GreetingMiddleware>();
        }
    }
}
