using System;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;

namespace Microsoft.Framework.Caching.MongoDB
{
    /// <summary>
    /// Extension methods for setting up MongoDB distributed cache related services in an
    /// <see cref="IServiceCollection" />.
    /// </summary>
    public static class MongoDBCacheServicesExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
        public static IServiceCollection AddMongoDbCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, MongoDBCache>());
            return services;
        }
    }
}
