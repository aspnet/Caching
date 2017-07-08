// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryDistributedCacheOptions : MemoryCacheOptions, IOptions<MemoryDistributedCacheOptions>
    {
        public MemoryDistributedCacheOptions()
            : base()
        {
            // Default size limit of 200 MB
            SizeLimit = 200 * 1024 * 1024;
        }

        MemoryDistributedCacheOptions IOptions<MemoryDistributedCacheOptions>.Value
        {
            get { return this; }
        }
    }
}