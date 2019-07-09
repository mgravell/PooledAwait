using PooledAwait.Internal;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class LazyTaskTests
    {
        [Fact]
        public void DefaultLazy()
        {
            var source = default(LazyTaskCompletionSource);
            Assert.False(source.IsValid);
            Assert.False(source.HasTask);
            Assert.False(source.HasSource);
            Assert.False(source.TrySetCanceled());
            Assert.False(source.TrySetException(new Exception()));
            Assert.False(source.TrySetResult());

            Assert.ThrowsAsync<InvalidOperationException>(() => source.Task);
        }

        [Fact]
        public void CreateAndDispose()
        {
            using (var source = LazyTaskCompletionSource.Create()) { }
            Counters.Reset();
            using (var source = LazyTaskCompletionSource.Create()) { }
#if DEBUG
            Assert.Equal(0, Counters.TotalAllocations);
#endif
            LazyTaskCompletionSource clone;
            using (var source = LazyTaskCompletionSource.Create())
            {
                Assert.True(source.IsValid);
                Assert.False(source.HasTask);
                Assert.False(source.HasSource);
#if DEBUG
                Assert.Equal(0, Counters.TotalAllocations);
#endif
                var task = source.Task;
                Assert.True(source.IsValid);
                Assert.True(source.HasTask);
                Assert.True(source.HasSource);
#if DEBUG
                Assert.Equal(1, Counters.TotalAllocations);
#endif
                clone = source;
            }
            Assert.False(clone.IsValid);
        }

        class TestException : Exception { }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        public async Task TaskFirst(ValueTaskSourceStatus status, bool async)
        {
            using (var source = LazyTaskCompletionSource.Create())
            {
                Assert.False(source.HasTask);
                Assert.False(source.HasSource);
                var task = source.Task;
                Assert.True(source.HasTask);
                Assert.True(source.HasSource);
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        Assert.True(source.TrySetCanceled());
                        Assert.False(source.TrySetCanceled());
                        try {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        Assert.True(source.TrySetException(new TestException()));
                        Assert.False(source.TrySetException(new TestException()));
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (TestException) { }
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        Assert.True(source.TrySetResult());
                        Assert.False(source.TrySetResult());
                        if (async) await task;
                        else task.Wait();
                        Assert.NotSame(task, TaskUtils.CompletedTask);
                        break;
                }
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        public async Task TaskLast(ValueTaskSourceStatus status, bool async)
        {
            using (var source = LazyTaskCompletionSource.Create())
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        Assert.True(source.TrySetCanceled());
                        Assert.False(source.TrySetCanceled());
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        Assert.True(source.TrySetException(new TestException()));
                        Assert.False(source.TrySetException(new TestException()));
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        Assert.True(source.TrySetResult());
                        Assert.False(source.TrySetResult());
                        break;
                }
                Assert.Equal(status == ValueTaskSourceStatus.Canceled, source.HasTask);
                Assert.False(source.HasSource);
                var task = source.Task;
                Assert.True(source.HasTask);
                Assert.False(source.HasSource);
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (TestException) { }
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        if (async) await task;
                        else task.Wait();
                        Assert.Same(task, TaskUtils.CompletedTask);
                        break;
                }

            }
        }



        [Fact]
        public void DefaultLazyT()
        {
            var source = default(LazyTaskCompletionSource<int>);
            Assert.False(source.IsValid);
            Assert.False(source.HasTask);
            Assert.False(source.HasSource);
            Assert.False(source.TrySetCanceled());
            Assert.False(source.TrySetException(new Exception()));
            Assert.False(source.TrySetResult(42));

            Assert.ThrowsAsync<InvalidOperationException>(() => source.Task);
        }

        [Fact]
        public void CreateAndDisposeT()
        {
            using (var source = LazyTaskCompletionSource<int>.Create()) { }
            Counters.Reset();
            using (var source = LazyTaskCompletionSource<int>.Create()) { }
#if DEBUG
            Assert.Equal(0, Counters.TotalAllocations);
#endif
            LazyTaskCompletionSource<int> clone;
            using (var source = LazyTaskCompletionSource<int>.Create())
            {
                Assert.True(source.IsValid);
                Assert.False(source.HasTask);
                Assert.False(source.HasSource);
#if DEBUG
                Assert.Equal(0, Counters.TotalAllocations);
#endif
                var task = source.Task;
                Assert.True(source.IsValid);
                Assert.True(source.HasTask);
                Assert.True(source.HasSource);
#if DEBUG
                Assert.Equal(1, Counters.TotalAllocations);
#endif
                clone = source;
            }
            Assert.False(clone.IsValid);
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        public async Task TaskFirstT(ValueTaskSourceStatus status, bool async)
        {
            using (var source = LazyTaskCompletionSource<int>.Create())
            {
                Assert.False(source.HasTask);
                Assert.False(source.HasSource);
                var task = source.Task;
                Assert.True(source.HasTask);
                Assert.True(source.HasSource);
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        Assert.True(source.TrySetCanceled());
                        Assert.False(source.TrySetCanceled());
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        Assert.True(source.TrySetException(new TestException()));
                        Assert.False(source.TrySetException(new TestException()));
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (TestException) { }
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        Assert.True(source.TrySetResult(42));
                        Assert.False(source.TrySetResult(42));
                        if (async) Assert.Equal(42, await task);
                        else Assert.Equal(42, task.Result);

                        Assert.NotSame(task, TaskUtils.CompletedTask);
                        break;
                }
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        public async Task TaskLastT(ValueTaskSourceStatus status, bool async)
        {
            using (var source = LazyTaskCompletionSource<int>.Create())
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        Assert.True(source.TrySetCanceled());
                        Assert.False(source.TrySetCanceled());
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        Assert.True(source.TrySetException(new TestException()));
                        Assert.False(source.TrySetException(new TestException()));
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        Assert.True(source.TrySetResult(42));
                        Assert.False(source.TrySetResult(42));
                        break;
                }
                Assert.Equal(status == ValueTaskSourceStatus.Canceled, source.HasTask);
                Assert.False(source.HasSource);
                var task = source.Task;
                Assert.True(source.HasTask);
                Assert.False(source.HasSource);
                switch (status)
                {
                    case ValueTaskSourceStatus.Canceled:
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        break;
                    case ValueTaskSourceStatus.Faulted:
                        try
                        {
                            if (async) await task;
                            else task.GetAwaiter().GetResult();
                        }
                        catch (TestException) { }
                        break;
                    case ValueTaskSourceStatus.Succeeded:
                        if (async) Assert.Equal(42, await task);
                        else Assert.Equal(42, task.Result);

                        Assert.NotSame(task, TaskUtils.CompletedTask);
                        break;
                }

            }
        }
    }
}
