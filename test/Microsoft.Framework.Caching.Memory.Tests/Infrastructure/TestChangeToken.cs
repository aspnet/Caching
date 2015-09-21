// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Primitives;

namespace Microsoft.Framework.Caching.Memory.Infrastructure
{
    internal class TestChangeToken : IChangeToken
    {
        private bool _hasChanged;
        private bool _activeChangeCallbacks;

        public bool HasChanged
        {
            get
            {
                IsExpiredWasCalled = true;
                return _hasChanged;
            }
            set
            {
                _hasChanged = value;
            }
        }

        public bool IsExpiredWasCalled { get; set; }

        public bool ActiveChangeCallbacks
        {
            get
            {
                ActiveChangeCallbacksWasCalled = true;
                return _activeChangeCallbacks;
            }
            set
            {
                _activeChangeCallbacks = value;
            }
        }

        public bool ActiveChangeCallbacksWasCalled { get; set; }

        public TokenCallbackRegistration Registration { get; set; }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            Registration = new TokenCallbackRegistration()
            {
                RegisteredCallback = callback,
                RegisteredState = state,
            };
            return Registration;
        }

        public void Fire()
        {
            HasChanged = true;
            if (Registration != null && !Registration.Disposed)
            {
                Registration.RegisteredCallback(Registration.RegisteredState);
            }
        }
    }
}