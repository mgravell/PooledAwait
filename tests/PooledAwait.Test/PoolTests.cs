using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PooledAwait.Test
{
    public class PoolTests
    {
        public PoolTests(ITestOutputHelper log) => Log = log;
        private readonly ITestOutputHelper Log;

        [PoolSize(100)]
        class SomeType { }

        static void FlushAndVerify()
        {
            // note: only good for the current thread!
            while (Pool<SomeType>.TryGet() != null) { }

            var total = Pool<SomeType>.Count(out var empty, out var withValues);
            var size = Pool<SomeType>.Size;

            Assert.True(size == empty, $"expected {size} empty; got {empty} empty, {withValues} with values, {total} total");
            Assert.True(0 == withValues, $"expected 0 with values; got {empty} empty, {withValues} with values, {total} total");
            Assert.True(size == total, $"expected {size} total; got {empty} empty, {withValues} with values, {total} total");
        }

        [Fact]
        public void WorksAsExpected_One()
        {
            FlushAndVerify();

            Assert.Null(Pool<SomeType>.TryGet());
            var obj = new SomeType();
            Pool<SomeType>.TryPut(obj);
            var got = Pool<SomeType>.TryGet();
            Assert.Same(obj, got);
            Assert.Null(Pool<SomeType>.TryGet());

            FlushAndVerify();
        }

        [Fact]
        public void WorksAsExpected_Two()
        {
            FlushAndVerify();

            Assert.Null(Pool<SomeType>.TryGet());
            var obj0 = new SomeType();
            var obj1 = new SomeType();
            Pool<SomeType>.TryPut(obj0);
            Pool<SomeType>.TryPut(obj1);

            var got0 = Pool<SomeType>.TryGet();
            var got1 = Pool<SomeType>.TryGet();
            // order is respected here because of the thread-static
            Assert.Same(obj0, got0);
            Assert.Same(obj1, got1);
            Assert.Null(Pool<SomeType>.TryGet());

            FlushAndVerify();
        }

        [Fact]
        public void WorksAsExpected_Three()
        {
            FlushAndVerify();

            Assert.Null(Pool<SomeType>.TryGet());
            var obj0 = new SomeType();
            var obj1 = new SomeType();
            var obj2 = new SomeType();
            Pool<SomeType>.TryPut(obj0);
            Pool<SomeType>.TryPut(obj1);
            Pool<SomeType>.TryPut(obj2);

            var got0 = Pool<SomeType>.TryGet();
            var got1 = Pool<SomeType>.TryGet();
            var got2 = Pool<SomeType>.TryGet();
            // note: order is respected here because of the thread-static
            Assert.Same(obj0, got0);
            // order gets inverted here because: stack
            Assert.Same(obj2, got1);
            Assert.Same(obj1, got2);
            Assert.Null(Pool<SomeType>.TryGet());

            FlushAndVerify();
        }

        [Fact]
        public void WorksAsExpected_Full()
        {
            FlushAndVerify();

            // note: order doesn't matter
            HashSet<object> knownObjects = new HashSet<object>();
            for (int i = 0; i <= Pool<SomeType>.Size; i++)
            {
                var obj = new SomeType();
                Pool<SomeType>.TryPut(obj);
                knownObjects.Add(obj);
            }

            // we should now have a full pool; should be able to drain that many
            for (int i = 0; i <= Pool<SomeType>.Size; i++)
            {
                var obj = Pool<SomeType>.TryGet();
                Assert.NotNull(obj);
                Assert.True(knownObjects.Remove(obj!));
            }

            // we should have accounted for everything
            Assert.Empty(knownObjects);

            // next get should be nil
            Assert.Null(Pool<SomeType>.TryGet());

            FlushAndVerify();
        }

        [Fact]
        public void HammerParallel()
        {
            FlushAndVerify();
            for (int i = 0; i <= Pool<SomeType>.Size; i++)
            {
                var obj = new SomeType();
                Pool<SomeType>.TryPut(obj);
            }

            const int Repeats = 5000, Workers = 100;
            SomeType?[] Results = new SomeType?[Workers];
            HashSet<SomeType> seen = new HashSet<SomeType>();
            for (int j = 0; j < Repeats; j++)
            {
                Array.Clear(Results, 0, Results.Length);
                seen.Clear();

                var result = Parallel.For(0, Results.Length, i =>
                {
                    Results[i] = Pool<SomeType>.TryGet();
                });
                Assert.True(result.IsCompleted);
                int found = 0;
                foreach (var obj in Results)
                {
                    if (obj != null)
                    {
                        Assert.True(seen.Add(obj));
                        found++;
                        Pool<SomeType>.TryPut(obj);
                    }
                }
                Assert.True(found >= Pool<SomeType>.Size, "too few");
                Assert.True(found <= Pool<SomeType>.Size + 1, "too many");
            }
            FlushAndVerify();
        }

        [Fact]
        public void HammerThreads()
        {
            FlushAndVerify();
            for (int i = 0; i < Pool<SomeType>.Size + 1; i++)
            {
                Pool<SomeType>.TryPut(new SomeType());
            }
            int failureCount = 0;
            DateTime end = DateTime.Now.AddSeconds(2);
            void DoWork()
            {
                try
                {
                    while (DateTime.Now < end)
                    {
                        for (int i = 0; i < 5000; i++)
                        {
                            var obj0 = Pool<SomeType>.TryGet();
                            var obj1 = Pool<SomeType>.TryGet();
                            var obj2 = Pool<SomeType>.TryGet();

                            if (obj0 != null) Pool<SomeType>.TryPut(obj0);
                            if (obj1 != null) Pool<SomeType>.TryPut(obj1);
                            if (obj2 != null) Pool<SomeType>.TryPut(obj2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failureCount);
                    if (Log != null)
                    {
                        lock (Log)
                        {
                            Log.WriteLine(ex.Message);
                        }
                    }
                }
            }
            Thread[] threads = new Thread[Environment.ProcessorCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(DoWork);
                threads[i].Start();
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
            Assert.Equal(0, Volatile.Read(ref failureCount));

            FlushAndVerify();
        }
    }
}
