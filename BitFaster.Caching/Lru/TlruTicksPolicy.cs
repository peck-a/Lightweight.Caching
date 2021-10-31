﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BitFaster.Caching.Lru
{
    /// <summary>
    /// Time aware Least Recently Used (TLRU) is a variant of LRU which discards the least 
    /// recently used items first, and any item that has expired.
    /// </summary>
    /// <remarks>
    /// This class measures time using Environment.TickCount, which is significantly faster
    /// than DateTime.Now. However, if the process runs for longer than 24.8 days, the integer
    /// value will wrap and time measurement will become invalid.
    /// </remarks>
    public readonly struct TLruTicksPolicy<K, V> : IPolicy<K, V, TickCountLruItem<K, V>>
    {
        private readonly int timeToLive;

        public TLruTicksPolicy(TimeSpan timeToLive)
        {
            this.timeToLive = (int)timeToLive.TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TickCountLruItem<K, V> CreateItem(K key, V value)
        {
            return new TickCountLruItem<K, V>(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Touch(TickCountLruItem<K, V> item)
        {
            item.WasAccessed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldDiscard(TickCountLruItem<K, V> item)
        {
            if (Environment.TickCount - item.TickCount > this.timeToLive)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteHot(TickCountLruItem<K, V> item)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteWarm(TickCountLruItem<K, V> item)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemDestination RouteCold(TickCountLruItem<K, V> item)
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
}
