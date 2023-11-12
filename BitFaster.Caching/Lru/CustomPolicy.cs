﻿using System;
using System.Runtime.CompilerServices;

namespace BitFaster.Caching.Lru
{
    public interface IExpiry<K, V>
    {
        Func<K, V, TimeSpan> GetExpireAfterCreate { get; }

        Func<K, V, TimeSpan> GetExpireAfterRead { get; }
        
        Func<K, V, TimeSpan> GetExpireAfterUpdate { get; }
    }

    public readonly struct Expiry<K, V> : IExpiry<K, V>
    {
        private readonly Func<K, V, TimeSpan> expireAfterCreate;
        private readonly Func<K, V, TimeSpan> expireAfterRead;
        private readonly Func<K, V, TimeSpan> expireAfterUpdate;

        public Expiry(Func<K, V, TimeSpan> expireAfterCreate)
        {
            this.expireAfterCreate = expireAfterCreate;
            this.expireAfterRead = expireAfterCreate;
            this.expireAfterUpdate = expireAfterCreate;
        }

        public Expiry(Func<K, V, TimeSpan> expireAfterCreate, Func<K, V, TimeSpan> expireAfterRead, Func<K, V, TimeSpan> expireAfterUpdate)
        {
            this.expireAfterCreate = expireAfterCreate;
            this.expireAfterRead = expireAfterRead;
            this.expireAfterUpdate = expireAfterUpdate;
        }

        public Func<K, V, TimeSpan> GetExpireAfterCreate => expireAfterCreate;

        public Func<K, V, TimeSpan> GetExpireAfterRead => expireAfterRead;

        public Func<K, V, TimeSpan> GetExpireAfterUpdate => expireAfterUpdate;
    }

#if NETCOREAPP3_0_OR_GREATER
    internal readonly struct CustomExpiryPolicy<K, V> : IItemPolicy<K, V, LongTickCountLruItem<K, V>>
    {
        private readonly IExpiry<K, V> expiry;
        private readonly Time time;

        public TimeSpan TimeToLive => TimeSpan.Zero;

        public CustomExpiryPolicy(IExpiry<K, V> expiry)
        {
            this.expiry = expiry;
            this.time = new Time();
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongTickCountLruItem<K, V> CreateItem(K key, V value)
        {
            var ttl = expiry.GetExpireAfterCreate(key, value);
            return new LongTickCountLruItem<K, V>(key, value, ttl.Ticks + Environment.TickCount64);
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Touch(LongTickCountLruItem<K, V> item)
        {
            var ttl = expiry.GetExpireAfterRead(item.Key, item.Value);
            item.TickCount = this.time.Last + ttl.Ticks;
            item.WasAccessed = true;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(LongTickCountLruItem<K, V> item)
        {
            var ttl = expiry.GetExpireAfterUpdate(item.Key, item.Value);
            item.TickCount = Environment.TickCount64 + ttl.Ticks;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldDiscard(LongTickCountLruItem<K, V> item)
        {
            this.time.Last = Environment.TickCount64;
            if (this.time.Last > item.TickCount)
            {
                return true;
            }

            return false;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanDiscard()
        {
            return true;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteHot(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Cold;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteWarm(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Cold;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteCold(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Remove;
        }
    }
#else
    // TODO: this should use stopwatch timing
    internal readonly struct CustomExpiryPolicy<K, V> : IItemPolicy<K, V, LongTickCountLruItem<K, V>>
    {
        private readonly IExpiry<K, V> expiry;
        private readonly Time time;

        public TimeSpan TimeToLive => TimeSpan.Zero;

        public CustomExpiryPolicy(IExpiry<K, V> expiry)
        {
            this.expiry = expiry;
            this.time = new Time();
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongTickCountLruItem<K, V> CreateItem(K key, V value)
        {
            var ttl = expiry.GetExpireAfterCreate(key, value);
            return new LongTickCountLruItem<K, V>(key, value, ttl.Ticks + Environment.TickCount);
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Touch(LongTickCountLruItem<K, V> item)
        {
            var ttl = expiry.GetExpireAfterRead(item.Key, item.Value);
            item.TickCount = this.time.Last + ttl.Ticks;
            item.WasAccessed = true;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(LongTickCountLruItem<K, V> item)
        {
            var ttl = expiry.GetExpireAfterUpdate(item.Key, item.Value);
            item.TickCount = Environment.TickCount + ttl.Ticks;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldDiscard(LongTickCountLruItem<K, V> item)
        {
            this.time.Last = Environment.TickCount;
            if (this.time.Last > item.TickCount)
            {
                return true;
            }

            return false;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanDiscard()
        {
            return true;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteHot(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Cold;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteWarm(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Cold;
        }

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteCold(LongTickCountLruItem<K, V> item)
        {
            if (this.ShouldDiscard(item))
            {
                return ItemDestination.Remove;
            }

            if (item.WasAccessed)
            {
                return ItemDestination.Warm;
            }

            return ItemDestination.Remove;
        }
    }

#endif
}
