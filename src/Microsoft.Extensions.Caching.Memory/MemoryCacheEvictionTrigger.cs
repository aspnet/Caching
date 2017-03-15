// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

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
        private Timer _timer;

        public MemoryCacheEvictionTrigger()
            : this(TimeSpan.FromMinutes(1), 10) // TODO: Need better defaults?
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

        public void Resume()
        {
            if (!_isDisposed && !_timerIsRunning)
            {
                lock (_lock)
                {
                    if (!_timerIsRunning && _timer != null)
                    {
                        _timerIsRunning = true;
                        _timer.Change(_evictionInterval, _evictionInterval);
                        Interlocked.Exchange(ref _intervalsWithoutEviction, 0);
                    }
                }
            }
        }

        public void Stop()
        {
            if (!_isDisposed && _timerIsRunning)
            {
                lock (_lock)
                {
                    _timerIsRunning = false;
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private void TimerLoop(object state)
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
                Stop();
            }
        }
    }
}
