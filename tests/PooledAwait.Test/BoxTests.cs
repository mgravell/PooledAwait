using PooledAwait.Internal;
using Xunit;

namespace PooledAwait.Test
{
    public class BoxTests
    {
        [Fact]
        public void BoxUnboxWorks()
        {
            object Create()
            {
                int id = 42;
                string name = "abc";
                var obj = Pool.Box((id, name));
                return obj;
            }

            void Consume(object obj)
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
    }
}
