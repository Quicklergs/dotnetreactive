﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Disposables;

namespace System.Reactive.Concurrency
{
    public static partial class Scheduler
    {
        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler"/> or <paramref name="action"/> is <c>null</c>.</exception>
        public static IDisposable Schedule(this IScheduler scheduler, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(action, Invoke);
        }

        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to execute.</param>
        /// <param name="state">A state object to be passed to <paramref name="action"/>.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler"/> or <paramref name="action"/> is <c>null</c>.</exception>
        internal static IDisposable Schedule<TState>(this IScheduler scheduler, Action<TState> action, TState state)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(
                (action, state), 
                (_, tuple) =>
                {
                    tuple.action(tuple.state);
                    return Disposable.Empty;
                });
        }

        /// <summary>
        /// Schedules an action to be executed after the specified relative due time.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to execute.</param>
        /// <param name="dueTime">Relative time after which to execute the action.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler"/> or <paramref name="action"/> is <c>null</c>.</exception>
        public static IDisposable Schedule(this IScheduler scheduler, TimeSpan dueTime, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(action, dueTime, Invoke);
        }

        /// <summary>
        /// Schedules an action to be executed at the specified absolute due time.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to execute.</param>
        /// <param name="dueTime">Absolute time at which to execute the action.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler"/> or <paramref name="action"/> is <c>null</c>.</exception>
        public static IDisposable Schedule(this IScheduler scheduler, DateTimeOffset dueTime, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(action, dueTime, Invoke);
        }

        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler"/> or <paramref name="action"/> is <c>null</c>.</exception>
        public static IDisposable ScheduleLongRunning(this ISchedulerLongRunning scheduler, Action<ICancelable> action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.ScheduleLongRunning(action, (a, c) => a(c));
        }

        private static IDisposable Invoke(IScheduler scheduler, Action action)
        {
            action();
            return Disposable.Empty;
        }
    }
}
