﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class Concat : AsyncEnumerableTests
    {
        [Fact]
        public void Concat_Null()
        {
            Assert.Throws<ArgumentNullException>(() => AsyncEnumerable.Concat(default, Return42));
            Assert.Throws<ArgumentNullException>(() => AsyncEnumerable.Concat(Return42, default));
        }

        [Fact]
        public async Task Concat1Async()
        {
            var ys = new[] { 1, 2, 3 }.ToAsyncEnumerable().Concat(new[] { 4, 5, 6 }.ToAsyncEnumerable());

            var e = ys.GetAsyncEnumerator();
            await HasNextAsync(e, 1);
            await HasNextAsync(e, 2);
            await HasNextAsync(e, 3);
            await HasNextAsync(e, 4);
            await HasNextAsync(e, 5);
            await HasNextAsync(e, 6);
            await NoNextAsync(e);
        }

        [Fact]
        public async Task Concat2Async()
        {
            var ex = new Exception("Bang");
            var ys = new[] { 1, 2, 3 }.ToAsyncEnumerable().Concat(Throw<int>(ex));

            var e = ys.GetAsyncEnumerator();
            await HasNextAsync(e, 1);
            await HasNextAsync(e, 2);
            await HasNextAsync(e, 3);
            await AssertThrowsAsync(e.MoveNextAsync(), ex);
        }

        [Fact]
        public async Task Concat3Async()
        {
            var ex = new Exception("Bang");
            var ys = Throw<int>(ex).Concat(new[] { 4, 5, 6 }.ToAsyncEnumerable());

            var e = ys.GetAsyncEnumerator();
            await AssertThrowsAsync(e.MoveNextAsync(), ex);
        }

        [Fact]
        public async Task Concat7Async()
        {
            var ws = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var xs = new[] { 4, 5 }.ToAsyncEnumerable();
            var ys = new[] { 6, 7, 8 }.ToAsyncEnumerable();
            var zs = new[] { 9, 10, 11 }.ToAsyncEnumerable();

            var res = ws.Concat(xs).Concat(ys).Concat(zs);

            var e = res.GetAsyncEnumerator();
            await HasNextAsync(e, 1);
            await HasNextAsync(e, 2);
            await HasNextAsync(e, 3);
            await HasNextAsync(e, 4);
            await HasNextAsync(e, 5);
            await HasNextAsync(e, 6);
            await HasNextAsync(e, 7);
            await HasNextAsync(e, 8);
            await HasNextAsync(e, 9);
            await HasNextAsync(e, 10);
            await HasNextAsync(e, 11);
            await NoNextAsync(e);
        }

        [Fact]
        public async Task Concat8()
        {
            var ws = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var xs = new[] { 4, 5 }.ToAsyncEnumerable();
            var ys = new[] { 6, 7, 8 }.ToAsyncEnumerable();
            var zs = new[] { 9, 10, 11 }.ToAsyncEnumerable();

            var res = ws.Concat(xs).Concat(ys).Concat(zs);

            await SequenceIdentity(res);
        }

        [Fact]
        public async Task Concat10()
        {
            var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var ys = new[] { 4, 5 }.ToAsyncEnumerable();
            var zs = new[] { 6, 7, 8 }.ToAsyncEnumerable();

            var c = xs.Concat(ys).Concat(zs);

            var res = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            Assert.True(res.SequenceEqual(await c.ToArrayAsync()));
        }

        [Fact]
        public async Task Concat11()
        {
            var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var ys = new[] { 4, 5 }.ToAsyncEnumerable();
            var zs = new[] { 6, 7, 8 }.ToAsyncEnumerable();

            var c = xs.Concat(ys).Concat(zs);

            var res = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            Assert.True(res.SequenceEqual(await c.ToListAsync()));
        }

        [Fact]
        public async Task Concat12()
        {
            var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var ys = new[] { 4, 5 }.ToAsyncEnumerable();
            var zs = new[] { 6, 7, 8 }.ToAsyncEnumerable();

            var c = xs.Concat(ys).Concat(zs);

            Assert.Equal(8, await c.CountAsync());
        }

        [Fact]
        public async Task Concat_Concurrency()
        {
            var state = new SharedState();
            var xs = new[] { 1, 2, 3 }.ToSharedStateAsyncEnumerable(state);
            var ys = new[] { 4, 5 }.ToSharedStateAsyncEnumerable(state);
            var zs = new[] { 6, 7, 8 }.ToSharedStateAsyncEnumerable(state);

            await xs.Concat(ys).Concat(zs).LastAsync();

            Assert.Equal(0, state.ConcurrentAccessCount);
        }
    }
}
