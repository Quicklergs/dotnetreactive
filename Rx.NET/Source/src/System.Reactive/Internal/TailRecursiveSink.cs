﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive
{
    internal abstract class TailRecursiveSink<TSource> : IdentitySink<TSource>
    {
        public TailRecursiveSink(IObserver<TSource> observer)
            : base(observer)
        {
        }

        bool _isDisposed;

        int _trampoline;

        IDisposable _currentSubscription;

        Stack<IEnumerator<IObservable<TSource>>> _stack = new Stack<IEnumerator<IObservable<TSource>>>();

        public void Run(IEnumerable<IObservable<TSource>> sources)
        {
            if (!TryGetEnumerator(sources, out var current))
                return;

            _stack.Push(current);

            Drain();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAll();
            }
            base.Dispose(disposing);
        }

        void Drain()
        {
            if (Interlocked.Increment(ref _trampoline) != 1)
            {
                return;
            }

            for (; ; )
            {
                if (Volatile.Read(ref _isDisposed))
                {
                    while (_stack.Count != 0)
                    {
                        var enumerator = _stack.Pop();
                        enumerator.Dispose();
                    }

                    Disposable.TryDispose(ref _currentSubscription);
                }
                else
                {
                    if (_stack.Count != 0)
                    {
                        var currentEnumerator = _stack.Peek();

                        var currentObservable = default(IObservable<TSource>);
                        var next = default(IObservable<TSource>);

                        try
                        {
                            if (currentEnumerator.MoveNext())
                            {
                                currentObservable = currentEnumerator.Current;
                            }
                        }
                        catch (Exception ex)
                        {
                            currentEnumerator.Dispose();
                            ForwardOnError(ex);
                            Volatile.Write(ref _isDisposed, true);
                            continue;
                        }

                        try
                        {
                            next = Helpers.Unpack(currentObservable);

                        }
                        catch (Exception ex)
                        {
                            next = null;
                            if (!Fail(ex))
                            {
                                Volatile.Write(ref _isDisposed, true);
                            }
                            continue;
                        }

                        if (next != null)
                        {
                            var nextSeq = Extract(next);
                            if (nextSeq != null)
                            {
                                if (TryGetEnumerator(nextSeq, out var nextEnumerator))
                                {
                                    _stack.Push(nextEnumerator);
                                    continue;
                                }
                                else
                                {
                                    Volatile.Write(ref _isDisposed, true);
                                    continue;
                                }
                            }
                            else
                            {
                                // we need an unique indicator for this as
                                // Subscribe could return a Disposable.Empty or
                                // a BooleanDisposable
                                var sad = ReadyToken.Ready;

                                // Swap in the Ready indicator so we know the sequence hasn't been disposed
                                if (Disposable.TrySetSingle(ref _currentSubscription, sad) == TrySetSingleResult.Success)
                                {
                                    // subscribe to the source
                                    var d = next.SubscribeSafe(this);

                                    // Try to swap in the returned disposable in place of the Ready indicator
                                    // Since this drain loop is the only one to use Ready, this should
                                    // be unambiguous
                                    var u = Interlocked.CompareExchange(ref _currentSubscription, d, sad);
                                    
                                    // sequence disposed
                                    if (u == BooleanDisposable.True)
                                    {
                                        d.Dispose();
                                        continue;
                                    }
                                }   
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            _stack.Pop();
                            currentEnumerator.Dispose();
                            continue;
                        }
                    }
                    else
                    {
                        Volatile.Write(ref _isDisposed, true);
                        Done();
                    }
                }

                if (Interlocked.Decrement(ref _trampoline) == 0)
                {
                    break;
                }
            }
        }

        void DisposeAll()
        {
            Volatile.Write(ref _isDisposed, true);
            // the disposing of currentSubscription is deferred to drain due to some ObservableExTest.Iterate_Complete()
            // Interlocked.Exchange(ref currentSubscription, BooleanDisposable.True)?.Dispose();
            Drain();
        }

        protected void Recurse()
        {
            if (Disposable.TrySetSerial(ref _currentSubscription, null))
                Drain();
        }

        protected abstract IEnumerable<IObservable<TSource>> Extract(IObservable<TSource> source);

        private bool TryGetEnumerator(IEnumerable<IObservable<TSource>> sources, out IEnumerator<IObservable<TSource>> result)
        {
            try
            {
                result = sources.GetEnumerator();
                return true;
            }
            catch (Exception exception)
            {
                ForwardOnError(exception);

                result = null;
                return false;
            }
        }

        protected virtual void Done()
        {
            ForwardOnCompleted();
        }

        protected virtual bool Fail(Exception error)
        {
            ForwardOnError(error);

            return false;
        }
    }

    /// <summary>
    /// Holds onto a singleton IDisposable indicating a ready state.
    /// </summary>
    internal static class ReadyToken
    {
        /// <summary>
        /// This indicates the operation has been prepared and ready for
        /// the next step.
        /// </summary>
        internal static readonly IDisposable Ready = new ReadyDisposable();

        sealed class ReadyDisposable : IDisposable
        {
            public void Dispose()
            {
                // deliberately no-op
            }
        }
    }
}
