using System;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    public class UnitTest1
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
    }
}
