// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    public class TriggerCallbackRegistration : IDisposable
    {
        public Action<object> RegisteredCallback { get; set; }

        public object RegisteredState { get; set; }

        public ManualResetEvent Disposed { get; set; } = new ManualResetEvent(false);

        public void Dispose()
        {
            Disposed.Set();
        }
    }
}