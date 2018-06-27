﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class Collect<TSource, TResult> : PushToPullAdapter<TSource, TResult>
    {
        private readonly Func<TResult> _getInitialCollector;
        private readonly Func<TResult, TSource, TResult> _merge;
        private readonly Func<TResult, TResult> _getNewCollector;

        public Collect(IObservable<TSource> source, Func<TResult> getInitialCollector, Func<TResult, TSource, TResult> merge, Func<TResult, TResult> getNewCollector)
            : base(source)
        {
            _getInitialCollector = getInitialCollector;
            _merge = merge;
            _getNewCollector = getNewCollector;
        }

        protected override PushToPullSink<TSource, TResult> Run(IDisposable subscription) => new _(_merge, _getNewCollector, _getInitialCollector(), subscription);

        private sealed class _ : PushToPullSink<TSource, TResult>
        {
            readonly object _gate;
            readonly Func<TResult, TSource, TResult> _merge;
            readonly Func<TResult, TResult> _getNewCollector;

            public _(Func<TResult, TSource, TResult> merge, Func<TResult, TResult> getNewCollector, TResult collector, IDisposable subscription)
                : base(subscription)
            {
                _gate = new object();
                _merge = merge;
                _getNewCollector = getNewCollector;
                _collector = collector;
            }

            private TResult _collector;
            private Exception _error;
            private bool _hasCompleted;
            private bool _done;

            public override void OnNext(TSource value)
            {
                lock (_gate)
                {
                    try
                    {
                        _collector = _merge(_collector, value);
                    }
                    catch (Exception ex)
                    {
                        _error = ex;

                        Dispose();
                    }
                }
            }

            public override void OnError(Exception error)
            {
                Dispose();

                lock (_gate)
                {
                    _error = error;
                }
            }

            public override void OnCompleted()
            {
                Dispose();

                lock (_gate)
                {
                    _hasCompleted = true;
                }
            }

            public override bool TryMoveNext(out TResult current)
            {
                lock (_gate)
                {
                    var error = _error;
                    if (error != null)
                    {
                        current = default;
                        error.Throw();
                    }
                    else
                    {
                        if (_hasCompleted)
                        {
                            if (_done)
                            {
                                current = default;
                                return false;
                            }

                            current = _collector;
                            _done = true;
                        }
                        else
                        {
                            current = _collector;

                            try
                            {
                                _collector = _getNewCollector(current);
                            }
                            catch
                            {
                                Dispose();
                                throw;
                            }
                        }
                    }

                    return true;
                }
            }
        }
    }
}
