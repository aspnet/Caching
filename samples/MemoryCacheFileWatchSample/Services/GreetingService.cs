// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MemoryCacheFileWatchSample.Abstractions;

namespace MemoryCacheFileWatchSample.Services
{
    public class GreetingService : IGreetingService
    {
        private readonly ITimeService _timeService;

        public GreetingService(ITimeService timeService)
        {
            _timeService = timeService;
        }

        public string Greet(string recipient)
        {
            var time = _timeService.Now;
            int hour = time.Hour;

            if (time.Hour < 12)
            {
                return $"Good morning, {recipient}!";
            }
            if (time.Hour < 17)
            {
                return $"Good afternoon, {recipient}!";
            }
            return $"Good evening, {recipient}!";
        }
    }
}