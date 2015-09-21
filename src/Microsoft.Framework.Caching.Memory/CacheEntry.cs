// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Framework.Caching.Memory
{
    internal class CacheEntry
    {
        private static readonly Action<object> ExpirationCallback = TokensExpired;

        private readonly Action<CacheEntry> _notifyCacheOfExpiration;

        private readonly DateTimeOffset? _absoluteExpiration;

        internal CacheEntry(
            object key,
            object value,
            DateTimeOffset utcNow,
            DateTimeOffset? absoluteExpiration,
            MemoryCacheEntryOptions options,
            Action<CacheEntry> notifyCacheOfExpiration)
        {
            Key = key;
            Value = value;
            LastAccessed = utcNow;
            Options = options;
            _notifyCacheOfExpiration = notifyCacheOfExpiration;
            _absoluteExpiration = absoluteExpiration;
            PostEvictionCallbacks = options.PostEvictionCallbacks;
        }

        internal MemoryCacheEntryOptions Options { get; private set; }

        internal object Key { get; private set; }

        internal object Value { get; private set; }

        private bool IsExpired { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        internal IList<IDisposable> ChangeTokenRegistrations { get; set; }

        internal IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; set; }

        internal DateTimeOffset LastAccessed { get; set; }

        internal bool CheckExpired(DateTimeOffset now)
        {
            return IsExpired || CheckForExpiredTime(now) || CheckForExpiredTokens();
        }

        internal void SetExpired(EvictionReason reason)
        {
            IsExpired = true;
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            DetachTokens();
        }

        private bool CheckForExpiredTime(DateTimeOffset now)
        {
            if (_absoluteExpiration.HasValue && _absoluteExpiration.Value <= now)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            if (Options.SlidingExpiration.HasValue
                && (now - LastAccessed) >= Options.SlidingExpiration)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            return false;
        }

        internal bool CheckForExpiredTokens()
        {
            var tokens = Options.ChangeTokens;
            if (tokens != null)
            {
                for (int i = 0; i < tokens.Count; i++)
                {
                    var token = tokens[i];
                    if (token.HasChanged)
                    {
                        SetExpired(EvictionReason.TokenChanged);
                        return true;
                    }
                }
            }
            return false;
        }

        // TODO: There's a possible race between AttachTokens and DetachTokens if a token fires almost immediately.
        // This may result in some registrations not getting disposed.
        internal void AttachTokens()
        {
            var tokens = Options.ChangeTokens;
            if (tokens != null)
            {
                for (int i = 0; i < tokens.Count; i++)
                {
                    var token = tokens[i];
                    if (token.ActiveChangeCallbacks)
                    {
                        if (ChangeTokenRegistrations == null)
                        {
                            ChangeTokenRegistrations = new List<IDisposable>(1);
                        }
                        var registration = token.RegisterChangeCallback(ExpirationCallback, this);
                        ChangeTokenRegistrations.Add(registration);
                    }
                }
            }
        }

        private static void TokensExpired(object obj)
        {
            var entry = (CacheEntry)obj;
            entry.SetExpired(EvictionReason.TokenChanged);
            entry._notifyCacheOfExpiration(entry);
        }

        // TODO: Thread safety
        private void DetachTokens()
        {
            var registrations = ChangeTokenRegistrations;
            if (registrations != null)
            {
                ChangeTokenRegistrations = null;
                for (int i = 0; i < registrations.Count; i++)
                {
                    var registration = registrations[i];
                    registration.Dispose();
                }
            }
        }

        // TODO: Ensure a thread safe way to prevent these from being invoked more than once;
        internal void InvokeEvictionCallbacks()
        {
            if (PostEvictionCallbacks != null)
            {
                Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void InvokeCallbacks(CacheEntry entry)
        {
            var callbackRegistrations = entry.PostEvictionCallbacks;
            entry.PostEvictionCallbacks = null;

            if (callbackRegistrations == null)
            {
                return;
            }

            for (int i = 0; i < callbackRegistrations.Count; i++)
            {
                var registration = callbackRegistrations[i];

                try
                {
                    registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
                }
                catch (Exception)
                {
                    // This will be invoked on a background thread, don't let it throw.
                    // TODO: LOG
                }
            }
        }
    }
}