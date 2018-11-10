﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return new DefaultIfEmptyAsyncIterator<TSource>(source, defaultValue);
        }

        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return DefaultIfEmpty(source, default);
        }

        private sealed class DefaultIfEmptyAsyncIterator<TSource> : AsyncIterator<TSource>, IAsyncIListProvider<TSource>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly TSource _defaultValue;

            private IAsyncEnumerator<TSource> _enumerator;

            public DefaultIfEmptyAsyncIterator(IAsyncEnumerable<TSource> source, TSource defaultValue)
            {
                Debug.Assert(source != null);

                _source = source;
                _defaultValue = defaultValue;
            }

            public override AsyncIterator<TSource> Clone()
            {
                return new DefaultIfEmptyAsyncIterator<TSource>(_source, _defaultValue);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore(CancellationToken cancellationToken)
            {
                switch (state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(cancellationToken);
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            current = _enumerator.Current;
                            state = AsyncIteratorState.Iterating;
                        }
                        else
                        {
                            current = _defaultValue;
                            await _enumerator.DisposeAsync().ConfigureAwait(false);
                            _enumerator = null;

                            state = AsyncIteratorState.Disposed;
                        }
                        return true;

                    case AsyncIteratorState.Iterating:
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            current = _enumerator.Current;
                            return true;
                        }
                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }

            public async Task<TSource[]> ToArrayAsync(CancellationToken cancellationToken)
            {
                var array = await _source.ToArray(cancellationToken).ConfigureAwait(false);
                return array.Length == 0 ? new[] { _defaultValue } : array;
            }

            public async Task<List<TSource>> ToListAsync(CancellationToken cancellationToken)
            {
                var list = await _source.ToList(cancellationToken).ConfigureAwait(false);
                if (list.Count == 0)
                {
                    list.Add(_defaultValue);
                }

                return list;
            }

            public async Task<int> GetCountAsync(bool onlyIfCheap, CancellationToken cancellationToken)
            {
                int count;
                if (!onlyIfCheap || _source is ICollection<TSource> || _source is ICollection)
                {
                    count = await _source.Count(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var listProv = _source as IAsyncIListProvider<TSource>;
                    count = listProv == null ? -1 : await listProv.GetCountAsync(onlyIfCheap: true, cancellationToken).ConfigureAwait(false);
                }

                return count == 0 ? 1 : count;
            }
        }
    }
}
