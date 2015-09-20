using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Framework.Caching.MongoDB
{
    internal class MongoDBCacheEntry
    {
        [BsonId]
        public string Key { get; set; }

        [BsonElement("cat")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("dat")]
        public byte[] CacheData { get; set; }

        [BsonElement("acc")]
        public DateTime LastAccess { get; set; }

        [BsonElement("eat")]
        public DateTime ExpireAt { get; set; }

        [BsonElement("sex")]
        public TimeSpan SlidingExpiration { get; set; } // TODO: seconds?

        [BsonElement("sat")]
        public DateTime SlidingExpireAt { get; set; }
    }
}
