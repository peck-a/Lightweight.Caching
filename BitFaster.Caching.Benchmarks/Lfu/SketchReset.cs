﻿
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BitFaster.Caching.Lfu;
using Microsoft.Diagnostics.Tracing.StackSources;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace BitFaster.Caching.Benchmarks.Lfu
{
    [SimpleJob(RuntimeMoniker.Net60)]
    public class SketchReset
    {
        static long ResetMask = 0x7777777777777777L;
        static long OneMask = 0x1111111111111111L;

        long[] table;

        [Params(8192, 1048576)]
        public int Size { get; set; }

        [GlobalSetup]
        public unsafe void Setup()
        {
            table = new long[Size];
        }

        [Benchmark(Baseline = true)]
        public int Reset1()
        {
            int count = 0;
            for (int i = 0; i < table.Length; i++)
            {
                count += BitOps.BitCount(table[i] & OneMask);
                table[i] = (long)((ulong)table[i] >> 1) & ResetMask;
            }

            return count;
        }

        [Benchmark()]
        public int Reset2()
        {
            int count0 = 0;
            int count1 = 0;

            for (int i = 0; i < table.Length; i += 2)
            {
                count0 += BitOps.BitCount(table[i] & OneMask);
                count1 += BitOps.BitCount(table[i + 1] & OneMask);

                table[i] = (long)((ulong)table[i] >> 1) & ResetMask;
                table[i + 1] = (long)((ulong)table[i + 1] >> 1) & ResetMask;
            }

            return count0 + count1;
        }

        [Benchmark()]
        public int Reset4()
        {
            int count0 = 0;
            int count1 = 0;
            int count2 = 0;
            int count3 = 0;

            for (int i = 0; i < table.Length; i += 4)
            {
                count0 += BitOps.BitCount(table[i] & OneMask);
                count1 += BitOps.BitCount(table[i + 1] & OneMask);
                count2 += BitOps.BitCount(table[i + 2] & OneMask);
                count3 += BitOps.BitCount(table[i + 3] & OneMask);

                table[i] = (long)((ulong)table[i] >> 1) & ResetMask;
                table[i + 1] = (long)((ulong)table[i + 1] >> 1) & ResetMask;
                table[i + 2] = (long)((ulong)table[i + 2] >> 1) & ResetMask;
                table[i + 3] = (long)((ulong)table[i + 3] >> 1) & ResetMask;
            }

            return (count0 + count1) + (count2 + count3);
        }

        [Benchmark()]
        public int Reset4NoPopcount()
        {
            for (int i = 0; i < table.Length; i += 4)
            {
                table[i] = (long)((ulong)table[i] >> 1) & ResetMask;
                table[i + 1] = (long)((ulong)table[i + 1] >> 1) & ResetMask;
                table[i + 2] = (long)((ulong)table[i + 2] >> 1) & ResetMask;
                table[i + 3] = (long)((ulong)table[i + 3] >> 1) & ResetMask;
            }

            return 0;
        }

        [Benchmark()]
        public unsafe int ResetAVXNoPopcount()
        {
            var resetMaskVector = Vector256.Create(ResetMask);

            fixed (long* tPtr = table)
            {
                for (int i = 0; i < table.Length; i += 4)
                {
                    Vector256<long> t = Avx2.LoadVector256(tPtr + i);
                    t = Avx2.ShiftRightLogical(t, 1);
                    t = Avx2.And(t, resetMaskVector);
                    Avx2.Store(tPtr + i, t);
                }
            }

            return 0;
        }

        [Benchmark()]
        public unsafe int ResetAVXNoPopcountUnroll2()
        {
            if (table.Length < 16)
            {
                return ResetAVXNoPopcount();
            }

            var resetMaskVector = Vector256.Create(ResetMask);

            fixed (long* tPtr = table)
            {
                for (int i = 0; i < table.Length; i += 8)
                {
                    Vector256<long> t1 = Avx2.LoadVector256(tPtr + i);
                    t1 = Avx2.ShiftRightLogical(t1, 1);
                    t1 = Avx2.And(t1, resetMaskVector);
                    Avx2.Store(tPtr + i, t1);

                    Vector256<long> t2 = Avx2.LoadVector256(tPtr + i + 4);
                    t2 = Avx2.ShiftRightLogical(t2, 1);
                    t2 = Avx2.And(t2, resetMaskVector);
                    Avx2.Store(tPtr + i + 4, t2);
                }
            }

            return 0;
        }

        [Benchmark()]
        public unsafe int ResetAVXNoPopcountUnroll4()
        {
            if (table.Length < 16)
            {
                return ResetAVXNoPopcount();
            }

            var resetMaskVector = Vector256.Create(ResetMask);

            fixed (long* tPtr = table)
            {
                for (int i = 0; i < table.Length; i += 16)
                {
                    Vector256<long> t1 = Avx2.LoadVector256(tPtr + i);
                    t1 = Avx2.ShiftRightLogical(t1, 1);
                    t1 = Avx2.And(t1, resetMaskVector);
                    Avx2.Store(tPtr + i, t1);

                    Vector256<long> t2 = Avx2.LoadVector256(tPtr + i + 4);
                    t2 = Avx2.ShiftRightLogical(t2, 1);
                    t2 = Avx2.And(t2, resetMaskVector);
                    Avx2.Store(tPtr + i + 4, t2);

                    Vector256<long> t3 = Avx2.LoadVector256(tPtr + i + 8);
                    t3 = Avx2.ShiftRightLogical(t3, 1);
                    t3 = Avx2.And(t3, resetMaskVector);
                    Avx2.Store(tPtr + i + 8, t3);

                    Vector256<long> t4 = Avx2.LoadVector256(tPtr + i + 12);
                    t4 = Avx2.ShiftRightLogical(t4, 1);
                    t4 = Avx2.And(t4, resetMaskVector);
                    Avx2.Store(tPtr + i + 12, t4);
                }
            }

            return 0;
        }

        [Benchmark()]
        public unsafe int ResetAVXAlignedNoPopcountUnroll4()
        {
            if (table.Length < 16)
            {
                return ResetAVXNoPopcount();
            }

            var resetMaskVector = Vector256.Create(ResetMask);

            fixed (long* tPtr = table)
            {
                long* alignedPtr = tPtr;
                int remainder = 0;

                while (((ulong)alignedPtr & 31UL) != 0)
                {
                    *alignedPtr = (*alignedPtr >> 1) & ResetMask;
                    alignedPtr++;
                    remainder = 16;
                }

                int c = table.Length - (int)(alignedPtr - tPtr) - remainder;
                int i = 0;

                for(; i < c; i += 16)
                {
                    Vector256<long> t1 = Avx2.LoadAlignedVector256(alignedPtr + i);
                    Vector256<long> t2 = Avx2.LoadAlignedVector256(alignedPtr + i + 4);
                    Vector256<long> t3 = Avx2.LoadAlignedVector256(alignedPtr + i + 8);
                    Vector256<long> t4 = Avx2.LoadAlignedVector256(alignedPtr + i + 12);

                    t1 = Avx2.ShiftRightLogical(t1, 1);
                    t2 = Avx2.ShiftRightLogical(t2, 1);
                    t3 = Avx2.ShiftRightLogical(t3, 1);
                    t4 = Avx2.ShiftRightLogical(t4, 1);

                    t1 = Avx2.And(t1, resetMaskVector);
                    t2 = Avx2.And(t2, resetMaskVector);
                    t3 = Avx2.And(t3, resetMaskVector);
                    t4 = Avx2.And(t4, resetMaskVector);

                    Avx2.StoreAligned(alignedPtr + i, t1);
                    Avx2.StoreAligned(alignedPtr + i + 4, t2);
                    Avx2.StoreAligned(alignedPtr + i + 8, t3);
                    Avx2.StoreAligned(alignedPtr + i + 12, t4);
                }

                int start = (int)(alignedPtr - tPtr) + i;

                for (int j = start; j < table.Length; j++)
                {
                    tPtr[j] = (tPtr[j] >> 1) & ResetMask;
                }

                return 0;
            }
        }
    }
}
