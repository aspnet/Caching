// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    public class GcNotificationTests
    {
        public static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(1);

        [Fact]
        public void CallbackRegisteredAndInvoked()
        {
            var callbackInvoked = new ManualResetEvent(false);
            GcNotification.Register(state =>
            {
                callbackInvoked.Set();
                return false;
            }, null);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout));
        }

        [Fact]
        public void CallbackInvokedMultipleTimes()
        {
            int callbackCount = 0;
            var callbackInvoked = new ManualResetEvent(false);
            GcNotification.Register(state =>
            {
                callbackCount++;
                callbackInvoked.Set();
                if (callbackCount < 2)
                {
                    return true;
                }
                return false;
            }, null);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout));
            Assert.Equal(1, callbackCount);

            callbackInvoked.Reset();

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout));
            Assert.Equal(2, callbackCount);

            callbackInvoked.Reset();

            // No callback expected the 3rd time
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.False(callbackInvoked.WaitOne(CallbackTimeout));
            Assert.Equal(2, callbackCount);
        }
    }
}