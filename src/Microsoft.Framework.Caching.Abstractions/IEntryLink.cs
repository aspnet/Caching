// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.Framework.Caching.Memory
{
    /// <summary>
    /// Used to flow expiration information from one entry to another. <see cref="IChangeToken"/>s and minimum absolute 
    /// expiration will be copied from the dependent entry to the parent entry. The parent entry will not expire if the
    /// dependent entry is removed manually, removed due to memory pressure, or expires due to sliding expiration.
    /// </summary>
    public interface IEntryLink : IDisposable
    {
        /// <summary>
        /// Gets the minimum absolute expiration for all dependent entries, or null if not set.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; }

        /// <summary>
        /// Gets all the <see cref="IChangeToken"/>s from the dependent entries.
        /// </summary>
        IEnumerable<IChangeToken> ChangeTokens { get; }

        /// <summary>
        /// Adds <see cref="IChangeToken"/>s from a dependent entries.
        /// </summary>
        /// <param name="changeTokens"><see cref="IChangeToken"/>s from dependent entries.</param>
        void AddChangeTokens(IList<IChangeToken> changeTokens);

        /// <summary>
        /// Sets the absolute expiration for from a dependent entry. The minimum value across all dependent entries
        /// will be used.
        /// </summary>
        /// <param name="absoluteExpiration"></param>
        void SetAbsoluteExpiration(DateTimeOffset absoluteExpiration);
    }
}