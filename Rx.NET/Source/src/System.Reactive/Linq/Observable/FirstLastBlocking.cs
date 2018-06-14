﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;

namespace System.Reactive.Linq.ObservableImpl
{

    internal abstract class BaseBlocking<T> : CountdownEvent, IObserver<T>
    {
        protected IDisposable _upstream;

        internal T _value;
        internal bool _hasValue;
        internal Exception _error;

        int once;

        internal BaseBlocking() : base(1) { }

        internal void SetUpstream(IDisposable d)
        {
            Disposable.SetSingle(ref _upstream, d);
        }

        protected void Unblock()
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                Signal();
            }
        }

        public abstract void OnCompleted();
        public virtual void OnError(Exception error)
        {
            _value = default;
            this._error = error;
            Unblock();
        }
        public abstract void OnNext(T value);

        public new void Dispose()
        {
            base.Dispose();
            if (!Disposable.GetIsDisposed(ref _upstream))
            {
                Disposable.TryDispose(ref _upstream);
            }
        }
    }

    internal sealed class FirstBlocking<T> : BaseBlocking<T>
    {
        internal FirstBlocking() : base() { }

        public override void OnCompleted()
        {
            Unblock();
            if (!Disposable.GetIsDisposed(ref _upstream))
            {
                Disposable.TryDispose(ref _upstream);
            }
        }

        public override void OnError(Exception error)
        {
            base.OnError(error);  
            if (!Disposable.GetIsDisposed(ref _upstream))
            {
                Disposable.TryDispose(ref _upstream);
            }
        }

        public override void OnNext(T value)
        {
            if (!_hasValue)
            {
                this._value = value;
                this._hasValue = true;
                Disposable.TryDispose(ref _upstream);
                Unblock();
            }
        }
    }

    internal sealed class LastBlocking<T> : BaseBlocking<T>
    {
        internal LastBlocking() : base() { }

        public override void OnCompleted()
        {
            Unblock();
            Disposable.TryDispose(ref _upstream);
        }

        public override void OnError(Exception error)
        {
            base.OnError(error);
            Disposable.TryDispose(ref _upstream);
        }

        public override void OnNext(T value)
        {
            this._value = value;
            this._hasValue = true;
        }

    }
}
