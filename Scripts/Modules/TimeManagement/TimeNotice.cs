﻿
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.TimeManagement
{
    public static class TimeNotice
    {
        //----- params -----

        public sealed class Timer
        {
            public string Name { get; set; }

            public long UnixTime { get; set; }
        }

        //----- field -----

        private static LifetimeDisposable lifetimeDisposable = null;

        private static Dictionary<string, Timer> timers = null;

        private static Func<long> getCurrentTimeFunction = null;

        private static long currentTime = 0;

        private static Subject<Timer> onTime = null;

        private static bool initialized = false;

        //----- property -----

        //----- method -----
        
        public static void Initialize(Func<long> getCurrentTimeFunction)
        {
            if (initialized) { return; }

            TimeNotice.getCurrentTimeFunction = getCurrentTimeFunction;

            lifetimeDisposable = new LifetimeDisposable();

            timers = new Dictionary<string, Timer>();

            currentTime = getCurrentTimeFunction.Invoke();

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateTimers())
                .AddTo(lifetimeDisposable.Disposable);

            initialized = true;
        }

        public static void Set(string name, DateTime dateTime)
        {
            Set(name, dateTime.ToUnixTime());
        }

        public static void Set(string name, long unixTime)
        {
            var timer = timers.GetValueOrDefault(name);

            if (timer == null)
            {
                timer = new Timer()
                {
                    Name = name,
                    UnixTime = unixTime,
                };

                timers.Add(timer.Name, timer);
            }

            timer.UnixTime = unixTime;
        }

        public static bool IsExists(string name)
        {
            return timers.ContainsKey(name);
        }

        private static void UpdateTimers()
        {
            var prevTime = currentTime;

            currentTime = getCurrentTimeFunction.Invoke();

            if (timers.Any())
            {
                var removeList = new List<Timer>();

                foreach (var timer in timers.Values)
                {
                    if (prevTime < timer.UnixTime && timer.UnixTime <= currentTime)
                    {
                        if (onTime != null)
                        {
                            onTime.OnNext(timer);
                        }

                        removeList.Add(timer);
                    }
                }

                foreach (var target in removeList)
                {
                    timers.Remove(target.Name);
                }
            }
        }

        public static IObservable<Timer> OnTimeAsObservable()
        {
            return onTime ?? (onTime = new Subject<Timer>());
        }
    }
}