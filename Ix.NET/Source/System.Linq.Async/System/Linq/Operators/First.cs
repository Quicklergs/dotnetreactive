﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return FirstCore(source, CancellationToken.None);
        }

        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return FirstCore(source, cancellationToken);
        }

        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return FirstCore(source, predicate, CancellationToken.None);
        }

        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return FirstCore(source, predicate, cancellationToken);
        }

        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return FirstCore(source, predicate, CancellationToken.None);
        }

        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Task<bool>> predicate, CancellationToken cancellationToken)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return FirstCore(source, predicate, cancellationToken);
        }

        private static async Task<TSource> FirstCore<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
        {
            var first = await TryGetFirst(source, cancellationToken).ConfigureAwait(false);

            return first.HasValue ? first.Value : throw Error.NoElements();
        }

        private static async Task<TSource> FirstCore<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
        {
            var first = await TryGetFirst(source, predicate, cancellationToken).ConfigureAwait(false);

            return first.HasValue ? first.Value : throw Error.NoElements();
        }

        private static async Task<TSource> FirstCore<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, Task<bool>> predicate, CancellationToken cancellationToken)
        {
            var first = await TryGetFirst(source, predicate, cancellationToken).ConfigureAwait(false);

            return first.HasValue ? first.Value : throw Error.NoElements();
        }
    }
}
