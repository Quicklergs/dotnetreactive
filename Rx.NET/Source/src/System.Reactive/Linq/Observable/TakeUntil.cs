﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class TakeUntil<TSource, TOther> : Producer<TSource, TakeUntil<TSource, TOther>._>
    {
        private readonly IObservable<TSource> _source;
        private readonly IObservable<TOther> _other;

        public TakeUntil(IObservable<TSource> source, IObservable<TOther> other)
        {
            _source = source;
            _other = other;
        }

        protected override _ CreateSink(IObserver<TSource> observer) => new _(observer);

        protected override void Run(_ sink) => sink.Run(this);

        internal sealed class _ : IdentitySink<TSource>
        {
            private IDisposable _mainDisposable;
            private IDisposable _otherDisposable;
            private int _halfSerializer;
            private Exception _error;

            public _(IObserver<TSource> observer)
                : base(observer)
            {
            }

            public void Run(TakeUntil<TSource, TOther> parent)
            {
                Disposable.SetSingle(ref _otherDisposable, parent._other.Subscribe(new OtherObserver(this)));
                Disposable.SetSingle(ref _mainDisposable, parent._source.Subscribe(this));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (!Disposable.GetIsDisposed(ref _mainDisposable))
                    {
                        Disposable.TryDispose(ref _mainDisposable);
                        Disposable.TryDispose(ref _otherDisposable);
                    }
                }

                base.Dispose(disposing);
            }

            public override void OnNext(TSource value)
            {
                HalfSerializer.ForwardOnNext(this, value, ref _halfSerializer, ref _error);
            }

            public override void OnError(Exception ex)
            {
                HalfSerializer.ForwardOnError(this, ex, ref _halfSerializer, ref _error);
            }

            public override void OnCompleted()
            {
                HalfSerializer.ForwardOnCompleted(this, ref _halfSerializer, ref _error);
            }

            sealed class OtherObserver : IObserver<TOther>
            {
                readonly _ _parent;

                public OtherObserver(_ parent)
                {
                    _parent = parent;
                }

                public void OnCompleted()
                {
                    // Completion doesn't mean termination in Rx.NET for this operator
                    Disposable.TryDispose(ref _parent._otherDisposable);
                }

                public void OnError(Exception error)
                {
                    HalfSerializer.ForwardOnError(_parent, error, ref _parent._halfSerializer, ref _parent._error);
                }

                public void OnNext(TOther value)
                {
                    HalfSerializer.ForwardOnCompleted(_parent, ref _parent._halfSerializer, ref _parent._error);
                }
            }

        }
    }

    internal sealed class TakeUntil<TSource> : Producer<TSource, TakeUntil<TSource>._>
    {
        private readonly IObservable<TSource> _source;
        private readonly DateTimeOffset _endTime;
        internal readonly IScheduler _scheduler;

        public TakeUntil(IObservable<TSource> source, DateTimeOffset endTime, IScheduler scheduler)
        {
            _source = source;
            _endTime = endTime;
            _scheduler = scheduler;
        }

        public IObservable<TSource> Combine(DateTimeOffset endTime)
        {
            //
            // Minimum semantics:
            //
            //   t                     0--1--2--3--4--5--6--7->   t                     0--1--2--3--4--5--6--7->
            //
            //   xs                    --o--o--o--o--o--o--|      xs                    --o--o--o--o--o--o--|
            //   xs.TU(5AM)            --o--o--o--o--o|           xs.TU(3AM)            --o--o--o|
            //   xs.TU(5AM).TU(3AM)    --o--o--o|                 xs.TU(3AM).TU(5AM)    --o--o--o|
            //
            if (_endTime <= endTime)
                return this;
            else
                return new TakeUntil<TSource>(_source, endTime, _scheduler);
        }

        protected override _ CreateSink(IObserver<TSource> observer) => new _(observer);

        protected override void Run(_ sink) => sink.Run(this);

        internal sealed class _ : IdentitySink<TSource>
        {
            private IDisposable _sourceDisposable;

            private int _wip;

            private Exception _error;

            public _(IObserver<TSource> observer)
                : base(observer)
            {
            }

            public void Run(TakeUntil<TSource> parent)
            {
                SetUpstream(parent._scheduler.Schedule(this, parent._endTime, (_, state) => state.Tick()));
                Disposable.SetSingle(ref _sourceDisposable, parent._source.SubscribeSafe(this));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Disposable.TryDispose(ref _sourceDisposable);
                }
                base.Dispose(disposing);
            }

            private IDisposable Tick()
            {
                OnCompleted();
                return Disposable.Empty;
            }

            public override void OnNext(TSource value)
            {
                HalfSerializer.ForwardOnNext(this, value, ref _wip, ref _error);
            }

            public override void OnError(Exception error)
            {
                HalfSerializer.ForwardOnError(this, error, ref _wip, ref _error);
            }

            public override void OnCompleted()
            {
                HalfSerializer.ForwardOnCompleted(this, ref _wip, ref _error);
            }
        }
    }
}
