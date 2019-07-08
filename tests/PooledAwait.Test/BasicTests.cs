using PooledAwait.Internal;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    public class BasicTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CompletedTaskIdentity(bool isAsync)
        {
            Task pending = Impl();
            if (isAsync) Assert.NotSame(Task.CompletedTask, pending);
            else Assert.Same(Task.CompletedTask, pending);
            await pending;

            async PooledTask Impl()
            {
                if(isAsync)
                {
                    await Task.Yield();
                    await Task.Yield();
                }
            }
        }

        [Fact]
        public async Task ZeroAllocTaskAsync()
        {
            await Impl();
            Counters.Reset();
            await Impl();
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(0, Counters.PooledStateRecycled.Value);
            Assert.Equal(2, Counters.StateMachineBoxRecycled.Value);

            async PooledTask Impl()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Fact]
        public async Task ZeroAllocValueTaskAsync()
        {
            await Impl();
            Counters.Reset();
            await Impl();
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(1, Counters.PooledStateRecycled.Value);
            Assert.Equal(2, Counters.StateMachineBoxRecycled.Value);

            async PooledValueTask Impl()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Fact]
        public async Task ZeroAllocTaskTAsync()
        {
            Assert.Equal(42, await Impl());
            Counters.Reset();
            Assert.Equal(42, await Impl());
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(0, Counters.PooledStateRecycled.Value);
            Assert.Equal(2, Counters.StateMachineBoxRecycled.Value);

            async PooledTask<int> Impl()
            {
                await Task.Yield();
                await Task.Yield();
                return 42;
            }
        }

        [Fact]
        public async Task ZeroAllocValueTaskTAsync()
        {
            Assert.Equal(42, await Impl());
            Counters.Reset();
            Assert.Equal(42, await Impl());
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(1, Counters.PooledStateRecycled.Value);
            Assert.Equal(2, Counters.StateMachineBoxRecycled.Value);

            async PooledValueTask<int> Impl()
            {
                await Task.Yield();
                await Task.Yield();
                return 42;
            }
        }
    }
}
