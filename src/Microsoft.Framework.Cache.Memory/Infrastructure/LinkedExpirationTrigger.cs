// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    internal class LinkedExpirationTrigger : BaseExpirationTrigger
    {
        internal LinkedExpirationTrigger(CacheEntry entry, ISystemClock clock)
            : base(true)
        {
            Entry = entry;
            Clock = clock;
        }

        private CacheEntry Entry { get; set; }

        private ISystemClock Clock { get; set; }

        protected override bool CheckIsExpired()
        {
            if (base.CheckIsExpired())
            {
                return true;
            }
            return Entry.CheckExpired(Clock.UtcNow);
        }
    }
}