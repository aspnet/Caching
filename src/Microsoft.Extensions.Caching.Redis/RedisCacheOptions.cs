// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Redis
{
    public class RedisCacheOptions
    {
        public RedisCacheOptions(IEnumerable<IConfigureOptions<RedisCacheOptions>> configureOptions = null)
        {
            if (configureOptions != null)
            {
                foreach (var configure in configureOptions)
                {
                    configure.Configure(this);
                }
            }
        }

        public string Configuration { get; set; }

        public string InstanceName { get; set; }
    }
}