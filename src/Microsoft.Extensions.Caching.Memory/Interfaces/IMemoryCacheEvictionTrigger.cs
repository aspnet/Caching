// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    public interface IMemoryCacheEvictionTrigger : IDisposable
    {
        // TODO: doc comments
        void SetEvictionCallback(Func<int> evictionCallback);

        void Resume(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries);
    }
}
