// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.SqlServer
{
    internal interface IDatabaseOperations
    {
        byte[] GetCacheItem(string key);

        Task<byte[]> GetCacheItemAsync(string key);

        void RefreshCacheItem(string key);

        Task RefreshCacheItemAsync(string key);

        void DeleteCacheItem(string key);

        Task DeleteCacheItemAsync(string key);

        void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options);

        void DeleteExpiredCacheItems();
    }
}