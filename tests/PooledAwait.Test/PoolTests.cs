using System.Collections.Generic;
using Xunit;

namespace PooledAwait.Test
{
    public class PoolTests
    {
        class SomeType {}

        static void Flush()
        {
            // note: only good for the current thread!
            while (Pool<SomeType>.TryGet() != null) { }
        }

        [Fact]
        public void WorksAsExpected_One()
        {
            Flush();
            
            Assert.Null(Pool<SomeType>.TryGet());
            var obj = new SomeType();
            Pool<SomeType>.TryPut(obj);
            var got = Pool<SomeType>.TryGet();
            Assert.Same(obj, got);
            Assert.Null(Pool<SomeType>.TryGet());
        }

        [Fact]
        public void WorksAsExpected_Two()
        {
            Flush();

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
        }

        [Fact]
        public void WorksAsExpected_Three()
        {
            Flush();

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
        }

        [Fact]
        public void WorksAsExpected_Full()
        {
            Flush();

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
        }
    }
}
