﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static IAsyncEnumerable<TSource> IgnoreElements<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new IgnoreElementsAsyncIterator<TSource>(source);
        }

        private sealed class IgnoreElementsAsyncIterator<TSource> : AsyncIterator<TSource>
        {
            private readonly IAsyncEnumerable<TSource> source;
            private IAsyncEnumerator<TSource> enumerator;

            public IgnoreElementsAsyncIterator(IAsyncEnumerable<TSource> source)
            {
                this.source = source;
            }

            public override AsyncIterator<TSource> Clone()
            {
                return new IgnoreElementsAsyncIterator<TSource>(source);
            }

            public override void Dispose()
            {
                if (enumerator != null)
                {
                    enumerator.Dispose();
                    enumerator = null;
                }

                base.Dispose();
            }

            protected override async Task<bool> MoveNextCore(CancellationToken cancellationToken)
            {
                switch (state)
                {
                    case AsyncIteratorState.Allocated:
                        enumerator = source.GetEnumerator();
                        state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        if (!await enumerator.MoveNext(cancellationToken)
                                             .ConfigureAwait(false))
                        {
                            break;
                        }

                        goto case AsyncIteratorState.Iterating; // Loop
                }

                Dispose();
                return false;
            }
        }
    }
}