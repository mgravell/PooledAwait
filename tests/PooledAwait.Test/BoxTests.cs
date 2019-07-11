using PooledAwait.Internal;
using System;
using Xunit;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class BoxTests
    {
        [Fact]
        public void BoxUnboxWorks()
        {
            static object Create()
            {
                int id = 42;
                string name = "abc";
                var obj = Pool.Box((id, name));
                return obj;
            }

            static void Consume(object obj)
            {
                (var id, var name) = Pool.UnboxAndReturn<(int, string)>(obj);
                Assert.Equal(42, id);
                Assert.Equal("abc", name);
            }

            Consume(Create());
            Counters.Reset();
            Consume(Create());
#if DEBUG
            Assert.Equal(0, Counters.TotalAllocations);
#endif
        }

        [Fact]
        public void DefaultPoolSize() => Assert.Equal(16, Pool<Default>.Size);

        [Fact]
        public void CusomSizeClass() => Assert.Equal(42, Pool<CustomClass>.Size);

        [Fact]
        public void MinPoolSize() => Assert.Equal(0, Pool<MinClass>.Size);

        [Fact]
        public void MaxPoolSize() => Assert.Equal(256, Pool<MaxClass>.Size);

        [Fact]
        public void CusomStructBoxed() => Assert.Equal(14, Pool<Pool.ItemBox<CustomStruct>>.Size);

        class Default { }

        [PoolSize(42)]
        class CustomClass { }

        [PoolSize(14)]
        struct CustomStruct { }

        [PoolSize(-12)]
        class MinClass { }


        [PoolSize(12314114)]
        class MaxClass { }

        [Fact]
        public void CannotUsePoolObject()
        {
            var ex = Assert.Throws<TypeInitializationException>(() => Pool<object>.Size);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal("Pool<Object> is not supported; please use a more specific type", ex.InnerException.Message);
        }
    }
}
