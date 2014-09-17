// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    public class BaseExpirationTrigger : IExpirationTrigger
    {
        private CancellationTokenSource _registrations = new CancellationTokenSource();
        private bool _expired;

        public BaseExpirationTrigger()
            : this(false)
        {
        }

        protected BaseExpirationTrigger(bool supportsActiveExpirationCallbacks)
        {
            ActiveExpirationCallbacks = supportsActiveExpirationCallbacks;
        }

        public bool ActiveExpirationCallbacks { get; private set; }

        public bool IsExpired
        {
            get
            {
                if (!_expired)
                {
                    _expired = CheckIsExpired();
                }

                return _expired;
            }
        }

        protected bool CheckIsExpired()
        {
            return _registrations.Token.IsCancellationRequested;
        }

        public void Expire()
        {
            _expired = true;
            ThreadPool.QueueUserWorkItem(InvokeCallbacks, _registrations);
        }

        private static void InvokeCallbacks(object state)
        {
            var _registrations = (CancellationTokenSource)state;
            _registrations.Cancel();
        }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            return _registrations.Token.Register(callback, state);
        }
    }
}