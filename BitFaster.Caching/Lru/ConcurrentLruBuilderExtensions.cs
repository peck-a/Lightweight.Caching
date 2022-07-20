﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitFaster.Caching.Lru.Builder;

namespace BitFaster.Caching.Lru
{
    public static class ConcurrentLruBuilderExtensions
    {
        public static ScopedLruBuilder<K, V, Scoped<V>> WithScopedValues<K, V>(this ConcurrentLruBuilder<K, V> b) where V : IDisposable
        {
            var scoped = new ConcurrentLruBuilder<K, Scoped<V>>(b.info);
            return new ScopedLruBuilder<K, V, Scoped<V>>(scoped);
        }

        public static AtomicLruBuilder<K, V> WithAtomicCreate<K, V>(this ConcurrentLruBuilder<K, V> b)
        {
            var a = new ConcurrentLruBuilder<K, AsyncAtomic<K, V>>(b.info);
            return new AtomicLruBuilder<K, V>(a);
        }

        public static ScopedAtomicLruBuilder<K, V, Scoped<V>> WithAtomicCreate<K, V, W>(this ScopedLruBuilder<K, V, W> b) where V : IDisposable where W : IScoped<V>
        {
            var atomicScoped = new ConcurrentLruBuilder<K, AsyncAtomic<K, Scoped<V>>>(b.info);

            return new ScopedAtomicLruBuilder<K, V, Scoped<V>>(atomicScoped);
        }

        public static ScopedAtomicLruBuilder<K, V, Scoped<V>> WithScopedValues<K, V>(this AtomicLruBuilder<K, V> b) where V : IDisposable
        {
            var atomicScoped = new ConcurrentLruBuilder<K, AsyncAtomic<K, Scoped<V>>>(b.info);
            return new ScopedAtomicLruBuilder<K, V, Scoped<V>>(atomicScoped);
        }
    }
}
