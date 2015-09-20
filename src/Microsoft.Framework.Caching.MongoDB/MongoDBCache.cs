using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using MongoDB.Driver;

namespace Microsoft.Framework.Caching.MongoDB
{
    public class MongoDBCache : IDistributedCache
    {
        private readonly MongoDBCacheOptions _options;
        private readonly ISystemClock _clock;

        private IMongoClient _client;
        private IMongoCollection<MongoDBCacheEntry> _collection;

        /// <exception cref="ArgumentNullException"><paramref name="optionsAccessor"/> is <see langword="null" />.</exception>
        public MongoDBCache(IOptions<MongoDBCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;
            _clock = _options.Clock ?? new SystemClock();
        }

        public void Connect()
        {
            ConnectAsync().Wait();
        }

        public async Task ConnectAsync()
        {
            var client = _client;
            if (client != null) return;

            _client = client = new MongoClient(_options.ConnectionString);
            var database = client.GetDatabase(_options.Database);
            var collection = _collection = database.GetCollection<MongoDBCacheEntry>(_options.Collection);

            // Create the index to expire on the "expire at" value
            await collection.Indexes.CreateOneAsync(
                Builders<MongoDBCacheEntry>.IndexKeys.Ascending(x => x.ExpireAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromSeconds(0)
                });

            // Create the index to expire on the "sliding expiration" value
            await collection.Indexes.CreateOneAsync(
                Builders<MongoDBCacheEntry>.IndexKeys.Ascending(x => x.SlidingExpireAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromSeconds(0)
                });
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return GetAsync(key).Result;
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public async Task<byte[]> GetAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await ConnectAsync();

            var data = await _collection.FindOneAndUpdateAsync<MongoDBCacheEntry, MongoDBCacheEntry>(x => x.Key == key,
                Builders<MongoDBCacheEntry>.Update.Set(x => x.LastAccess, DateTime.UtcNow),
                new FindOneAndUpdateOptions<MongoDBCacheEntry>
                {
                    ReturnDocument = ReturnDocument.After,
                    Projection = Builders<MongoDBCacheEntry>.Projection.Include(x => x.CacheData)
                });

            return data?.CacheData;
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            SetAsync(key, value, options).Wait();
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="MemoryCacheEntryOptions.AbsoluteExpiration"/> was in the past.</exception>
        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var update = Builders<MongoDBCacheEntry>.Update
                .Set(x => x.CacheData, value);

            // determine absolute expiration time
            var now = _clock.UtcNow;
            var expireAt = CalculateExpireAt(options, now);

            // determine the sliding expiration value
            var slidingExpireAt = expireAt;
            if (options.SlidingExpiration.HasValue)
            {
                slidingExpireAt = now.Add(options.SlidingExpiration.Value);

                // this value is only required if we actually do sliding expiration
                update = update.Set(x => x.SlidingExpiration, options.SlidingExpiration.Value);
            }

            // the begin the update statement
            update = update
                .Set(x => x.CreatedAt, now.UtcDateTime)
                .Set(x => x.ExpireAt, expireAt.UtcDateTime)
                .Set(x => x.SlidingExpireAt, slidingExpireAt.UtcDateTime);

            // Configure an upsert and store
            var updateOptions = new FindOneAndUpdateOptions<MongoDBCacheEntry, string>
            {
                IsUpsert = true,
                // we actually don't need anything back, this is just to keep the data returned small
                Projection = Builders<MongoDBCacheEntry>.Projection.Include(x => x.Key)
            };

            await ConnectAsync();
            await _collection.FindOneAndUpdateAsync(x => x.Key == key,
                update,
                updateOptions);
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public void Refresh(string key)
        {
            RefreshAsync(key).Wait();
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public async Task RefreshAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // refreshing is nasty because we need a roundtrip to Mongo
            // to obtain the sliding expiration value
            var cursor = await _collection.FindAsync(x => x.Key == key,
                new FindOptions<MongoDBCacheEntry>
                {
                    Limit = 1,
                    Projection = Builders<MongoDBCacheEntry>.Projection
                    .Include(x => x.ExpireAt)
                    .Include(x => x.SlidingExpiration)
                });

            if (!await cursor.MoveNextAsync())
            {
                return;
            }

            var entry = cursor.Current.First();

            var now = _clock.UtcNow;
            var sex = entry.SlidingExpiration;
            var sat = sex == default(TimeSpan) ? entry.ExpireAt : now.Add(sex).UtcDateTime;

            // now we just recalculate the key and store
            var update = Builders<MongoDBCacheEntry>.Update
                .Set(x => x.LastAccess, now.UtcDateTime)
                .Set(x => x.SlidingExpireAt, sat);

            await _collection.FindOneAndUpdateAsync(x => x.Key == key, update);
        }

        public void Remove(string key)
        {
            RemoveAsync(key).Wait();
        }

        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null" />.</exception>
        public Task RemoveAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _collection.DeleteOneAsync(x => x.Key == key);
        }

        /// <summary>
        /// Calculates the expiration time based on the current time
        /// </summary>
        /// <param name="options">The expiration options</param>
        /// <param name="now">The current time</param>
        /// <returns>The absolute expiration time</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="MemoryCacheEntryOptions.AbsoluteExpiration"/> was in the past.</exception>
        private static DateTimeOffset CalculateExpireAt(DistributedCacheEntryOptions options, DateTimeOffset now)
        {
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                return now.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value < now)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                        options.AbsoluteExpiration.Value,
                        "The absolute expiration value must be in the future.");
                }
                return options.AbsoluteExpiration.Value;
            }

            return DateTimeOffset.MaxValue;
        }
    }
}
