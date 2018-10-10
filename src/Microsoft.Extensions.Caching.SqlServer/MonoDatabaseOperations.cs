// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Caching.SqlServer
{
    internal class MonoDatabaseOperations : DatabaseOperations
    {
        public MonoDatabaseOperations(
            string connectionString, string schemaName, string tableName, ISystemClock systemClock)
            : base(connectionString, schemaName, tableName, systemClock)
        {
        }

        protected override byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;

            string query;
            if (includeValue)
            {
                query = SqlQueries.GetCacheItem;
            }
            else
            {
                query = SqlQueries.GetCacheItemWithoutValue;
            }

            byte[] value = null;
            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand(query, connection);
                command.Parameters
                    .AddCacheItemId(key)
                    .AddWithValue("UtcNow", SqlDbType.DateTime, utcNow.UtcDateTime);

                connection.Open();

                var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                if (reader.Read())
                {
                    if (includeValue)
                    {
                        value = (byte[])reader[Columns.Indexes.CacheItemOnlyValueIndex];
                    }
                }
                else
                {
                    return null;
                }
            }

            return value;
        }

        protected override async Task<byte[]> GetCacheItemAsync(string key, bool includeValue, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            var utcNow = SystemClock.UtcNow;

            string query;
            if (includeValue)
            {
                query = SqlQueries.GetCacheItem;
            }
            else
            {
                query = SqlQueries.GetCacheItemWithoutValue;
            }

            byte[] value = null;
            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand(SqlQueries.GetCacheItem, connection);
                command.Parameters
                    .AddCacheItemId(key)
                    .AddWithValue("UtcNow", SqlDbType.DateTime, utcNow.UtcDateTime);

                await connection.OpenAsync(token);

                var reader = await command.ExecuteReaderAsync(
                    CommandBehavior.SingleRow | CommandBehavior.SingleResult,
                    token);

                if (await reader.ReadAsync(token))
                {
                    if (includeValue)
                    {
                        value = (byte[])reader[Columns.Indexes.CacheItemOnlyValueIndex];
                    }
                }
                else
                {
                    return null;
                }
            }

            return value;
        }

        public override void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new SqlConnection(ConnectionString))
            {
                var upsertCommand = new SqlCommand(SqlQueries.SetCacheItem, connection);
                upsertCommand.Parameters
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpirationMono(absoluteExpiration)
                    .AddWithValue("UtcNow", SqlDbType.DateTime, utcNow.UtcDateTime);

                connection.Open();

                try
                {
                    upsertCommand.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public override async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new SqlConnection(ConnectionString))
            {
                var upsertCommand = new SqlCommand(SqlQueries.SetCacheItem, connection);
                upsertCommand.Parameters
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpirationMono(absoluteExpiration)
                    .AddWithValue("UtcNow", SqlDbType.DateTime, utcNow.UtcDateTime);

                await connection.OpenAsync(token);

                try
                {
                    await upsertCommand.ExecuteNonQueryAsync(token);
                }
                catch (SqlException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public override void DeleteExpiredCacheItems()
        {
            var utcNow = SystemClock.UtcNow;

            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand(SqlQueries.DeleteExpiredCacheItems, connection);
                command.Parameters.AddWithValue("UtcNow", SqlDbType.DateTime, utcNow.UtcDateTime);

                connection.Open();

                var effectedRowCount = command.ExecuteNonQuery();
            }
        }
    }
}