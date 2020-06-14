# ⚡ Lightweight.Caching

A collection of lightweight caching primitives.

LRU implementations are intended as an alternative to the System.Runtime.Caching.MemoryCache family of classes (e.g. HttpRuntime.Cache, System.Web.Caching et. al.). MemoryCache makes heap allocations when the native object key is not type string, and does not offer the fastest possible performance.

[![NuGet version](https://badge.fury.io/nu/Lightweight.Caching.svg)](https://badge.fury.io/nu/Lightweight.Caching)

# Overview

| Class |  Description |
|:-------|:---------|
| ClassicLru       | Bounded size LRU based with strict ordering.<br><br>Use if ordering is important, but data structures are synchronized with a lock which limits scalability. |
| ConcurrentLru       |  Bounded size pseudo LRU.<br><br>For when you   want a ConcurrentDictionary, but with bounded size. Maintains psuedo order, but is faster than ClassicLru and not prone to lock contention. |
| ConcurrentTlru        | Bounded size pseudo LRU, items have TTL.<br><br>Same as ConcurrentLru, but with a [time aware least recently used (TLRU)](https://en.wikipedia.org/wiki/Cache_replacement_policies#Time_aware_least_recently_used_(TLRU)) eviction policy. |
| FastConcurrentLru/FastConcurrentTLru      | Same as ConcurrentLru/ConcurrentTLru, but with hit counting logic eliminated making them between 10 and 30% faster.   |
| SingletonCache      | Cache singletons by key. Discard when no longer in use. <br><br> For example, cache a SemaphoreSlim per user, where user population is large, but active user count is low.   |
| Scoped<IDisposable>      | A threadsafe wrapper for storing IDisposable objects in a cache that may dispose and invalidate them. The scope keeps the object alive until all callers have finished.   |

# Performance

## Lru Benchmarks

### Lookup speed

Cache contains 6 items which are fetched repeatedly, no items are evicted. 

- ConcurrentLru family does not move items in the queues, it is just marking as accessed for pure cache hits.
- ClassicLru must maintain item order, and is internally splicing the fetched item to the head of the linked list.
- MemoryCache and ConcurrentDictionary represent a pure lookup. This is the best case scenario for MemoryCache, since the lookup key is a string (if the key were a Guid, using MemoryCache adds string conversion overhead). 

FastConcurrentLru does not allocate and is approximately 10x faster than MemoryCache.

~~~
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.900 (1909/November2018Update/19H2)
Intel Core i7-5600U CPU 2.60GHz (Broadwell), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.301
  [Host]    : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  RyuJitX64 : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT

Job=RyuJitX64  Jit=RyuJit  Platform=X64
~~~

|                       Method |      Mean |    Error |   StdDev | Ratio |  Gen 0 | Allocated |
|----------------------------- |----------:|---------:|---------:|------:|-------:|----------:|
| ConcurrentDictionaryGetOrAdd |  18.72 ns | 0.289 ns | 0.641 ns |  1.00 |      - |         - |
|    FastConcurrentLruGetOrAdd |  25.64 ns | 0.434 ns | 0.427 ns |  1.35 |      - |         - |
|        ConcurrentLruGetOrAdd |  35.53 ns | 0.259 ns | 0.216 ns |  1.86 |      - |         - |
|   FastConcurrentTLruGetOrAdd | 132.75 ns | 1.493 ns | 1.397 ns |  6.96 |      - |         - |
|       ConcurrentTLruGetOrAdd | 144.87 ns | 2.179 ns | 1.819 ns |  7.59 |      - |         - |
|           ClassicLruGetOrAdd |  75.67 ns | 1.513 ns | 1.554 ns |  3.99 |      - |         - |
|      MemoryCacheGetStringKey | 309.14 ns | 2.155 ns | 1.910 ns | 16.17 | 0.0153 |      32 B |

MissHitHitRemove

|               Method |       Mean |    Error |   StdDev | Ratio |  Gen 0 | Allocated |
|--------------------- |-----------:|---------:|---------:|------:|-------:|----------:|
| ConcurrentDictionary |   175.4 ns |  1.80 ns |  1.50 ns |  1.00 | 0.0381 |      80 B |
|    FastConcurrentLru |   370.8 ns |  3.86 ns |  3.02 ns |  2.11 | 0.0534 |     112 B |
|        ConcurrentLru |   379.8 ns |  3.50 ns |  2.93 ns |  2.17 | 0.0534 |     112 B |
|   FastConcurrentTlru |   891.8 ns | 13.16 ns | 11.67 ns |  5.09 | 0.0572 |     120 B |
|       ConcurrentTlru |   917.0 ns | 13.07 ns | 16.05 ns |  5.24 | 0.0572 |     120 B |
|           ClassicLru |   356.9 ns |  5.13 ns |  4.80 ns |  2.04 | 0.0763 |     160 B |
|          MemoryCache | 2,366.7 ns | 46.05 ns | 47.29 ns | 13.49 | 2.3460 |    4912 B |

## Meta-programming using structs for JIT dead code removal and inlining

TemplateConcurrentLru features injectable policies defined as structs. Since structs are subject to special JIT optimizations, the implementation is much faster than if these policies were defined as classes. Using this technique, 'Fast' versions without hit counting are within 30% of the speed of a ConcurrentDictionary.

Since DateTime.UtcNow is around 4x slower than a ConcurrentDictionary lookup, policies that involve time based expiry are significantly slower. Since these are injected as structs and the slow code is optimized away, it is possible maintain the fastest possible speed for the non-TTL policy.
