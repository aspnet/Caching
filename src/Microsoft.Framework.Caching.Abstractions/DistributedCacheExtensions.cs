// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
#if DNX451
using System.Runtime.Serialization.Formatters.Binary;
#endif
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using Newtonsoft.Json;

namespace Microsoft.Framework.Caching.Distributed
{
    public static class CacheExtensions
    {
        public static void Set(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            cache.Set(key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            return cache.SetAsync(key, value, new DistributedCacheEntryOptions());
        }

        #region BinaryReader & BinaryWriter

        public static async Task<bool> GetBooleanAsync(this IDistributedCache cache, string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadBoolean();
            }
        }

        public static async Task<char> GetCharAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadChar();
            }
        }

        public static async Task<decimal> GetDecimalAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadDecimal();
            }
        }

        public static async Task<double> GetDoubleAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadDouble();
            }
        }

        public static async Task<short> GetShortAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadInt16();
            }
        }

        public static async Task<int> GetIntAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadInt32();
            }
        }

        public static async Task<long> GetLongAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadInt64();
            }
        }

        public static async Task<float> GetFloatAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadSingle();
            }
        }

        public static async Task<string> GetStringAsync(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                return binaryReader.ReadString();
            }
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, bool value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, bool value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, char value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, char value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, decimal value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, decimal value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, double value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, double value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, short value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, short value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, int value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, int value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, long value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, long value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, float value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, float value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, string value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, string value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(value);
                bytes = memoryStream.ToArray();
            }
            return cache.SetAsync(key, bytes, options);
        }

        #endregion

        #region JSON

        public static async Task<T> GetAsJsonAsync<T>(this IDistributedCache cache, [NotNull] string key)
        {
            string json = await GetStringAsync(cache, key);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static Task SetAsJsonAsync<T>(this IDistributedCache cache, [NotNull] string key, T value)
        {
            return SetAsJsonAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsJsonAsync<T>(this IDistributedCache cache, [NotNull] string key, T value, DistributedCacheEntryOptions options)
        {
            string json = JsonConvert.SerializeObject(value, Formatting.None);
            return SetAsync(cache, key, json, options);
        }

        #endregion

        #region BinaryFormatter
#if DNX451
        public static async Task<T> GetAsync<T>(this IDistributedCache cache, [NotNull] string key)
        {
            byte[] bytes = await cache.GetAsync(key);
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }

        public static Task SetAsync<T>(this IDistributedCache cache, [NotNull] string key, T value)
        {
            return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync<T>(this IDistributedCache cache, [NotNull] string key, T value, DistributedCacheEntryOptions options)
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, value);
                bytes = memoryStream.ToArray();
            }

            return cache.SetAsync(key, bytes, options);
        }
#endif
        #endregion
    }
}