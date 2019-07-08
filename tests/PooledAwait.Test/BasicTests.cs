using PooledAwait.Internal;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocTaskAsync(bool isAsync)
        {
            Task pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            Counters.Reset();
            await Impl();
            Assert.Equal(isAsync ? 1 : 0, Counters.TaskAllocated.Value);
            Assert.Equal(isAsync ? 1 : 0, Counters.TotalAllocations);
            Assert.Equal(0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);

            async PooledTask Impl()
            {
                if (isAsync)
                {
                    await Task.Yield();
                    await Task.Yield();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocValueTaskAsync(bool isAsync)
        {
            ValueTask pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            Counters.Reset();
            await Impl();
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(isAsync ? 1 : 0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);

            async PooledValueTask Impl()
            {
                if (isAsync)
                {
                    await Task.Yield();
                    await Task.Yield();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocTaskTAsync(bool isAsync)
        {
            Task<int> pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            Counters.Reset();
            Assert.Equal(42, await Impl());
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);

            async PooledTask<int> Impl()
            {
                if (isAsync)
                {
                    await Task.Yield();
                    await Task.Yield();
                }
                return 42;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocValueTaskTAsync(bool isAsync)
        {
            ValueTask<int> pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            Counters.Reset();
            Assert.Equal(42, await Impl());
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(isAsync ? 1 : 0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);

            async PooledValueTask<int> Impl()
            {
                if (isAsync)
                {
                    await Task.Yield();
                    await Task.Yield();
                }
                return 42;
            }
        }
    }
}
