﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class DistinctUntilChanged<TSource, TKey> : Pipe<TSource>
    {
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        private TKey _currentKey;
        private bool _hasCurrentKey;

        public DistinctUntilChanged(IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            : base(source)
        {
            _keySelector = keySelector;
            _comparer = comparer;
        }

        protected override Pipe<TSource, TSource> Clone() => new DistinctUntilChanged<TSource, TKey>(_source, _keySelector, _comparer);
        

        public override void OnNext(TSource value)
        {
            var key = default(TKey);
            try
            {
                key = _keySelector(value);
            }
            catch (Exception exception)
            {
                ForwardOnError(exception);
                return;
            }

            var comparerEquals = false;
            if (_hasCurrentKey)
            {
                try
                {
                    comparerEquals = _comparer.Equals(_currentKey, key);
                }
                catch (Exception exception)
                {
                    ForwardOnError(exception);
                    return;
                }
            }

            if (!_hasCurrentKey || !comparerEquals)
            {
                _hasCurrentKey = true;
                _currentKey = key;
                ForwardOnNext(value);
            }
        }
    }
}
