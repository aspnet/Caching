// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Framework.Caching.MongoDB
{
    public static class MongoDBTestConfig
    {
        internal const string FunctionalTestsMongoDBServerExeName = "mongod";

        private static volatile Process _redisServerProcess; // null implies if server exists it was not started by this code
        private static readonly object _redisServerProcessLock = new object();
        public static int MongoDBPort = 27017; // override default so that do not interfere with anyone else's server

        public static MongoDBCache CreateCacheInstance(string instanceName)
        {
            return new MongoDBCache(new MongoDBCacheOptions()
            {
                ConnectionString = $"mongodb://localhost:{MongoDBPort}",
                Database = "caching",
                Collection = "cache"
            });
        }

        public static void GetOrStartServer()
        {
            if (UserHasStartedOwnServer())
            {
                // user claims they have started their own
                return;
            }

            if (AlreadyOwnRunningServer())
            {
                return;
            }

            TryConnectToOrStartServer();
        }

        private static bool AlreadyOwnRunningServer()
        {
            // Does RedisTestConfig already know about a running server?
            if (_redisServerProcess != null
                && !_redisServerProcess.HasExited)
            {
                return true;
            }

            return false;
        }

        private static bool TryConnectToOrStartServer()
        {
            if (CanFindExistingServer())
            {
                return true;
            }

            throw new InvalidOperationException("A running MongoDB server is required.");
        }

        public static void StopRedisServer()
        {
            if (UserHasStartedOwnServer())
            {
                // user claims they have started their own - they are responsible for stopping it
                return;
            }

            if (CanFindExistingServer())
            {
                lock (_redisServerProcessLock)
                {
                    if (_redisServerProcess != null)
                    {
                        _redisServerProcess.Kill();
                        _redisServerProcess = null;
                    }
                }
            }
        }

        private static bool CanFindExistingServer()
        {
            var process = Process.GetProcessesByName(FunctionalTestsMongoDBServerExeName).SingleOrDefault();
            if (process == null || process.HasExited)
            {
                lock (_redisServerProcessLock)
                {
                    _redisServerProcess = null;
                }
                return false;
            }

            lock (_redisServerProcessLock)
            {
                _redisServerProcess = process;
            }
            return true;
        }

        public static bool UserHasStartedOwnServer()
        {
            // if the user sets this environment variable they are claiming they've started
            // their own Redis Server and are responsible for starting/stopping it
            return (Environment.GetEnvironmentVariable("STARTED_OWN_MONGODB_SERVER") != null);
        }
    }
}
