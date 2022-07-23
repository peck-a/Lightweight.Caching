﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitFaster.Caching.Lru;
using FluentAssertions;
using Xunit;

namespace BitFaster.Caching.UnitTests
{
    public abstract class ScopedAsyncCacheTestBase
    {
        protected const int capacity = 6;
        protected readonly IScopedAsyncCache<int, Disposable> cache;

        protected List<ItemRemovedEventArgs<int, Scoped<Disposable>>> removedItems = new();

        protected ScopedAsyncCacheTestBase(IScopedAsyncCache<int, Disposable> cache)
        {
            this.cache = cache;
        }

        [Fact]
        public void WhenCreatedCapacityPropertyWrapsInnerCache()
        {
            this.cache.Capacity.Should().Be(capacity);
        }

        [Fact]
        public void WhenItemIsAddedCountIsCorrect()
        {
            this.cache.Count.Should().Be(0);

            this.cache.AddOrUpdate(1, new Disposable());

            this.cache.Count.Should().Be(1);
        }

        [Fact]
        public void WhenItemIsAddedThenLookedUpMetricsAreCorrect()
        {
            this.cache.AddOrUpdate(1, new Disposable());
            this.cache.ScopedTryGet(1, out var lifetime);

            this.cache.Metrics.Misses.Should().Be(0);
            this.cache.Metrics.Hits.Should().Be(1);
        }

        [Fact]
        public void WhenEventHandlerIsRegisteredItIsFired()
        {
            this.cache.Events.ItemRemoved += OnItemRemoved;

            this.cache.AddOrUpdate(1, new Disposable());
            this.cache.TryRemove(1);

            this.removedItems.First().Key.Should().Be(1);
        }

        [Fact]
        public void WhenKeyDoesNotExistAddOrUpdateAddsNewItem()
        {
            var d = new Disposable();
            this.cache.AddOrUpdate(1, d);

            this.cache.ScopedTryGet(1, out var lifetime).Should().BeTrue();
            lifetime.Value.Should().Be(d);
        }

        [Fact]
        public void WhenKeyExistsAddOrUpdateUpdatesExistingItem()
        {
            var d1 = new Disposable();
            var d2 = new Disposable();
            this.cache.AddOrUpdate(1, d1);
            this.cache.AddOrUpdate(1, d2);

            this.cache.ScopedTryGet(1, out var lifetime).Should().BeTrue();
            lifetime.Value.Should().Be(d2);
        }

        [Fact]
        public void WhenItemUpdatedOldValueIsAliveUntilLifetimeCompletes()
        {
            var d1 = new Disposable();
            var d2 = new Disposable();

            // start a lifetime on 1
            this.cache.AddOrUpdate(1, d1);
            this.cache.ScopedTryGet(1, out var lifetime1).Should().BeTrue();

            using (lifetime1)
            {
                // replace 1
                this.cache.AddOrUpdate(1, d2);

                // cache reflects replacement
                this.cache.ScopedTryGet(1, out var lifetime2).Should().BeTrue();
                lifetime2.Value.Should().Be(d2);

                d1.IsDisposed.Should().BeFalse();
            }

            d1.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void WhenClearedItemsAreDisposed()
        {
            var d = new Disposable();
            this.cache.AddOrUpdate(1, d);

            this.cache.Clear();

            d.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void WhenItemExistsTryGetReturnsLifetime()
        {
            this.cache.AddOrUpdate(1, new Disposable());
            this.cache.ScopedTryGet(1, out var lifetime).Should().BeTrue();

            lifetime.Should().NotBeNull();
        }

        [Fact]
        public void WhenItemDoesNotExistTryGetReturnsFalse()
        {
            this.cache.ScopedTryGet(1, out var lifetime).Should().BeFalse();
        }

        [Fact]
        public void WhenCacheContainsValuesTrim1RemovesColdestValue()
        {
            this.cache.AddOrUpdate(0, new Disposable());
            this.cache.AddOrUpdate(1, new Disposable());
            this.cache.AddOrUpdate(2, new Disposable());

            this.cache.Trim(1);

            this.cache.ScopedTryGet(0, out var lifetime).Should().BeFalse();
        }

        [Fact]
        public void WhenKeyDoesNotExistTryRemoveReturnsFalse()
        {
            this.cache.TryRemove(1).Should().BeFalse();
        }

        [Fact]
        public void WhenKeyExistsTryRemoveReturnsTrue()
        {
            this.cache.AddOrUpdate(1, new Disposable());
            this.cache.TryRemove(1).Should().BeTrue();
        }

        [Fact]
        public void WhenKeyDoesNotExistTryUpdateReturnsFalse()
        {
            this.cache.TryUpdate(1, new Disposable()).Should().BeFalse();
        }

        [Fact]
        public void WhenKeyExistsTryUpdateReturnsTrue()
        {
            this.cache.AddOrUpdate(1, new Disposable());

            this.cache.TryUpdate(1, new Disposable()).Should().BeTrue();
        }

        protected void OnItemRemoved(object sender, ItemRemovedEventArgs<int, Scoped<Disposable>> e)
        {
            this.removedItems.Add(e);
        }
    }
}
