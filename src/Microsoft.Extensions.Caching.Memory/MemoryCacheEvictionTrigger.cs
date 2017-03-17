// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Caching.Memory
{
    // TODO: review disposable/locking logic
    public class MemoryCacheEvictionTrigger : IMemoryCacheEvictionTrigger
    {
        private readonly object _lock = new object();
        private readonly TimeSpan _evictionInterval;
        private readonly int _intervalsWithoutEvictionUntilIdle;

        private volatile bool _isDisposed;
        private volatile bool _timerIsRunning;
        private int _intervalsWithoutEviction;
        private int _evictionRunning;
        private Timer _timer;

        public MemoryCacheEvictionTrigger()
            : this(TimeSpan.FromMinutes(1), 2)
        { }

        public MemoryCacheEvictionTrigger(TimeSpan evictionInterval, int intervalsWithoutEvictionUntilIdle)
        {
            _evictionInterval = evictionInterval;
            _intervalsWithoutEvictionUntilIdle = intervalsWithoutEvictionUntilIdle;
            _timer = new Timer(TimerLoop, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Func<bool> EvictionCallback { get; set; }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                lock (_lock)
                {
                    if (_timer != null)
                    {
                        _timerIsRunning = false;
                        _timer.Dispose();
                        _timer = null;
                    }
                }
            }
        }

        public void Resume(IReadOnlyCollection<KeyValuePair<object, IRetrievedCacheEntry>> entries)
        {
            Interlocked.Exchange(ref _intervalsWithoutEviction, 0);

            if (!_isDisposed)
            {
                lock (_lock)
                {
                    if (!_timerIsRunning && _timer != null)
                    {
                        _timerIsRunning = true;
                        _timer.Change(_evictionInterval, TimeSpan.FromMilliseconds(-1));
                    }
                }
            }
        }

        private void TimerLoop(object state)
        {
            if (Interlocked.CompareExchange(ref _evictionRunning, 1, 0) == 0)
            {
                if (EvictionCallback())
                {
                    Interlocked.Exchange(ref _intervalsWithoutEviction, 0);
                }
                else
                {
                    Interlocked.Increment(ref _intervalsWithoutEviction);
                }

                if (Volatile.Read(ref _intervalsWithoutEviction) >= _intervalsWithoutEvictionUntilIdle)
                {
                    lock (_lock)
                    {
                        _timerIsRunning = false;
                    }
                }
                else
                {
                    _timer.Change(_evictionInterval, TimeSpan.FromMilliseconds(-1));
                }

                Interlocked.Exchange(ref _evictionRunning, 0);
            }
        }
    }
}
