using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.Caching.MongoDB
{
    public class MongoDBCacheOptions : IOptions<MongoDBCacheOptions>
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";

        public string Database { get; set; } = "caching";

        public string Collection { get; set; } = "cache";

        MongoDBCacheOptions IOptions<MongoDBCacheOptions>.Value => this;
        public ISystemClock Clock { get; set; }
    }
}
