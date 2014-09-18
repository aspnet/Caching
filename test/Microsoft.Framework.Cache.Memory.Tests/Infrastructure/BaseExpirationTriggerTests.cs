// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    public class BaseExpirationTriggerTests
    {
        public static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(1);

        [Fact]
        public void CreateAndCheckProperties()
        {
            var trigger = new BaseExpirationTrigger();
            Assert.False(trigger.ActiveExpirationCallbacks);
            Assert.False(trigger.IsExpired);
            trigger.Expire();
            Assert.True(trigger.IsExpired);
        }

        [Fact]
        public void ExpireInvokesCallbacks()
        {
            var myState = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new BaseExpirationTrigger();
            trigger.RegisterExpirationCallback(subState =>
            {
                callbackInvoked.Set();
            }, myState);
            trigger.Expire();
            Assert.True(callbackInvoked.WaitOne(CallbackTimeout));
        }

        [Fact]
        public void ExceptionInCallbackIsSuppressed()
        {
            var myState = new object();
            var trigger = new BaseExpirationTrigger();
            trigger.RegisterExpirationCallback(subState =>
            {
                throw new NotImplementedException();
            }, myState);
            trigger.Expire();
        }
    }
}