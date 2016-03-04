// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Redis distributed cache related services in an
    /// <see cref="IServiceCollection" />.
    /// </summary>
    public static class RedisCacheServicesExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns> 
        public static IServiceCollection AddRedisCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<RedisCacheOptions>();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, RedisCache>());
            return services;
        }
    }
}
