﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class SkipUntil<TSource, TOther> : Producer<TSource, SkipUntil<TSource, TOther>._>
    {
        private readonly IObservable<TSource> _source;
        private readonly IObservable<TOther> _other;

        public SkipUntil(IObservable<TSource> source, IObservable<TOther> other)
        {
            _source = source;
            _other = other;
        }

        protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => sink.Run(this);

        internal sealed class _ : IdentitySink<TSource>
        {
            readonly OtherObserver other;

            IDisposable mainDisposable;

            volatile bool _forward;

            int halfSerializer;

            Exception error;

            static readonly Exception TerminalException = new Exception("No further exceptions");

            public _(IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.other = new OtherObserver(this);
            }

            public IDisposable Run(SkipUntil<TSource, TOther> parent)
            {
                other.OnSubscribe(parent._other.Subscribe(other));

                Disposable.TrySetSingle(ref mainDisposable, parent._source.Subscribe(this));

                return this;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeMain();
                other.Dispose();
            }

            void DisposeMain()
            {
                if (!Disposable.GetIsDisposed(ref mainDisposable))
                {
                    Disposable.TryDispose(ref mainDisposable);
                }
            }

            public override void OnNext(TSource value)
            {
                if (_forward)
                {
                    if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
                    {
                        ForwardOnNext(value);
                        if (Interlocked.Decrement(ref halfSerializer) != 0)
                        {
                            var ex = error;
                            error = TerminalException;
                            ForwardOnError(ex);
                        }
                    }
                }
            }

            public override void OnError(Exception ex)
            {
                if (Interlocked.CompareExchange(ref error, ex, null) == null)
                {
                    if (Interlocked.Increment(ref halfSerializer) == 1)
                    {
                        error = TerminalException;
                        ForwardOnError(ex);
                    }
                }
            }

            public override void OnCompleted()
            {
                if (_forward)
                {
                    if (Interlocked.CompareExchange(ref error, TerminalException, null) == null)
                    {
                        if (Interlocked.Increment(ref halfSerializer) == 1)
                        {
                            ForwardOnCompleted();
                        }
                    }
                }
                else
                {
                    DisposeMain();
                }
            }

            void OtherComplete()
            {
                _forward = true;
            }

            sealed class OtherObserver : IObserver<TOther>, IDisposable
            {
                readonly _ parent;

                IDisposable upstream;

                public OtherObserver(_ parent)
                {
                    this.parent = parent;
                }

                public void OnSubscribe(IDisposable d)
                {
                    Disposable.TrySetSingle(ref upstream, d);
                }

                public void Dispose()
                {
                    if (!Disposable.GetIsDisposed(ref upstream))
                    {
                        Disposable.TryDispose(ref upstream);
                    }
                }

                public void OnCompleted()
                {
                    Dispose();
                }

                public void OnError(Exception error)
                {
                    parent.OnError(error);
                }

                public void OnNext(TOther value)
                {
                    parent.OtherComplete();
                    Dispose();
                }
            }
        }
    }

    internal sealed class SkipUntil<TSource> : Producer<TSource, SkipUntil<TSource>._>
    {
        private readonly IObservable<TSource> _source;
        private readonly DateTimeOffset _startTime;
        internal readonly IScheduler _scheduler;

        public SkipUntil(IObservable<TSource> source, DateTimeOffset startTime, IScheduler scheduler)
        {
            _source = source;
            _startTime = startTime;
            _scheduler = scheduler;
        }

        public IObservable<TSource> Combine(DateTimeOffset startTime)
        {
            //
            // Maximum semantics:
            //
            //   t                     0--1--2--3--4--5--6--7->   t                     0--1--2--3--4--5--6--7->
            //
            //   xs                    --o--o--o--o--o--o--|      xs                    --o--o--o--o--o--o--|
            //   xs.SU(5AM)            xxxxxxxxxxxxxxxx-o--|      xs.SU(3AM)            xxxxxxxxxx-o--o--o--|
            //   xs.SU(5AM).SU(3AM)    xxxxxxxxx--------o--|      xs.SU(3AM).SU(5AM)    xxxxxxxxxxxxxxxx-o--|
            //
            if (startTime <= _startTime)
                return this;
            else
                return new SkipUntil<TSource>(_source, startTime, _scheduler);
        }

        protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => sink.Run(this);

        internal sealed class _ : IdentitySink<TSource>
        {
            private volatile bool _open;

            public _(IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public IDisposable Run(SkipUntil<TSource> parent)
            {
                var t = parent._scheduler.Schedule(parent._startTime, Tick);
                var d = parent._source.SubscribeSafe(this);
                return StableCompositeDisposable.Create(t, d);
            }

            private void Tick()
            {
                _open = true;
            }

            public override void OnNext(TSource value)
            {
                if (_open)
                    ForwardOnNext(value);
            }
        }
    }
}
