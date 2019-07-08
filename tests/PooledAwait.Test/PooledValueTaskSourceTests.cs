using System;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class PooledValueTaskSourceTests
    {
        [Fact]
        public void CreateAndSet()
        {
            var source = PooledValueTaskSource<int>.Create();
            var task = source.Task;

            Assert.False(task.IsCompleted);
            Assert.True(source.IsValid);

            Assert.True(source.TrySetResult(42));

            Assert.True(task.IsCompleted);
            Assert.Equal(42, task.Result);

            // now it has been fetched, it should have been reset/recycled
            Assert.False(source.IsValid);
            Assert.False(source.TrySetResult(43));
            Assert.Throws<InvalidOperationException>(() => _ = task.Result);
        }

        [Fact]
        public async Task CreateAndSetAsync()
        {
            var source = PooledValueTaskSource<int>.Create();
            var task = source.Task;

            Assert.False(task.IsCompleted);
            Assert.True(source.IsValid);

            Assert.True(source.TrySetResult(42));

            Assert.True(task.IsCompleted);
            Assert.Equal(42, await task);

            // now it has been fetched, it should have been reset/recycled
            Assert.False(source.IsValid);
            Assert.False(source.TrySetResult(43));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => _ = await task);
        }

        [Fact]
        public void ReadSync()
        {
            var source = PooledValueTaskSource<int>.Create();
            _ = Delayed();
            Assert.Equal(42, source.Task.Result);

            async FireAndForget Delayed()
            {
                await Task.Delay(100);
                source.TrySetResult(42);
            }
        }
    }
}
