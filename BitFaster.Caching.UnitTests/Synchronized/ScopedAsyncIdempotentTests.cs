﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BitFaster.Caching.Synchronized;
using FluentAssertions;
using Xunit;

namespace BitFaster.Caching.UnitTests.Synchronized
{
    public class ScopedAsyncIdempotentTests
    {
        [Fact]
        public async Task WhenCreateFromValueLifetimeContainsValue()
        {
            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>(new IntHolder() { actualNumber = 1 });

            (bool r, Lifetime<IntHolder> l) result = await idempotent.TryCreateLifetimeAsync(1, k =>
            {
                return Task.FromResult(new IntHolder() { actualNumber = 2 });
            });

            result.r.Should().BeTrue();
            result.l.Value.actualNumber.Should().Be(1);
        }

        [Fact]
        public async Task WhenScopeIsDisposedTryCreateReturnsFalse()
        {
            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>(new IntHolder() { actualNumber = 1 });
            idempotent.Dispose();

            (bool r, Lifetime<IntHolder> l) result = await idempotent.TryCreateLifetimeAsync(1, k =>
            {
                return Task.FromResult(new IntHolder() { actualNumber = 2 });
            });

            result.r.Should().BeFalse();
            result.l.Should().BeNull();
        }

        [Fact]
        public void WhenValueIsCreatedDisposeDisposesValue()
        {
            var holder = new IntHolder() { actualNumber = 2 };
            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>(holder);
            
            idempotent.Dispose();

            holder.disposed.Should().BeTrue();
        }

        [Fact]
        public async Task WhenCallersRunConcurrentlyResultIsFromWinner()
        {
            var enter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>();
            var winningNumber = 0;
            var winnerCount = 0;

            Task<(bool r, Lifetime<IntHolder> l)> first = idempotent.TryCreateLifetimeAsync(1, async k =>
            {
                enter.SetResult(true);
                await resume.Task;

                winningNumber = 1;
                Interlocked.Increment(ref winnerCount);
                return new IntHolder() { actualNumber = 1 };
            });

            Task<(bool r, Lifetime<IntHolder> l)> second = idempotent.TryCreateLifetimeAsync(1, async k =>
            {
                enter.SetResult(true);
                await resume.Task;

                winningNumber = 2;
                Interlocked.Increment(ref winnerCount);
                return new IntHolder() { actualNumber = 2 };
            });

            await enter.Task;
            resume.SetResult(true);

            var result1 = await first;
            var result2 = await second;

            result1.r.Should().BeTrue();
            result2.r.Should().BeTrue();

            result1.l.Value.actualNumber.Should().Be(winningNumber);
            result2.l.Value.actualNumber.Should().Be(winningNumber);
                
            winnerCount.Should().Be(1);
        }

        [Fact]
        public async Task WhenDisposedWhileInitResultIsDisposed()
        {
            var enter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>();
            var holder = new IntHolder() { actualNumber = 1 };

            Task<(bool r, Lifetime<IntHolder> l)> first = idempotent.TryCreateLifetimeAsync(1, async k =>
            {
                enter.SetResult(true);
                await resume.Task;

                return holder;
            });

            await enter.Task;
            idempotent.Dispose();
            resume.SetResult(true);

            var result = await first;

            result.r.Should().BeFalse();
            result.l.Should().BeNull();

            holder.disposed.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDisposedWhileThrowingNextInitIsDisposed()
        {
            var enter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var idempotent = new ScopedAsyncIdempotent<int, IntHolder>();
            var holder = new IntHolder() { actualNumber = 1 };

            Task<(bool r, Lifetime<IntHolder> l)> first = idempotent.TryCreateLifetimeAsync(1, async k =>
            {
                enter.SetResult(true);
                await resume.Task;

                throw new InvalidOperationException();
            });

            await enter.Task;
            idempotent.Dispose();
            resume.SetResult(true);

            // At this point, the scoped value is not created but the initializer is marked
            // to dispose the item. If no further calls are made, there is nothing to dispose.
            // If we create an item, to be in a consistent state we should dispose it.

            Func<Task> tryCreateAsync = async () => { await first; };
            await tryCreateAsync.Should().ThrowAsync<InvalidOperationException>();

            (bool r, Lifetime<IntHolder> l) result = await idempotent.TryCreateLifetimeAsync(1, k =>
            {
                return Task.FromResult(holder);
            });

            result.r.Should().BeFalse();
            result.l.Should().BeNull();

            holder.disposed.Should().BeTrue();
        }

        private class IntHolder : IDisposable
        {
            public bool disposed;
            public int actualNumber;

            public void Dispose()
            {
                disposed = true;
            }
        }
    }
}
